﻿using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Movement.Enums;
using AmeisenBotX.Logging;
using AmeisenBotX.Pathfinding.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AmeisenBotX.Core.Statemachine.States
{
    internal class StateAttacking : BasicState
    {
        public StateAttacking(AmeisenBotStateMachine stateMachine, AmeisenBotConfig config, WowInterface wowInterface) : base(stateMachine, config, wowInterface)
        {
            Enemies = new List<WowUnit>();
            CombatClassStopwatch = new Stopwatch();

            // default distance values
            DistanceToTarget = WowInterface.CombatClass == null || WowInterface.CombatClass.IsMelee ? 3.0 : 25.0;
        }

        public double DistanceToTarget { get; private set; }

        private Stopwatch CombatClassStopwatch { get; set; }

        private List<WowUnit> Enemies { get; set; }

        private DateTime LastRotationCheck { get; set; }

        public override void Enter()
        {
            WowInterface.HookManager.ClearTarget();
            WowInterface.MovementEngine.Reset();
            WowInterface.XMemory.Write(WowInterface.OffsetList.CvarMaxFps, Config.MaxFpsCombat);
        }

        public override void Execute()
        {
            WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Player);

            if (!WowInterface.ObjectManager.Player.IsInCombat
                && !StateMachine.IsAnyPartymemberInCombat()
                && (WowInterface.BattlegroundEngine == null || !WowInterface.BattlegroundEngine.ForceCombat))
            {
                StateMachine.SetState(BotState.Idle);
                return;
            }

            // we can do nothing until the ObjectManager is initialzed
            if (WowInterface.ObjectManager != null
                && WowInterface.ObjectManager.Player != null)
            {
                // use the default Target selection algorithm if the CombatClass doenst

                // Note: this only works for dps classes, tanks and healers need
                //        to implement their own target selection algorithm
                if (WowInterface.CombatClass == null || !WowInterface.CombatClass.HandlesTargetSelection)
                {
                    // make sure we got both objects refreshed before we check them
                    WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Target);

                    // do we need to clear our target
                    if (IsTargetInvalid())
                    {
                        WowInterface.HookManager.ClearTarget();
                        WowInterface.MovementEngine.Reset();

                        WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Player);

                        // select a new target if our current target is invalid
                        if (SelectTargetToAttack(out WowUnit target))
                        {
                            WowInterface.HookManager.TargetGuid(target.Guid);
                            WowInterface.MovementEngine.Reset();

                            WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Player);
                            WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Target);
                        }
                        else
                        {
                            // there is no valid target to attack
                            return;
                        }
                    }
                }

                // use the default MovementEngine to move if the CombatClass doesnt
                if ((WowInterface.CombatClass == null || !WowInterface.CombatClass.HandlesMovement)
                        && WowInterface.ObjectManager.Target != null)
                {
                    HandleMovement(WowInterface.ObjectManager.Target);
                }

                // if no CombatClass is loaded, just autoattack
                if (WowInterface.CombatClass != null)
                {
                    CombatClassStopwatch.Restart();

                    WowInterface.CombatClass.Execute();

                    CombatClassStopwatch.Stop();
                    AmeisenLogger.Instance.Log("CombatClass", $"Execution took: {CombatClassStopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    if (!WowInterface.ObjectManager.Player.IsAutoAttacking)
                    {
                        WowInterface.HookManager.StartAutoAttack();
                    }
                }
            }
        }

        public override void Exit()
        {
            WowInterface.HookManager.ClearTarget();
            WowInterface.MovementEngine.Reset();

            Enemies.Clear();

            // set our normal maxfps
            WowInterface.XMemory.Write(WowInterface.OffsetList.CvarMaxFps, Config.MaxFps);
        }

        private void HandleMovement(WowUnit target)
        {
            // we cant move to a null target and we don't want to
            // move when we are casting or channeling something
            if (!BotUtils.IsValidUnit(target)
                || target.IsDead
                || WowInterface.ObjectManager.Player.CurrentlyCastingSpellId > 0
                || WowInterface.ObjectManager.Player.CurrentlyChannelingSpellId > 0)
            {
                return;
            }

            // if we are close enough, stop movement and start attacking
            double distance = WowInterface.ObjectManager.Player.Position.GetDistance(target.Position);
            if (distance <= DistanceToTarget)
            {
                // do we need to stop movement
                WowInterface.HookManager.StopClickToMove(WowInterface.ObjectManager.Player);

                // perform a facing check every 250ms, should be enough
                if (target.Guid != WowInterface.ObjectManager.PlayerGuid
                    && !BotMath.IsFacing(WowInterface.ObjectManager.Player.Position, WowInterface.ObjectManager.Player.Rotation, target.Position)
                    && DateTime.Now - LastRotationCheck > TimeSpan.FromMilliseconds(250))
                {
                    WowInterface.HookManager.FacePosition(WowInterface.ObjectManager.Player, target.Position);
                    LastRotationCheck = DateTime.Now;
                }
            }
            else
            {
                // position to got to should be a bit ahead of the enemy to predict movement hehe xd
                Vector3 positionToGoTo = target.Position;
                WowInterface.MovementEngine.SetState(MovementEngineState.Moving, positionToGoTo);
                WowInterface.MovementEngine.Execute();
            }
        }

        private bool IsTargetInvalid()
            => !BotUtils.IsValidUnit(WowInterface.ObjectManager.Target)
                || WowInterface.ObjectManager.Target.IsDead
                || WowInterface.ObjectManager.Target.Guid == WowInterface.ObjectManager.PlayerGuid
                || WowInterface.ObjectManager.Player.Position.GetDistance(WowInterface.ObjectManager.Target.Position) > 50;

        private bool SelectTargetToAttack(out WowUnit target)
        {
            // TODO: need to handle duels, our target will
            // be friendly there but is attackable

            // get all targets that are not friendly to us
            List<WowUnit> nonFriendlyUnits = WowInterface.ObjectManager.WowObjects.OfType<WowUnit>()
                .Where(e => e.IsInCombat && WowInterface.HookManager.GetUnitReaction(WowInterface.ObjectManager.Player, e) != WowUnitReaction.Friendly)
                .ToList();

            // remove all invalid, dead units
            nonFriendlyUnits = nonFriendlyUnits.Where(e => BotUtils.IsValidUnit(e) || e.IsDead).ToList();

            // if there are no non Friendly units, we can't attack anything
            if (nonFriendlyUnits.Count > 0)
            {
                List<WowUnit> unitsInCombat = nonFriendlyUnits.OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position)).ToList();
                target = unitsInCombat.FirstOrDefault();

                if (target != null)
                {
                    return true;
                }
                else
                {
                    // maybe we are able to assist our partymembers
                    if (WowInterface.ObjectManager.PartymemberGuids.Count > 0)
                    {
                        Dictionary<WowUnit, int> partymemberTargets = new Dictionary<WowUnit, int>();
                        WowInterface.ObjectManager.Partymembers.ForEach(e =>
                        {
                            if (e.TargetGuid > 0)
                            {
                                WowUnit target = WowInterface.ObjectManager.GetWowObjectByGuid<WowUnit>(e.TargetGuid);

                                if (target != null
                                    && BotUtils.IsValidUnit(e)
                                    && target.IsInCombat
                                    && WowInterface.HookManager.GetUnitReaction(WowInterface.ObjectManager.Player, target) != WowUnitReaction.Friendly)
                                {
                                    if (partymemberTargets.ContainsKey(target))
                                    {
                                        partymemberTargets[target]++;
                                    }
                                    else
                                    {
                                        partymemberTargets.Add(target, 1);
                                    }
                                }
                            }
                        });

                        List<KeyValuePair<WowUnit, int>> selectedTargets = partymemberTargets.OrderByDescending(e => e.Value).ToList();

                        // filter out invalid, not in combat and friendly units
                        WowUnit validTarget = selectedTargets.First().Key;

                        if (validTarget != null)
                        {
                            target = validTarget;
                            return true;
                        }
                    }

                    // last fallback, target our nearest enemy
                    WowInterface.HookManager.TargetNearestEnemy();
                    WowInterface.ObjectManager.UpdateObject(WowInterface.ObjectManager.Player);

                    target = WowInterface.ObjectManager.WowObjects.OfType<WowUnit>()
                        .FirstOrDefault(e => e.IsInCombat && e.Guid == WowInterface.ObjectManager.Player.Guid);

                    if (BotUtils.IsValidUnit(target)
                        && target.IsInCombat)
                    {
                        return true;
                    }
                }
            }

            target = null;
            return false;
        }
    }
}