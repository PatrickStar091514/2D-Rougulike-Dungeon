using System.Collections;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon.View;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Dungeon.Types;

namespace RogueDungeon.Dungeon.Portal
{
    /// <summary>
    /// 跨层传送过渡协调器。场景级 MonoBehaviour，处理 Portal 触碰后的
    /// 黑屏过渡 → 楼层切换 → 传送玩家 → 亮屏的完整序列。
    /// </summary>
    public class PortalTransitCoordinator : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float fadeOutDuration = 0.3f; // 黑屏淡入时长（秒）
        [SerializeField] private float fadeInDuration = 0.3f;  // 亮屏淡出时长（秒）

        private bool _inTransit; // 是否正在过渡中

        /// <summary>
        /// 启动跨层传送过渡序列
        /// </summary>
        /// <param name="portal">触碰的传送门</param>
        public void StartTransit(Portal portal)
        {
            if (_inTransit) return;
            StartCoroutine(TransitCoroutine(portal));
        }

        private IEnumerator TransitCoroutine(Portal portal)
        {
            _inTransit = true;

            int targetFloor = portal.TargetFloorIndex;
            int fromFloor = RunManager.Instance?.CurrentRun?.FloorIndex ?? 0;


            // 1. 禁用玩家
            var player = GameObject.FindGameObjectWithTag("Player");
            // if (player != null)
            //     player.SetActive(false);

            // 2. 黑屏 FadeOut
            yield return StartCoroutine(FadeScreen(fadeOutDuration));

            // 3. 释放旧层 + 切换视图根节点
            var viewManager = FindFirstObjectByType<DungeonViewManager>();
            if (viewManager != null)
            {
                viewManager.ReleaseFloorSlot(fromFloor % 2);
                viewManager.SwitchActiveRoot(targetFloor % 2);
            }

            // 4. 切换地牢数据层
            var dm = DungeonManager.Instance;
            if (dm != null)
            {
                dm.SwitchToFloor(targetFloor);
            }
            else
            {
                Debug.LogError("[PortalTransitCoordinator] DungeonManager.Instance 为 null，无法切换楼层");
                if (player != null) player.SetActive(true);
                _inTransit = false;
                yield break;
            }

            // 最终层通关：SwitchToFloor 已广播 RunEnded，跳过后续楼层切换流程
            if (RunManager.Instance?.CurrentRun == null)
            {
                if (player != null) player.SetActive(true);
                if (portal != null) portal.Deactivate();
                _inTransit = false;
                yield break;
            }

            // 5. 注册新层房间到 _roomViews + 更新 _currentMap
            if (viewManager != null)
            {
                viewManager.RegisterFloorRooms(targetFloor % 2);
                viewManager.SetCurrentMap(dm.CurrentMap);
            }

            // 6. 再次广播 RoomEntered（_roomViews 已填充，OnRoomEntered 可揭露迷雾）
            dm.TransitToRoom(dm.CurrentRoom?.Id ?? dm.CurrentMap?.StartRoomId ?? "");

            // 7. 广播 DungeonReady 触发：
            //    DoorTransitCoordinator.BuildDoorConnections（门连接）
            //    RoomBehaviorOrchestrator（StartRoom.OnEnter）
            //    FloorMinimapController（小地图刷新）
            //    DungeonManager（下一层预加载）
            EventCenter.Broadcast(GameEventType.DungeonReady, new DungeonReadyEvent());

            // 8. 传送玩家到 StartRoom（_roomViews 已填充，TryGetRoomView 成功）
            TeleportPlayerToStartRoom();

            // 6. 亮屏 FadeIn
            yield return StartCoroutine(FadeScreen(fadeInDuration));

            // 7. 恢复玩家
            if (player != null)
                player.SetActive(true);

            // 8. 停用 Portal（回到 Prefab 中的默认非活跃状态，等待下次复用）
            if (portal != null)
                portal.Deactivate();

            // 9. 广播 FloorCompleted
            EventCenter.Broadcast(GameEventType.FloorCompleted, new FloorCompletedEvent
            {
                FromFloorIndex = fromFloor,
                ToFloorIndex = targetFloor
            });

            _inTransit = false;
        }

        /// <summary>
        /// 传送玩家到当前层 StartRoom 的入口位置
        /// </summary>
        private static void TeleportPlayerToStartRoom()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[PortalTransitCoordinator] 未找到 Player，跳过传送");
                return;
            }

            var dm = DungeonManager.Instance;
            if (dm?.CurrentRoom == null) return;

            // 优先从 RoomView 获取位置，失败时直接从房间数据计算世界坐标
            Vector3 entryPos;
            var viewManager = FindFirstObjectByType<DungeonViewManager>();
            if (viewManager != null && viewManager.TryGetRoomView(dm.CurrentRoom.Id, out var roomView))
            {
                entryPos = roomView.transform.position;
                var spawnPoints = roomView.GetSpawnPoints(SpawnType.Player);
                if (spawnPoints != null && spawnPoints.Count > 0)
                    entryPos = spawnPoints[0].transform.position;
            }
            else
            {
                // 回退：直接从房间数据计算中心世界坐标
                entryPos = CalculateRoomCenterWorld(dm.CurrentRoom);
                Debug.LogWarning($"[PortalTransitCoordinator] 未找到 StartRoom RoomView，使用回退坐标: {entryPos}");
            }

            player.transform.position = entryPos;
        }

        /// <summary>
        /// 从 RoomInstance 数据直接计算房间世界中心坐标（不依赖 RoomView）
        /// </summary>
        private static Vector3 CalculateRoomCenterWorld(RoomInstance room)
        {
            if (room?.Cells == null || room.Cells.Count == 0)
                return Vector3.zero;

            float size = DungeonViewManager.CellWorldSize;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < room.Cells.Count; i++)
                sum += new Vector2(room.Cells[i].x + 0.5f, room.Cells[i].y + 0.5f) * size;
            Vector2 center = sum / room.Cells.Count;
            return new Vector3(center.x, center.y, 0f);
        }

        /// <summary>
        /// 简易屏幕过渡等待（后续可接入 UI Canvas 实现真实黑屏效果）
        /// </summary>
        private static IEnumerator FadeScreen(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
