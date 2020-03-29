﻿using AmeisenBotX.Core.Character.Inventory.Objects;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Movement.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AmeisenBotX.Core.Statemachine.States
{
    public class StateSelling : BasicState
    {
        public StateSelling(AmeisenBotStateMachine stateMachine, AmeisenBotConfig config, WowInterface wowInterface) : base(stateMachine, config, wowInterface)
        {
        }

        private bool IsAtNpc { get; set; }

        private DateTime SellActionGo { get; set; }

        public override void Enter()
        {
            IsAtNpc = false;
        }

        public override void Execute()
        {
            if (WowInterface.HookManager.GetFreeBagSlotCount() > 4
               || !WowInterface.CharacterManager.Inventory.Items.Any(e => e.Price > 0))
            {
                StateMachine.SetState(BotState.Idle);
                return;
            }

            if (!IsAtNpc)
            {
                WowUnit selectedUnit = WowInterface.ObjectManager.WowObjects.OfType<WowUnit>()
                    .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position))
                    .FirstOrDefault(e => e.GetType() != typeof(WowPlayer)
                        && WowInterface.HookManager.GetUnitReaction(WowInterface.ObjectManager.Player, e) == WowUnitReaction.Friendly
                        && e.IsRepairVendor
                        && e.Position.GetDistance(WowInterface.ObjectManager.Player.Position) < 50);

                if (selectedUnit != null && !selectedUnit.IsDead)
                {
                    double distance = WowInterface.ObjectManager.Player.Position.GetDistance(selectedUnit.Position);
                    if (distance > 3.0)
                    {
                        WowInterface.MovementEngine.SetState(MovementEngineState.Moving, selectedUnit.Position);
                        WowInterface.MovementEngine.Execute();
                    }
                    else
                    {
                        WowInterface.HookManager.UnitOnRightClick(selectedUnit);
                        SellActionGo = DateTime.Now + TimeSpan.FromSeconds(1);
                        IsAtNpc = true;
                    }
                }
                else if(DateTime.Now > SellActionGo)
                {
                    WowInterface.HookManager.SellAllGrayItems();
                    foreach (IWowItem item in WowInterface.CharacterManager.Inventory.Items.Where(e => e.Price > 0))
                    {
                        IWowItem itemToSell = item;
                        if (WowInterface.CharacterManager.IsItemAnImprovement(item, out IWowItem itemToReplace))
                        {
                            itemToSell = itemToReplace;
                            WowInterface.HookManager.ReplaceItem(null, item);
                        }

                        WowInterface.HookManager.UseItemByBagAndSlot(itemToSell.BagId, itemToSell.BagSlot);
                    }
                }
            }
            else
            {
                StateMachine.SetState(BotState.Idle);
            }
        }

        public override void Exit()
        {
        }
    }
}