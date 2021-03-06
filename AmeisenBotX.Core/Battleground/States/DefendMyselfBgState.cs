﻿using AmeisenBotX.Core.Battleground.Enums;
using AmeisenBotX.Core.Battleground.Profiles;
using AmeisenBotX.Core.Data.Objects.WowObject;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Battleground.States
{
    public class DefendMyselfBgState : BasicBattlegroundState
    {
        public DefendMyselfBgState(BattlegroundEngine battlegroundEngine, WowInterface wowInterface) : base(battlegroundEngine)
        {
            WowInterface = wowInterface;
        }

        private WowInterface WowInterface { get; }

        public override void Enter()
        {
        }

        public override void Execute()
        {
            // at the beginning of the BG, move to the enemies base
            // TODO: maybe handle speedbuff here
            IEnumerable<WowPlayer> nearEnemies = WowInterface.ObjectManager.GetNearEnemies<WowPlayer>(WowInterface.ObjectManager.Player.Position, 30);
            IEnumerable<WowPlayer> nearFriends = WowInterface.ObjectManager.GetNearFriends<WowPlayer>(WowInterface.ObjectManager.Player.Position, 30);

            int enemyCount = nearEnemies.Count();
            int friendCount = nearFriends.Count();

            switch (BattlegroundEngine.BattlegroundProfile.BattlegroundType)
            {
                case BattlegroundType.CaptureTheFlag:
                    DoCtfLogic(enemyCount, friendCount);
                    break;

                default:
                    // fight
                    break;
            }
        }

        public override void Exit()
        {
        }

        private void DoCtfLogic(int enemyCount, int friendCount)
        {
            bool isMeFlagCarrier = ((ICtfBattlegroundProfile)BattlegroundEngine.BattlegroundProfile).IsMeFlagCarrier;

            // CTF flag priority
            if (BattlegroundEngine.BattlegroundProfile.BattlegroundType == BattlegroundType.CaptureTheFlag)
            {
                if (BattlegroundEngine.BattlegroundProfile.HanldeInterruptStates())
                {
                    return;
                }

                if (enemyCount > friendCount + 2 || isMeFlagCarrier)
                {
                    // flee to base
                    BattlegroundEngine.SetState(BattlegroundState.MoveToOwnBase);
                }
                else if (enemyCount == 0)
                {
                    WowPlayer enemyFlagCarrier = ((ICtfBattlegroundProfile)BattlegroundEngine.BattlegroundProfile).EnemyFlagCarrierPlayer;
                    WowPlayer ownFlagCarrier = ((ICtfBattlegroundProfile)BattlegroundEngine.BattlegroundProfile).OwnFlagCarrierPlayer;

                    if (enemyFlagCarrier != null && ownFlagCarrier != null)
                    {
                        // move to enemy flag carrier
                        BattlegroundEngine.SetState(BattlegroundState.MoveToEnemyFlagCarrier);
                    }
                    else if (enemyFlagCarrier != null && ownFlagCarrier == null)
                    {
                        // get the enemies flag
                        BattlegroundEngine.SetState(BattlegroundState.MoveToEnemyBase);
                    }
                    else
                    {
                        if (ownFlagCarrier != null)
                        {
                            // help our flag carrier to get the flag to the base
                            BattlegroundEngine.SetState(BattlegroundState.AssistOwnFlagCarrier);
                        }
                        else
                        {
                            // try to get the enemies flag
                            BattlegroundEngine.SetState(BattlegroundState.MoveToEnemyBase);
                        }
                        return;
                    }
                }
            }
        }
    }
}