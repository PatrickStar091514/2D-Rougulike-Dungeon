using System.Collections;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 地牢相机控制器。挂载在正交 Camera 上，提供：
    /// LateUpdate 玩家跟随 + 房间 AABB 边界钳制 + RoomEntered 快速滑移 + DungeonReady 首帧落位。
    /// </summary>
    public class DungeonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _followTarget; // 跟随目标（玩家 Transform）
        [SerializeField] private float _panDuration = 0.2f; // 滑移持续时间（秒）
        [SerializeField] private float _orthographicSize = 5f; // 正交相机尺寸（固定）

        private Camera _camera; // 缓存相机组件
        private Rect _currentBounds; // 当前房间世界 AABB
        private bool _isTransitioning; // 是否正在滑移中
        private Coroutine _panCoroutine; // 当前滑移协程引用
        private bool _initialized; // 是否已完成首帧初始化
        private bool _followTargetWarningLogged; // followTarget null 警告是否已输出

        /// <summary>
        /// 跟随目标 Transform（可通过代码或 Inspector 赋值）
        /// </summary>
        public Transform FollowTarget
        {
            get => _followTarget;
            set => _followTarget = value;
        }

        /// <summary>
        /// 是否正在执行房间切换滑移
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// 当前房间世界 AABB（只读，供测试使用）
        /// </summary>
        public Rect CurrentBounds => _currentBounds;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ApplyOrthographicSize();
        }

        private void OnValidate()
        {
            if (_orthographicSize < 0.01f)
                _orthographicSize = 0.01f;

            if (_camera == null)
                _camera = GetComponent<Camera>();

            ApplyOrthographicSize();
        }

        private void OnEnable()
        {
            EventCenter.AddListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.AddListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.RemoveListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
        }

        private void LateUpdate()
        {
            if (_isTransitioning || !_initialized) return;

            if (_followTarget == null)
            {
                if (!_followTargetWarningLogged)
                {
                    Debug.LogWarning("[DungeonCamera] followTarget 为 null，相机跟随已暂停");
                    _followTargetWarningLogged = true;
                }
                return;
            }

            _followTargetWarningLogged = false;
            var targetPos = (Vector2)_followTarget.position;
            var clamped = ClampPosition(targetPos, _currentBounds, _orthographicSize, _camera.aspect);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }

        /// <summary>
        /// 响应 DungeonReady 事件，执行首帧落位
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            SnapToInitialTarget();
        }

        /// <summary>
        /// 响应 RoomEntered 事件，启动滑移协程切换到新房间
        /// </summary>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room == null) return;

            var newBounds = CalculateRoomBounds(evt.Room);

            // 停止当前滑移（如有）
            if (_panCoroutine != null)
            {
                StopCoroutine(_panCoroutine);
                _panCoroutine = null;
            }

            _panCoroutine = StartCoroutine(PanToRoom(evt.Room, newBounds));
        }

        /// <summary>
        /// 首帧落位：优先对齐 followTarget，若为空则回退到 StartRoom 中心
        /// </summary>
        private void SnapToInitialTarget()
        {
            Vector2 target;
            Rect bounds;

            if (_followTarget != null)
            {
                target = _followTarget.position;
                var startRoom = GetStartRoom();
                bounds = startRoom != null
                    ? CalculateRoomBounds(startRoom)
                    : new Rect(target.x - 5, target.y - 5, 10, 10);
            }
            else
            {
                var startRoom = GetStartRoom();
                if (startRoom != null)
                {
                    bounds = CalculateRoomBounds(startRoom);
                    target = bounds.center;
                }
                else
                {
                    Debug.LogError("[DungeonCamera] 首帧落位失败: followTarget 和 StartRoom 均不可用");
                    _initialized = true;
                    return;
                }
            }

            _currentBounds = bounds;
            var clamped = ClampPosition(target, _currentBounds, _orthographicSize, _camera.aspect);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            _initialized = true;
        }

        /// <summary>
        /// 快速滑移协程：从当前位置 Lerp 到目标房间钳制位置
        /// </summary>
        private IEnumerator PanToRoom(RoomInstance room, Rect newBounds)
        {
            _isTransitioning = true;

            var targetPos = _followTarget != null
                ? (Vector2)_followTarget.position
                : newBounds.center;
            var endPos = ClampPosition(targetPos, newBounds, _orthographicSize, _camera.aspect);

            var startPos = (Vector2)transform.position;
            float elapsed = 0f;
            float duration = Mathf.Max(_panDuration, 0.01f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // SmoothStep
                var pos = Vector2.Lerp(startPos, endPos, t);
                transform.position = new Vector3(pos.x, pos.y, transform.position.z);
                yield return null;
            }

            transform.position = new Vector3(endPos.x, endPos.y, transform.position.z);
            _currentBounds = newBounds;
            _isTransitioning = false;
            _panCoroutine = null;
        }

        /// <summary>
        /// 计算房间世界空间 AABB。基于 RoomShapeUtil.GetCells 和 GridPosition × CellWorldSize
        /// </summary>
        /// <param name="room">房间实例</param>
        /// <returns>世界空间 Rect（x=left, y=bottom, width, height）</returns>
        public static Rect CalculateRoomBounds(RoomInstance room)
        {
            var cells = RoomShapeUtil.GetCells(room.Shape);
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var cell in cells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }

            float size = DungeonViewManager.CellWorldSize;
            float worldMinX = (minX * size) + (room.GridPosition.x * size);
            float worldMinY = (minY * size) + (room.GridPosition.y * size);
            float worldMaxX = ((maxX + 1) * size) + (room.GridPosition.x * size);
            float worldMaxY = ((maxY + 1) * size) + (room.GridPosition.y * size);

            return new Rect(worldMinX, worldMinY, worldMaxX - worldMinX, worldMaxY - worldMinY);
        }

        /// <summary>
        /// 将目标位置钳制在房间边界内。按轴独立判断：视口半宽大于半房间宽度时该轴居中
        /// </summary>
        /// <param name="target">目标世界位置</param>
        /// <param name="roomBounds">房间世界 AABB</param>
        /// <param name="orthoSize">正交相机尺寸（半高）</param>
        /// <param name="aspect">相机宽高比</param>
        /// <returns>钳制后的相机位置</returns>
        public static Vector2 ClampPosition(Vector2 target, Rect roomBounds, float orthoSize, float aspect)
        {
            float halfHeight = orthoSize;
            float halfWidth = orthoSize * aspect;

            float roomHalfWidth = roomBounds.width * 0.5f;
            float roomHalfHeight = roomBounds.height * 0.5f;

            float x, y;

            if (halfWidth >= roomHalfWidth)
            {
                x = roomBounds.center.x;
            }
            else
            {
                x = Mathf.Clamp(target.x, roomBounds.xMin + halfWidth, roomBounds.xMax - halfWidth);
            }

            if (halfHeight >= roomHalfHeight)
            {
                y = roomBounds.center.y;
            }
            else
            {
                y = Mathf.Clamp(target.y, roomBounds.yMin + halfHeight, roomBounds.yMax - halfHeight);
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// 获取起始房间实例（通过 DungeonManager Singleton）
        /// </summary>
        private static RoomInstance GetStartRoom()
        {
            if (DungeonManager.Instance == null || DungeonManager.Instance.CurrentMap == null)
                return null;
            return DungeonManager.Instance.CurrentMap.GetRoom(
                DungeonManager.Instance.CurrentMap.StartRoomId);
        }

        /// <summary>
        /// 同步 Inspector 配置到 Camera 组件
        /// </summary>
        private void ApplyOrthographicSize()
        {
            if (_camera == null) return;
            _camera.orthographic = true;
            _camera.orthographicSize = _orthographicSize;
        }
    }
}
