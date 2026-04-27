using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon;
using RogueDungeon.Dungeon.View;

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 在 DungeonReady 后将玩家对齐到起始房间中心，确保测试场景中的初始站位一致。
    /// </summary>
    public class PlayerStartRoomAligner : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform; // 需要对齐位置的玩家 Transform

        private void Awake()
        {
            if (playerTransform == null)
                playerTransform = transform;
        }

        private void OnEnable()
        {
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        /// <summary>
        /// 地牢视图就绪后，将玩家移动到当前起始房间中心点。
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            if (playerTransform == null)
            {
                Debug.LogError("[PlayerStartRoomAligner] playerTransform 为空，无法对齐");
                return;
            }

            var dungeonManager = DungeonManager.Instance;
            if (dungeonManager == null || dungeonManager.CurrentRoom == null)
            {
                Debug.LogWarning("[PlayerStartRoomAligner] DungeonManager 或 CurrentRoom 为空，跳过对齐");
                return;
            }

            var startBounds = DungeonCamera.CalculateRoomBounds(dungeonManager.CurrentRoom);
            var center = startBounds.center;
            playerTransform.position = new Vector3(center.x, center.y, playerTransform.position.z);
        }
    }
}
