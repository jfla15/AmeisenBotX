﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Statemachine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class ShamanRestoration : BasicCombatClass
    {
        // author: Jannis Höschele

        private readonly string ancestralSpiritSpell = "Ancestral Spirit";
        private readonly int buffCheckTime = 8;
        private readonly string chainHealSpell = "Chain Heal";
        private readonly int deadPartymembersCheckTime = 4;
        private readonly string earthlivingWeaponSpell = "Earthliving Weapon";
        private readonly string earthShieldSpell = "Earth Shield";
        private readonly string healingWaveSpell = "Healing Wave";
        private readonly string naturesSwiftnessSpell = "Nature's Swiftness";
        private readonly string riptideSpell = "Riptide";
        private readonly string tidalForceSpell = "Tidal Force";
        private readonly string waterShieldSpell = "Water Shield";

        public ShamanRestoration(WowInterface wowInterface) : base(wowInterface)
        {
            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { waterShieldSpell, () => CastSpellIfPossible(waterShieldSpell, 0, true) }
            };

            SpellUsageHealDict = new Dictionary<int, string>()
            {
                { 0, riptideSpell },
                { 5000, healingWaveSpell },
            };
        }

        public override string Author => "Jannis";

        public override WowClass Class => WowClass.Shaman;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Restoration Shaman spec.";

        public override string Displayname => "Shaman Restoration";

        public override bool HandlesMovement => false;

        public override bool HandlesTargetSelection => true;

        public override bool IsMelee => false;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicSpiritComparator(new List<ArmorType>() { ArmorType.SHIEDLS });

        public override CombatClassRole Role => CombatClassRole.Heal;

        public override string Version => "1.0";

        private DateTime LastBuffCheck { get; set; }

        private DateTime LastDeadPartymembersCheck { get; set; }

        private Dictionary<int, string> SpellUsageHealDict { get; }

        public override void Execute()
        {
            // we dont want to do anything if we are casting something...
            if (WowInterface.ObjectManager.Player.IsCasting)
            {
                return;
            }

            if (NeedToHealSomeone(out List<WowPlayer> playersThatNeedHealing))
            {
                HandleTargetSelection(playersThatNeedHealing);
                WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Player);

                if (WowInterface.ObjectManager.Target != null)
                {
                    if (WowInterface.ObjectManager.Target.HealthPercentage < 25
                        && CastSpellIfPossible(earthShieldSpell, 0, true))
                    {
                        return;
                    }

                    if (playersThatNeedHealing.Count > 4
                        && CastSpellIfPossible(chainHealSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        return;
                    }

                    if (playersThatNeedHealing.Count > 6
                        && (CastSpellIfPossible(naturesSwiftnessSpell, 0, true)
                        || CastSpellIfPossible(tidalForceSpell, WowInterface.ObjectManager.TargetGuid, true)))
                    {
                        return;
                    }

                    double healthDifference = WowInterface.ObjectManager.Target.MaxHealth - WowInterface.ObjectManager.Target.Health;
                    List<KeyValuePair<int, string>> spellsToTry = SpellUsageHealDict.Where(e => e.Key <= healthDifference).ToList();

                    foreach (KeyValuePair<int, string> keyValuePair in spellsToTry.OrderByDescending(e => e.Value))
                    {
                        if (CastSpellIfPossible(keyValuePair.Value, WowInterface.ObjectManager.TargetGuid, true))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (MyAuraManager.Tick())
                    {
                        return;
                    }
                }
            }
        }

        public override void OutOfCombatExecute()
        {
            if (MyAuraManager.Tick()
                || (DateTime.Now - LastDeadPartymembersCheck > TimeSpan.FromSeconds(deadPartymembersCheckTime)
                && HandleDeadPartymembers(ancestralSpiritSpell)))
            {
                return;
            }
        }

        private void HandleTargetSelection(List<WowPlayer> possibleTargets)
        {
            // select the one with lowest hp
            WowUnit target = possibleTargets.Where(e => !e.IsDead && e.Health > 1).OrderBy(e => e.HealthPercentage).First();

            if (target != null)
            {
                WowInterface.HookManager.TargetGuid(target.Guid);
            }
        }

        private bool NeedToHealSomeone(out List<WowPlayer> playersThatNeedHealing)
        {
            IEnumerable<WowPlayer> players = WowInterface.ObjectManager.WowObjects.OfType<WowPlayer>();
            List<WowPlayer> groupPlayers = players.Where(e => !e.IsDead && e.Health > 1 && WowInterface.ObjectManager.PartymemberGuids.Contains(e.Guid) && e.Position.GetDistance(WowInterface.ObjectManager.Player.Position) < 35).ToList();

            groupPlayers.Add(WowInterface.ObjectManager.Player);

            playersThatNeedHealing = groupPlayers.Where(e => e.HealthPercentage < 90).ToList();

            return playersThatNeedHealing.Count > 0;
        }
    }
}