﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Statemachine.Enums;
using AmeisenBotX.Core.Statemachine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;
using static AmeisenBotX.Core.Statemachine.Utils.InterruptManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class HunterSurvival : BasicCombatClass
    {
        // author: Jannis Höschele

        private readonly string aimedShotSpell = "Aimed Shot";
        private readonly string arcaneShotSpell = "Arcane Shot";
        private readonly string aspectOfTheDragonhawkSpell = "Aspect of the Dragonhawk";
        private readonly string blackArrowSpell = "Black Arrow";
        private readonly string callPetSpell = "Call Pet";
        private readonly string concussiveShotSpell = "Concussive Shot";
        private readonly string deterrenceSpell = "Deterrence";
        private readonly string disengageSpell = "Disengage";
        private readonly string explosiveShotSpell = "Explosive Shot";
        private readonly string feignDeathSpell = "Feign Death";
        private readonly string frostTrapSpell = "Frost Trap";
        private readonly string huntersMarkSpell = "Hunter's Mark";
        private readonly string killCommandSpell = "Kill Command";
        private readonly string killShotSpell = "Kill Shot";
        private readonly string mendPetSpell = "Mend Pet";
        private readonly string mongooseBiteSpell = "Mongoose Bite";
        private readonly string multiShotSpell = "Multi-Shot";
        private readonly string rapidFireSpell = "Rapid Fire";
        private readonly string raptorStrikeSpell = "Raptor Strike";
        private readonly string revivePetSpell = "Revive Pet";
        private readonly string serpentStingSpell = "Serpent Sting";
        private readonly string steadyShotSpell = "Steady Shot";
        private readonly string wingClipSpell = "Wing Clip";
        private readonly string wyvernStingSpell = "Wyvern Sting";

        public HunterSurvival(WowInterface wowInterface) : base(wowInterface)
        {
            PetManager = new PetManager(
                WowInterface.ObjectManager.Pet,
                TimeSpan.FromSeconds(15),
                () => CastSpellIfPossible(mendPetSpell, 0, true),
                () => CastSpellIfPossible(callPetSpell, 0),
                () => CastSpellIfPossible(revivePetSpell, 0));

            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { aspectOfTheDragonhawkSpell, () => CastSpellIfPossible(aspectOfTheDragonhawkSpell, 0, true) }
            };

            TargetAuraManager.DebuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { huntersMarkSpell, () => CastSpellIfPossible(huntersMarkSpell, WowInterface.ObjectManager.TargetGuid, true) },
                { serpentStingSpell, () => CastSpellIfPossible(serpentStingSpell, WowInterface.ObjectManager.TargetGuid, true) },
                { blackArrowSpell, () => CastSpellIfPossible(blackArrowSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };

            TargetInterruptManager.InterruptSpells = new SortedList<int, CastInterruptFunction>()
            {
                { 0, () => CastSpellIfPossible(wyvernStingSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };
        }

        public override string Author => "Jannis";

        public override WowClass Class => WowClass.Hunter;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Survival Hunter spec.";

        public override string Displayname => "Hunter Survival";

        public override bool HandlesMovement => false;

        public override bool HandlesTargetSelection => false;

        public override bool IsMelee => false;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicIntellectComparator(new List<ArmorType>() { ArmorType.SHIEDLS });

        public override CombatClassRole Role => CombatClassRole.Dps;

        public override string Version => "1.0";

        private bool DisengagePrepared { get; set; } = false;

        private bool InFrostTrapCombo { get; set; } = false;

        private DateTime LastAutoAttackCheck { get; set; }

        private PetManager PetManager { get; set; }

        public override void Execute()
        {
            // we dont want to do anything if we are casting something...
            if (WowInterface.ObjectManager.Player.IsCasting)
            {
                return;
            }

            if (DateTime.Now - LastAutoAttackCheck > TimeSpan.FromSeconds(4) && !WowInterface.ObjectManager.Player.IsAutoAttacking)
            {
                LastAutoAttackCheck = DateTime.Now;
                WowInterface.HookManager.StartAutoAttack();
            }

            if (MyAuraManager.Tick()
                || TargetAuraManager.Tick()
                || TargetInterruptManager.Tick()
                || PetManager.Tick())
            {
                return;
            }

            if (WowInterface.ObjectManager.Target != null)
            {
                double distanceToTarget = WowInterface.ObjectManager.Target.Position.GetDistance(WowInterface.ObjectManager.Player.Position);

                if (WowInterface.ObjectManager.Player.HealthPercentage < 15
                    && CastSpellIfPossible(feignDeathSpell, 0))
                {
                    return;
                }

                if (distanceToTarget < 3)
                {
                    if (CastSpellIfPossible(frostTrapSpell, 0, true))
                    {
                        InFrostTrapCombo = true;
                        DisengagePrepared = true;
                        return;
                    }

                    if (WowInterface.ObjectManager.Player.HealthPercentage < 30
                        && CastSpellIfPossible(deterrenceSpell, 0, true))
                    {
                        return;
                    }

                    if (CastSpellIfPossible(raptorStrikeSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || CastSpellIfPossible(mongooseBiteSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        return;
                    }
                }
                else
                {
                    if (DisengagePrepared
                        && CastSpellIfPossible(concussiveShotSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        DisengagePrepared = false;
                        return;
                    }

                    if (InFrostTrapCombo
                        && CastSpellIfPossible(disengageSpell, 0, true))
                    {
                        InFrostTrapCombo = false;
                        return;
                    }

                    if (WowInterface.ObjectManager.Target.HealthPercentage < 20
                        && CastSpellIfPossible(killShotSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        return;
                    }

                    CastSpellIfPossible(killCommandSpell, WowInterface.ObjectManager.TargetGuid, true);
                    CastSpellIfPossible(rapidFireSpell, 0);

                    if ((WowInterface.ObjectManager.WowObjects.OfType<WowUnit>().Where(e => WowInterface.ObjectManager.Target.Position.GetDistance(e.Position) < 16).Count() > 2 && CastSpellIfPossible(multiShotSpell, WowInterface.ObjectManager.TargetGuid, true))
                        || CastSpellIfPossible(explosiveShotSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || CastSpellIfPossible(aimedShotSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || CastSpellIfPossible(steadyShotSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        return;
                    }
                }
            }
        }

        public override void OutOfCombatExecute()
        {
            if (MyAuraManager.Tick()
                || PetManager.Tick())
            {
                return;
            }

            DisengagePrepared = false;
        }
    }
}