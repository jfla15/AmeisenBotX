﻿using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Movement.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Statemachine.States
{
    public class StateLooting : BasicState
    {
        public StateLooting(AmeisenBotStateMachine stateMachine, AmeisenBotConfig config, WowInterface wowInterface) : base(stateMachine, config, wowInterface)
        {
            UnitLootQueue = new Queue<ulong>();
            UnitsAlreadyLootedList = new List<ulong>();
            LastOpenLootTry = DateTime.Now;
        }

        public List<ulong> UnitsAlreadyLootedList { get; private set; }

        private DateTime LastOpenLootTry { get; set; }

        private Queue<ulong> UnitLootQueue { get; set; }

        public override void Enter()
        {
        }

        public override void Execute()
        {
            // add nearby Units to the loot List
            if (Config.LootUnits)
            {
                foreach (WowUnit lootableUnit in StateMachine.GetNearLootableUnits())
                {
                    if (!UnitLootQueue.Contains(lootableUnit.Guid))
                    {
                        UnitLootQueue.Enqueue(lootableUnit.Guid);
                    }
                }
            }

            if (UnitLootQueue.Count == 0)
            {
                StateMachine.SetState(BotState.Idle);
            }
            else
            {
                if (UnitsAlreadyLootedList.Contains(UnitLootQueue.Peek()))
                {
                    if (UnitLootQueue.Count > 0)
                    {
                        UnitLootQueue.Dequeue();
                    }

                    return;
                }

                WowUnit selectedUnit = WowInterface.ObjectManager.WowObjects.OfType<WowUnit>()
                    .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position))
                    .FirstOrDefault(e => e.Guid == UnitLootQueue.Peek());

                if (selectedUnit != null && selectedUnit.IsDead && selectedUnit.IsLootable)
                {
                    if (WowInterface.ObjectManager.Player.Position.GetDistance(selectedUnit.Position) > 3.0)
                    {
                        WowInterface.MovementEngine.SetState(MovementEngineState.Moving, selectedUnit.Position);
                        WowInterface.MovementEngine.Execute();
                    }
                    else if (DateTime.Now - LastOpenLootTry > TimeSpan.FromSeconds(1))
                    {
                        WowInterface.HookManager.StopClickToMoveIfActive(WowInterface.ObjectManager.Player);

                        WowInterface.HookManager.UnitOnRightClick(selectedUnit);
                        if (WowInterface.XMemory.ReadByte(WowInterface.OffsetList.LootWindowOpen, out byte lootOpen)
                             && lootOpen > 0)
                        {
                            WowInterface.HookManager.LootEveryThing();
                            UnitsAlreadyLootedList.Add(UnitLootQueue.Dequeue());
                            LastOpenLootTry = DateTime.Now;

                            if (UnitLootQueue.Count > 0)
                            {
                                UnitLootQueue.Dequeue();
                            }
                        }
                    }
                }
                else
                {
                    if (UnitLootQueue.Count > 0)
                    {
                        UnitLootQueue.Dequeue();
                    }
                }
            }
        }

        public override void Exit()
        {
        }
    }
}