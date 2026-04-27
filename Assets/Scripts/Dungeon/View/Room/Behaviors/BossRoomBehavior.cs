using RogueDungeon.Core;
using UnityEngine;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// Boss 房间行为。OnEnter 锁门并记录 Boss 战开始日志，OnClear 记录 Boss 击败日志。
    /// </summary>
    public class BossRoomBehavior : IRoomBehavior
    {
        /// <inheritdoc/>
        public void OnEnter(RoomView room)
        {
            // 已清理的 Boss 房间不再锁门
            if (room.Room.Cleared) return;

            Debug.Log($"[BossRoomBehavior] Boss room entered: {room.RoomId}");

            foreach (var door in room.ActiveDoors)
            {
                if (door.State == DoorState.Unlocked)
                    door.Lock();
            }
        }

        /// <inheritdoc/>
        public void OnClear(RoomView room)
        {
            Debug.Log($"[BossRoomBehavior] Boss defeated: {room.RoomId}");
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.ChangeState(GameState.RoomClear);
            gameManager.ChangeState(GameState.RewardSelect);
        }

        /// <inheritdoc/>
        public void OnExit(RoomView room) { }
    }
}
