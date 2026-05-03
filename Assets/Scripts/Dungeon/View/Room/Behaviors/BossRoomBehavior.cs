using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using UnityEngine;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// Boss 房间行为。OnEnter 锁门并记录 Boss 战开始日志，
    /// OnClear 解锁房门、广播 FloorBossDefeated 事件、触发奖励阶段。
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

            // 解锁 Boss 房门，让玩家自由进出（清理剩余房间或使用 Portal）
            foreach (var door in room.ActiveDoors)
            {
                if (door.State == DoorState.Locked)
                    door.Unlock();
            }

            // 通知 Portal 系统生成传送门
            var run = RunManager.Instance?.CurrentRun;
            EventCenter.Broadcast(GameEventType.FloorBossDefeated, new FloorBossDefeatedEvent
            {
                RoomId = room.RoomId,
                FloorIndex = run?.FloorIndex ?? 0
            });

            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.ChangeState(GameState.RoomClear);
            gameManager.ChangeState(GameState.RewardSelect);
        }

        /// <inheritdoc/>
        public void OnExit(RoomView room) { }
    }
}
