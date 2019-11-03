﻿using AmeisenBotX.Core.Character;
using AmeisenBotX.Core.Data;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Hook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmeisenBotX.Core.StateMachine.CombatClasses
{
    public class PriestHoly : ICombatClass
    {
        private readonly string powerWordFortitudeSpell = "Power Word: Fortitude";
        private readonly string innerFireSpell = "Inner Fire";
        private readonly string flashHealSpell = "Flash Heal";
        private readonly string greaterHealSpell = "Greater Heal";
        private readonly string bindingHealSpell = "Binding Heal";
        private readonly string renewSpell = "Renew";
        private readonly string prayerOfMendingSpell = "Prayer of Mending";
        private readonly string prayerOfHealingSpell = "Prayer of Healing";
        private readonly string hymnOfHopeSpell = "Hymn of Hope";
        private readonly string guardianSpiritSpell = "Guardian Spirit";
        private readonly string resurrectionSpell = "Resurrection";

        private readonly int buffCheckTime = 30;
        private readonly int deadPartymembersCheckTime = 4;

        private Dictionary<int, string> SpellUsageHealDict { get; }

        public PriestHoly(ObjectManager objectManager, CharacterManager characterManager, HookManager hookManager)
        {
            ObjectManager = objectManager;
            CharacterManager = characterManager;
            HookManager = hookManager;

            SpellUsageHealDict = new Dictionary<int, string>()
            {
                { 0, flashHealSpell },
                { 5000, greaterHealSpell },
            };
        }

        public bool HandlesMovement => false;

        public bool HandlesTargetSelection => true;

        public bool IsMelee => false;

        private DateTime LastBuffCheck { get; set; }

        private DateTime LastDeadPartymembersCheck { get; set; }

        private CharacterManager CharacterManager { get; }

        private HookManager HookManager { get; }

        private ObjectManager ObjectManager { get; }

        public void Execute()
        {
            WowUnit target = (WowUnit)ObjectManager.WowObjects.FirstOrDefault(e => e.Guid == ObjectManager.TargetGuid);

            if (target == null || target.IsDead || target.Health == 1)
            {
                HookManager.TargetGuid(ObjectManager.PlayerGuid);
            }

            if (NeedToHealSomeone(out List<WowPlayer> playersThatNeedHealing))
            {
                HandleTargetSelection(playersThatNeedHealing);
                ObjectManager.UpdateObject(ObjectManager.Player.Type, ObjectManager.Player.BaseAddress);

                if (target == null || target.IsDead || target.Health == 1)
                {
                    return;
                }

                ObjectManager.UpdateObject(target.Type, target.BaseAddress);

                if (target.HealthPercentage < 25
                    && IsSpellKnown(guardianSpiritSpell)
                    && !IsOnCooldown(guardianSpiritSpell))
                {
                    HookManager.CastSpell(guardianSpiritSpell);
                    return;
                }

                if (playersThatNeedHealing.Count > 4
                    && IsSpellKnown(prayerOfHealingSpell)
                    && !IsOnCooldown(prayerOfHealingSpell))
                {
                    HookManager.CastSpell(prayerOfHealingSpell);
                    return;
                }

                if (target.HealthPercentage < 70
                    && ObjectManager.Player.HealthPercentage < 70
                    && IsSpellKnown(bindingHealSpell)
                    && !IsOnCooldown(bindingHealSpell))
                {
                    HookManager.CastSpell(bindingHealSpell);
                }

                if (ObjectManager.Player.ManaPercentage < 50
                    && IsSpellKnown(hymnOfHopeSpell)
                    && !IsOnCooldown(hymnOfHopeSpell))
                {
                    HookManager.CastSpell(hymnOfHopeSpell);
                }

                double healthDifference = target.MaxHealth - target.Health;
                List<KeyValuePair<int, string>> spellsToTry = SpellUsageHealDict.Where(e => e.Key <= healthDifference).ToList();

                foreach (KeyValuePair<int, string> keyValuePair in spellsToTry.OrderByDescending(e => e.Value))
                {
                    if (IsSpellKnown(keyValuePair.Value)
                        && HasEnoughMana(keyValuePair.Value)
                        && !IsOnCooldown(keyValuePair.Value))
                    {
                        HookManager.CastSpell(keyValuePair.Value);
                        break;
                    }
                }
            }
            else
            {
                HookManager.TargetGuid(ObjectManager.PlayerGuid);

                if (DateTime.Now - LastBuffCheck > TimeSpan.FromSeconds(buffCheckTime))
                {
                    HandleBuffing();
                }
            }
        }

        public void OutOfCombatExecute()
        {
            if (DateTime.Now - LastBuffCheck > TimeSpan.FromSeconds(buffCheckTime))
            {
                HandleBuffing();
            }

            if (DateTime.Now - LastDeadPartymembersCheck > TimeSpan.FromSeconds(deadPartymembersCheckTime))
            {
                HandleDeadPartymembers();
            }
        }

        private void HandleDeadPartymembers()
        {
            if (IsSpellKnown(resurrectionSpell)
                && !IsOnCooldown(resurrectionSpell))
            {
                IEnumerable<WowPlayer> players = ObjectManager.WowObjects.OfType<WowPlayer>();
                List<WowPlayer> groupPlayers = players.Where(e => e.IsDead && e.Health == 0 && ObjectManager.PartymemberGuids.Contains(e.Guid)).ToList();

                if (groupPlayers.Count > 0)
                {
                    HookManager.TargetGuid(groupPlayers.First().Guid);
                    HookManager.CastSpell(resurrectionSpell);
                }
            }
        }

        private void HandleBuffing()
        {
            List<string> myBuffs = HookManager.GetBuffs(WowLuaUnit.Player.ToString());
            HookManager.TargetGuid(ObjectManager.PlayerGuid);

            if (IsSpellKnown(powerWordFortitudeSpell)
                && !myBuffs.Any(e => e.Equals(powerWordFortitudeSpell, StringComparison.OrdinalIgnoreCase))
                && !IsOnCooldown(powerWordFortitudeSpell))
            {
                HookManager.CastSpell(powerWordFortitudeSpell);
                return;
            }

            if (IsSpellKnown(innerFireSpell)
                && !myBuffs.Any(e => e.Equals(innerFireSpell, StringComparison.OrdinalIgnoreCase))
                && !IsOnCooldown(innerFireSpell))
            {
                HookManager.CastSpell(innerFireSpell);
                return;
            }

            LastBuffCheck = DateTime.Now;
        }

        private bool NeedToHealSomeone(out List<WowPlayer> playersThatNeedHealing)
        {
            IEnumerable<WowPlayer> players = ObjectManager.WowObjects.OfType<WowPlayer>();
            List<WowPlayer> groupPlayers = players.Where(e => !e.IsDead && e.Health > 1 && ObjectManager.PartymemberGuids.Contains(e.Guid) && e.Position.GetDistance2D(ObjectManager.Player.Position) < 35).ToList();

            groupPlayers.Add(ObjectManager.Player);

            playersThatNeedHealing = groupPlayers.Where(e => e.HealthPercentage < 90).ToList();

            return playersThatNeedHealing.Count > 0;
        }

        private void HandleTargetSelection(List<WowPlayer> possibleTargets)
        {
            // select the one with lowest hp
            HookManager.TargetGuid(possibleTargets.OrderBy(e => e.HealthPercentage).First().Guid);
        }

        private bool HasEnoughMana(string spellName)
            => CharacterManager.SpellBook.Spells.OrderByDescending(e => e.Rank).FirstOrDefault(e => e.Name.Equals(spellName))?.Costs <= ObjectManager.Player.Mana;

        private bool IsSpellKnown(string spellName)
            => CharacterManager.SpellBook.Spells.Any(e => e.Name.Equals(spellName));

        private bool IsOnCooldown(string spellName)
            => HookManager.GetSpellCooldown(spellName) > 0;
    }
}