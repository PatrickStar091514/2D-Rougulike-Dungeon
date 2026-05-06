using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using UnityEngine;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// Boss 房间行为。OnEnter 锁门，OnClear 触发奖励阶段。
    /// 门解锁和传送门激活延迟到奖励拾取后（RewardSpawner.OnDropPicked）。
    /// </summary>
    public class BossRoomBehavior : IRoomBehavior
    {
        /// <inheritdoc/>
        public void OnEnter(RoomView room)
        {
            // 已清理的 Boss 房间不再锁门
            if (room.Room.Cleared) return;


            foreach (var door in room.ActiveDoors)
            {
                if (door.State == DoorState.Unlocked)
                    door.Lock();
            }
        }

        /// <inheritdoc/>
        public void OnClear(RoomView room)
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.ChangeState(GameState.RoomClear);
            gameManager.ChangeState(GameState.RewardSelect);
        }

        /// <inheritdoc/>
        public void OnExit(RoomView room) { }
    }
}
