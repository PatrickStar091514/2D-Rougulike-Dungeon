using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 门转场协调器。监听 DungeonReady 建立门连接并注册回调，
    /// 监听 RoomCleared 解锁门，编排完整的 Transit 协程流程。
    /// 场景级 MonoBehaviour，与 DungeonViewManager 同场景挂载。
    /// </summary>
    public class DoorTransitCoordinator : MonoBehaviour
    {
        private const float MinEntryOffsetDistance = 0.1f;

        /// <summary>全局输入启用标志，玩家移动脚本需检查此属性</summary>
        public static bool InputEnabled { get; private set; } = true;

        [SerializeField] private DungeonViewManager _viewManager; // 视图管理器引用
        [SerializeField] private Transform _playerTransform;      // 玩家 Transform
        [SerializeField] private float _cameraWaitTimeout = 1f;   // 相机滑移超时（秒）
        [Header("Transit Spawn")]
        [Min(0.1f)]
        [Tooltip("玩家传送到目标门后，沿“目标门朝向的反方向（房间内侧）”移动的偏移距离。最小值 0.1。")]
        [SerializeField] private float _entryOffsetDistance = 1.5f; // 入场偏移距离（全方向共用，朝房间内侧）

        private bool _isTransiting; // Transit 防重入锁
        private DungeonCamera _dungeonCamera; // 缓存相机引用

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (_viewManager == null)
            {
                Debug.LogError("[DoorTransitCoordinator] viewManager 未赋值，组件已禁用");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
            ResetTransitState();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnregisterEvents();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[DEBUG] DoorTransitCoordinator.OnSceneLoaded scene={scene.name} mode={mode}");
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents();
            EventCenter.AddListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.AddListener<RewardClaimedEvent>(
                GameEventType.RewardClaimed, OnRewardClaimed);
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.RemoveListener<RewardClaimedEvent>(
                GameEventType.RewardClaimed, OnRewardClaimed);
        }

        /// <summary>
        /// 响应 DungeonReady：建立门双向连接、注册回调、起始房间门解锁
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            _dungeonCamera = FindFirstObjectByType<DungeonCamera>();
            BuildDoorConnections();
        }

        /// <summary>
        /// 响应 RewardClaimed（奖励拾取后）：解锁对应房间的所有门
        /// </summary>
        private void OnRewardClaimed(RewardClaimedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.RoomId))
            {
                Debug.LogWarning("[DoorTransitCoordinator] RewardClaimed 事件 RoomId 为空");
                return;
            }

            if (!_viewManager.TryGetRoomView(evt.RoomId, out var roomView))
            {
                Debug.LogWarning($"[DoorTransitCoordinator] RewardClaimed: 未找到房间视图 {evt.RoomId}");
                return;
            }

            foreach (var door in roomView.ActiveDoors)
            {
                if (door.State == DoorState.Locked)
                    door.Unlock();
            }
        }

        /// <summary>
        /// 接收门触发回调，启动 Transit 协程
        /// </summary>
        /// <param name="sourceDoor">触发的源门</param>
        public void RequestTransit(DoorView sourceDoor)
        {
            if (sourceDoor == null)
            {
                Debug.LogError("[DoorTransitCoordinator] RequestTransit 失败: sourceDoor 为 null");
                return;
            }

            if (_isTransiting)
            {
                Debug.Log("[DoorTransitCoordinator] Transit 进行中，忽略重复请求");
                return;
            }

            if (sourceDoor.State != DoorState.Unlocked)
            {
                Debug.LogWarning($"[DoorTransitCoordinator] RequestTransit 忽略: 门状态为 {sourceDoor.State}");
                return;
            }

            if (sourceDoor.ConnectedDoor == null)
            {
                Debug.LogError($"[DoorTransitCoordinator] 门 {sourceDoor.Slot} 的 ConnectedDoor 为 null，取消 Transit");
                return;
            }

            if (sourceDoor.ConnectedDoor.OwnerRoom == null)
            {
                Debug.LogError("[DoorTransitCoordinator] ConnectedDoor.OwnerRoom 为 null，取消 Transit");
                return;
            }

            if (DungeonManager.Instance == null)
            {
                Debug.LogError("[DoorTransitCoordinator] DungeonManager.Instance 为 null，取消 Transit");
                return;
            }

            StartCoroutine(TransitCoroutine(sourceDoor));
        }

        /// <summary>
        /// 完整 Transit 协程：10 步流程
        /// </summary>
        private IEnumerator TransitCoroutine(DoorView sourceDoor)
        {
            _isTransiting = true;
            InputEnabled = false;
            bool transitStarted = false;

            try
            {
                // Step 1: 源门进入 Transit 状态
                sourceDoor.BeginTransit();
                if (sourceDoor.State != DoorState.Transit)
                {
                    Debug.LogError("[DoorTransitCoordinator] BeginTransit 未成功，取消 Transit");
                    yield break;
                }
                transitStarted = true;

                // Step 3: 获取目标门与房间
                var targetDoor = sourceDoor.ConnectedDoor;
                if (targetDoor == null || targetDoor.OwnerRoom == null)
                {
                    Debug.LogError("[DoorTransitCoordinator] 目标门或目标房间为空，取消 Transit");
                    yield break;
                }
                var targetRoomId = targetDoor.OwnerRoom.RoomId;

                // Step 4: 先将玩家放置到目标门入口偏移位置
                // 保证 RoomEntered 后续监听逻辑读取到的是进入后位置
                TeleportPlayer(targetDoor);

                // Step 5: 调用 DungeonManager 切换房间（触发 RoomEntered 事件）
                var dungeonManager = DungeonManager.Instance;
                if (dungeonManager == null)
                {
                    Debug.LogError("[DoorTransitCoordinator] DungeonManager.Instance 为 null，取消 Transit");
                    yield break;
                }
                dungeonManager.TransitToRoom(targetRoomId);

                // Step 6-7: 迷雾由 DungeonViewManager 响应 RoomEntered 自动处理

                // Step 8: 等待相机滑移完成（带超时）
                if (_dungeonCamera != null)
                {
                    float elapsed = 0f;
                    while (_dungeonCamera.IsTransitioning && elapsed < _cameraWaitTimeout)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (elapsed >= _cameraWaitTimeout)
                        Debug.LogWarning("[DoorTransitCoordinator] 相机滑移超时，强制完成 Transit");
                }
                else
                {
                    yield return null;
                }
            }
            finally
            {
                if (transitStarted && sourceDoor != null && sourceDoor.State == DoorState.Transit)
                {
                    // Step 10: 源门恢复 Unlocked
                    sourceDoor.EndTransit();
                }

                // Step 9 + 状态兜底：恢复输入并释放锁
                ResetTransitState();
            }
        }

        /// <summary>
        /// 传送玩家到目标门入口偏移位置。
        /// 优先以 DoorTrigger 碰撞体的房间内侧边缘作为锚点，再沿内侧方向偏移。
        /// 若目标门无 Collider2D，则回退为以门 Transform 位置为锚点偏移。
        /// 示例：从 A(E) 进入 B(W) 时，会在 B 的 W 门向房间内侧（+X）偏移。
        /// </summary>
        /// <param name="targetDoor">目标门视图</param>
        public void TeleportPlayer(DoorView targetDoor)
        {
            if (targetDoor == null)
            {
                Debug.LogError("[DoorTransitCoordinator] targetDoor 为空，无法传送");
                return;
            }

            if (_playerTransform == null)
            {
                Debug.LogError("[DoorTransitCoordinator] playerTransform 未赋值，无法传送");
                return;
            }

            float safeOffsetDistance = GetValidatedEntryOffsetDistance();
            Vector2 targetPos;
            if (targetDoor.TryGetComponent<Collider2D>(out var doorTriggerCollider))
            {
                targetPos = CalculateSpawnPosition(doorTriggerCollider.bounds, targetDoor.Slot.Direction, safeOffsetDistance);
            }
            else
            {
                var entryOffset = CalculateEntryOffset(targetDoor.Slot.Direction, safeOffsetDistance);
                targetPos = (Vector2)targetDoor.transform.position + entryOffset;
            }

            _playerTransform.position = new Vector3(targetPos.x, targetPos.y, _playerTransform.position.z);
        }

        /// <summary>
        /// 基于 DoorTrigger 碰撞体的房间内侧边缘计算传送落点。
        /// </summary>
        /// <param name="doorTriggerBounds">DoorTrigger 碰撞体 Bounds</param>
        /// <param name="targetDoorDirection">目标门朝向（门外方向）</param>
        /// <param name="entryOffsetDistance">沿门内方向的偏移距离（最小 0.1）</param>
        /// <returns>玩家出生点世界坐标</returns>
        public static Vector2 CalculateSpawnPosition(Bounds doorTriggerBounds, Direction targetDoorDirection, float entryOffsetDistance)
        {
            float safeDistance = Mathf.Max(MinEntryOffsetDistance, entryOffsetDistance);
            Vector2 interiorDirection = -(Vector2)targetDoorDirection.ToVector2Int();
            float interiorEdgeExtent = Mathf.Abs(interiorDirection.x) > 0f
                ? doorTriggerBounds.extents.x
                : doorTriggerBounds.extents.y;
            Vector2 interiorEdgeAnchor = (Vector2)doorTriggerBounds.center + interiorDirection * interiorEdgeExtent;
            return interiorEdgeAnchor + interiorDirection * safeDistance;
        }

        /// <summary>
        /// 计算门入口偏移向量（门朝向的反方向 × 配置偏移距离）
        /// </summary>
        /// <param name="targetDoorDirection">目标门朝向（如 W 门即 West）</param>
        /// <param name="entryOffsetDistance">偏移距离（最小 0.1）</param>
        /// <returns>世界空间偏移向量</returns>
        public static Vector2 CalculateEntryOffset(Direction targetDoorDirection, float entryOffsetDistance)
        {
            float safeDistance = Mathf.Max(MinEntryOffsetDistance, entryOffsetDistance);
            // 目标门朝向表示“门外方向”，传送落点应朝“门内方向”，因此取反向量。
            return -(Vector2)targetDoorDirection.ToVector2Int() * safeDistance;
        }

        /// <summary>
        /// 获取经过最小值保护后的入场偏移距离。
        /// </summary>
        private float GetValidatedEntryOffsetDistance()
        {
            if (_entryOffsetDistance < MinEntryOffsetDistance)
            {
                Debug.LogWarning($"[DoorTransitCoordinator] entryOffsetDistance({_entryOffsetDistance}) 低于最小值 {MinEntryOffsetDistance}，已按最小值处理");
            }
            return Mathf.Max(MinEntryOffsetDistance, _entryOffsetDistance);
        }

        /// <summary>
        /// 扫描所有房间建立门双向连接、注册回调、起始房间门解锁
        /// </summary>
        private void BuildDoorConnections()
        {
            if (_viewManager == null)
            {
                Debug.LogError("[DoorTransitCoordinator] viewManager 未赋值");
                return;
            }

            foreach (var roomView in _viewManager.AllRoomViews)
            {
                foreach (var door in roomView.ActiveDoors)
                {
                    door.ConnectedDoor = null;

                    // 建立双向连接（ConnectedRoomId + ExpectedRemoteDoor 精确匹配）
                    if (!string.IsNullOrEmpty(door.ConnectedRoomId))
                    {
                        if (_viewManager.TryGetRoomView(door.ConnectedRoomId, out var connectedRoomView))
                        {
                            var matchingDoor = FindMatchingDoor(connectedRoomView, roomView.RoomId, door.ExpectedRemoteDoor);
                            if (matchingDoor != null)
                            {
                                door.ConnectedDoor = matchingDoor;
                                matchingDoor.ConnectedDoor = door;
                            }
                            else
                            {
                                Debug.LogError(
                                    $"[DoorTransitCoordinator] 门精确匹配失败: sourceRoom={roomView.RoomId}, " +
                                    $"sourceSlot={door.Slot}, targetRoom={door.ConnectedRoomId}, expectedRemoteSlot={door.ExpectedRemoteDoor}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[DoorTransitCoordinator] 门精确匹配失败: 未找到目标房间 {door.ConnectedRoomId}");
                        }
                    }

                    // 注册回调
                    door.OnPlayerEntered -= RequestTransit;
                    door.OnPlayerEntered += RequestTransit;
                }

                // 起始房间门直接 Unlock
                if (roomView.Room != null && roomView.Room.Type == RoomType.Start)
                {
                    foreach (var door in roomView.ActiveDoors)
                    {
                        if (door.State == DoorState.Locked)
                            door.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// 在目标房间中查找：连接回 sourceRoomId 且门位等于 expectedRemoteSlot 的门
        /// </summary>
        private static DoorView FindMatchingDoor(RoomView targetRoom, string sourceRoomId, DoorSlot expectedRemoteSlot)
        {
            foreach (var door in targetRoom.ActiveDoors)
            {
                if (door.ConnectedRoomId != sourceRoomId)
                    continue;
                if (!door.Slot.Equals(expectedRemoteSlot))
                    continue;
                return door;
            }
            return null;
        }

        /// <summary>
        /// 重置 Transit 状态（异常安全兜底）
        /// </summary>
        private void ResetTransitState()
        {
            _isTransiting = false;
            InputEnabled = true;
        }
    }
}
