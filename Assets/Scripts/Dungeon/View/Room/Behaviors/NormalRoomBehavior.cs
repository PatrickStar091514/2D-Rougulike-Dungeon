using UnityEngine;
using RogueDungeon.Core;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 普通房间行为。OnEnter 时锁定所有活跃门（已清理房间跳过），OnClear 记录日志。
    /// Elite / Event 在 MVP 阶段复用此行为。
    /// </summary>
    public class NormalRoomBehavior : IRoomBehavior
    {
        /// <inheritdoc/>
        public void OnEnter(RoomView room)
        {
            // 已清理的房间不再锁门（re-entry guard）
            if (room.Room.Cleared) return;

            Debug.Log($"[NormalRoomBehavior] Normal room entered, locking doors: {room.RoomId}");

            foreach (var door in room.ActiveDoors)
            {
                if (door.State == DoorState.Unlocked)
                    door.Lock();
            }
        }

        /// <inheritdoc/>
        public void OnClear(RoomView room)
        {
            Debug.Log($"[NormalRoomBehavior] Room cleared: {room.RoomId}");
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.ChangeState(GameState.RoomClear);
            gameManager.ChangeState(GameState.RewardSelect);
        }

        /// <inheritdoc/>
        public void OnExit(RoomView room) { }
    }
}
