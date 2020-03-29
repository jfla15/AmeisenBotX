﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Statemachine.Enums;
using System.Collections.Generic;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;
using static AmeisenBotX.Core.Statemachine.Utils.InterruptManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class MageFire : BasicCombatClass
    {
        // author: Jannis Höschele

        private readonly string arcaneIntellectSpell = "Arcane Intellect";
        private readonly string counterspellSpell = "Counterspell";
        private readonly string evocationSpell = "Evocation";
        private readonly string fireballSpell = "Fireball";
        private readonly string hotstreakSpell = "Hot Streak";
        private readonly string iceBlockSpell = "Ice Block";
        private readonly string livingBombSpell = "Living Bomb";
        private readonly string manaShieldSpell = "Mana Shield";
        private readonly string mirrorImageSpell = "Mirror Image";
        private readonly string moltenArmorSpell = "Molten Armor";
        private readonly string pyroblastSpell = "Pyroblast";
        private readonly string scorchSpell = "Scorch";
        private readonly string spellStealSpell = "Spellsteal";

        public MageFire(WowInterface wowInterface) : base(wowInterface)
        {
            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { arcaneIntellectSpell, () => CastSpellIfPossible(arcaneIntellectSpell, WowInterface.ObjectManager.PlayerGuid, true) },
                { moltenArmorSpell, () => CastSpellIfPossible(moltenArmorSpell, 0, true) },
                { manaShieldSpell, () => CastSpellIfPossible(manaShieldSpell, 0, true) }
            };

            TargetAuraManager.DebuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { scorchSpell, () => CastSpellIfPossible(scorchSpell, WowInterface.ObjectManager.TargetGuid, true) },
                { livingBombSpell, () => CastSpellIfPossible(livingBombSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };

            TargetAuraManager.DispellBuffs = () => WowInterface.HookManager.HasUnitStealableBuffs(WowLuaUnit.Target) && CastSpellIfPossible(spellStealSpell, WowInterface.ObjectManager.TargetGuid, true);

            TargetInterruptManager.InterruptSpells = new SortedList<int, CastInterruptFunction>()
            {
                { 0, () => CastSpellIfPossible(counterspellSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };
        }

        public override string Author => "Jannis";

        public override WowClass Class => WowClass.Mage;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Fire Mage spec.";

        public override string Displayname => "Mage Fire";

        public override bool HandlesMovement => false;

        public override bool HandlesTargetSelection => false;

        public override bool IsMelee => false;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicIntellectComparator();

        public override CombatClassRole Role => CombatClassRole.Dps;

        public override string Version => "1.0";

        public override void Execute()
        {
            // we dont want to do anything if we are casting something...
            if (WowInterface.ObjectManager.Player.IsCasting)
            {
                return;
            }

            if (MyAuraManager.Tick()
                || TargetAuraManager.Tick()
                || TargetInterruptManager.Tick())
            {
                return;
            }

            if (WowInterface.ObjectManager.Target != null)
            {
                if (CastSpellIfPossible(mirrorImageSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || (WowInterface.ObjectManager.Player.HealthPercentage < 16
                        && CastSpellIfPossible(iceBlockSpell, 0, true))
                    || (MyAuraManager.Buffs.Contains(hotstreakSpell.ToLower()) && CastSpellIfPossible(pyroblastSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || (WowInterface.ObjectManager.Player.ManaPercentage < 40
                        && CastSpellIfPossible(evocationSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || CastSpellIfPossible(fireballSpell, WowInterface.ObjectManager.TargetGuid, true))
                {
                    return;
                }
            }
        }

        public override void OutOfCombatExecute()
        {
            if (MyAuraManager.Tick())
            {
                return;
            }
        }
    }
}