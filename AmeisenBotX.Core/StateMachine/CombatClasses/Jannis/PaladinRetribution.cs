﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Statemachine.Enums;
using System.Collections.Generic;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;
using static AmeisenBotX.Core.Statemachine.Utils.InterruptManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class PaladinRetribution : BasicCombatClass
    {
        // author: Jannis Höschele

        private readonly string avengingWrathSpell = "Avenging Wrath";
        private readonly string blessingOfMightSpell = "Blessing of Might";
        private readonly string consecrationSpell = "Consecration";
        private readonly string crusaderStrikeSpell = "Crusader Strike";
        private readonly string divinePleaSpell = "Divine Plea";
        private readonly string divineStormSpell = "Divine Storm";
        private readonly string exorcismSpell = "Exorcism";
        private readonly string hammerOfJusticeSpell = "Hammer of Justice";
        private readonly string hammerOfWrathSpell = "Hammer of Wrath";
        private readonly string holyLightSpell = "Holy Light";
        private readonly string holyWrathSpell = "Holy Wrath";
        private readonly string judgementOfLightSpell = "Judgement of Light";
        private readonly string layOnHandsSpell = "Lay on Hands";
        private readonly string retributionAuraSpell = "Retribution Aura";
        private readonly string sealOfVengeanceSpell = "Seal of Vengeance";

        public PaladinRetribution(WowInterface wowInterface) : base(wowInterface)
        {
            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { blessingOfMightSpell, () => CastSpellIfPossible(blessingOfMightSpell, WowInterface.ObjectManager.PlayerGuid, true) },
                { retributionAuraSpell, () => CastSpellIfPossible(retributionAuraSpell, 0, true) },
                { sealOfVengeanceSpell, () => CastSpellIfPossible(sealOfVengeanceSpell, 0, true) }
            };

            TargetInterruptManager.InterruptSpells = new SortedList<int, CastInterruptFunction>()
            {
                { 0, () => CastSpellIfPossible(hammerOfJusticeSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };
        }

        public override string Author => "Jannis";

        public override WowClass Class => WowClass.Paladin;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Retribution Paladin spec.";

        public override string Displayname => "Paladin Retribution";

        public override bool HandlesMovement => false;

        public override bool HandlesTargetSelection => false;

        public override bool IsMelee => true;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicStrengthComparator();

        public override CombatClassRole Role => CombatClassRole.Dps;

        public override string Version => "1.0";

        public override void Execute()
        {
            // we dont want to do anything if we are casting something...
            if (WowInterface.ObjectManager.Player.IsCasting)
            {
                return;
            }

            if (!WowInterface.ObjectManager.Player.IsAutoAttacking)
            {
                WowInterface.HookManager.StartAutoAttack();
            }

            if (MyAuraManager.Tick()
                || TargetInterruptManager.Tick()
                || (MyAuraManager.Buffs.Contains(sealOfVengeanceSpell.ToLower())
                    && CastSpellIfPossible(judgementOfLightSpell, 0))
                || (WowInterface.ObjectManager.Player.HealthPercentage < 20
                    && CastSpellIfPossible(layOnHandsSpell, WowInterface.ObjectManager.PlayerGuid))
                || (WowInterface.ObjectManager.Player.HealthPercentage < 60
                    && CastSpellIfPossible(holyLightSpell, WowInterface.ObjectManager.PlayerGuid, true))
                || CastSpellIfPossible(avengingWrathSpell, 0, true)
                || (WowInterface.ObjectManager.Player.ManaPercentage < 80
                    && CastSpellIfPossible(divinePleaSpell, 0, true)))
            {
                return;
            }

            if (WowInterface.ObjectManager.Target != null)
            {
                if ((WowInterface.ObjectManager.Player.HealthPercentage < 20
                        && CastSpellIfPossible(hammerOfWrathSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || CastSpellIfPossible(crusaderStrikeSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || CastSpellIfPossible(divineStormSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || CastSpellIfPossible(consecrationSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || CastSpellIfPossible(exorcismSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || CastSpellIfPossible(holyWrathSpell, WowInterface.ObjectManager.TargetGuid, true))
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