using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 门视图组件，挂载在 DoorTrigger GameObject 上。
    /// 维护 DoorState 三态模型，处理碰撞触发检测，提供视觉反馈。
    /// </summary>
    public class DoorView : MonoBehaviour
    {
        [SerializeField] private Tilemap _doorTilemap; // 门 Tilemap（用于视觉颜色反馈）

        private RoomView _ownerRoom;       // 所属房间视图
        private DoorSlot _slot;            // 门位标识
        private string _connectedRoomId;   // 连接的目标房间 Id
        private DoorSlot _expectedRemoteDoor; // 期望连接的目标房间门位
        private DoorState _state;          // 当前门状态

        /// <summary>
        /// 所属的 RoomView
        /// </summary>
        public RoomView OwnerRoom => _ownerRoom;

        /// <summary>
        /// 门位标识（含 CellOffset 和 Direction）
        /// </summary>
        public DoorSlot Slot => _slot;

        /// <summary>
        /// 连接的目标房间 Id
        /// </summary>
        public string ConnectedRoomId => _connectedRoomId;

        /// <summary>
        /// 期望连接的目标房间门位
        /// </summary>
        public DoorSlot ExpectedRemoteDoor => _expectedRemoteDoor;

        /// <summary>
        /// 当前门状态
        /// </summary>
        public DoorState State => _state;

        /// <summary>
        /// 双向连接的对端门视图（由 DoorTransitCoordinator 建立）
        /// </summary>
        public DoorView ConnectedDoor { get; set; }

        /// <summary>
        /// 玩家进入门触发区回调（仅 Unlocked 状态触发）
        /// </summary>
        public event Action<DoorView> OnPlayerEntered;

        /// <summary>
        /// 初始化门视图，绑定所属房间、门位和连接信息
        /// </summary>
        /// <param name="owner">所属房间视图</param>
        /// <param name="slot">门位标识</param>
        /// <param name="connectedRoomId">连接的目标房间 Id</param>
        /// <param name="remoteDoor">目标房间期望门位</param>
        public void Initialize(RoomView owner, DoorSlot slot, string connectedRoomId, DoorSlot remoteDoor)
        {
            _ownerRoom = owner;
            _slot = slot;
            _connectedRoomId = connectedRoomId;
            _expectedRemoteDoor = remoteDoor;
            _state = DoorState.Locked;
            UpdateVisual();
        }

        /// <summary>
        /// 锁定门。仅允许 Unlocked → Locked 转换
        /// </summary>
        public void Lock()
        {
            if (_state != DoorState.Unlocked)
            {
                Debug.LogWarning($"[DoorView] Lock() 失败: 当前状态为 {_state}，仅 Unlocked 可锁定");
                return;
            }
            _state = DoorState.Locked;
            UpdateVisual();
        }

        /// <summary>
        /// 解锁门。仅允许 Locked → Unlocked 转换
        /// </summary>
        public void Unlock()
        {
            if (_state != DoorState.Locked)
            {
                Debug.LogWarning($"[DoorView] Unlock() 失败: 当前状态为 {_state}，仅 Locked 可解锁");
                return;
            }
            _state = DoorState.Unlocked;
            UpdateVisual();
        }

        /// <summary>
        /// 进入传送状态。仅允许 Unlocked → Transit 转换
        /// </summary>
        public void BeginTransit()
        {
            if (_state != DoorState.Unlocked)
            {
                Debug.LogWarning($"[DoorView] BeginTransit() 失败: 当前状态为 {_state}，仅 Unlocked 可开始传送");
                return;
            }
            _state = DoorState.Transit;
            UpdateVisual();
        }

        /// <summary>
        /// 结束传送，恢复 Unlocked 状态
        /// </summary>
        public void EndTransit()
        {
            _state = DoorState.Unlocked;
            UpdateVisual();
        }

        /// <summary>
        /// 碰撞触发检测：仅 Unlocked 状态 + Player Tag 时触发回调
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != DoorState.Unlocked) return;
            if (!other.CompareTag("Player")) return;
            OnPlayerEntered?.Invoke(this);
        }

        /// <summary>
        /// 根据当前 DoorState 更新门 Tilemap 颜色
        /// </summary>
        private void UpdateVisual()
        {
            if (_doorTilemap == null) return;

            _doorTilemap.color = _state switch
            {
                DoorState.Locked => new Color(1f, 0.3f, 0.3f),   // 红色
                DoorState.Unlocked => new Color(0.3f, 1f, 0.3f), // 绿色
                DoorState.Transit => new Color(0.5f, 0.5f, 0.5f), // 灰色
                _ => Color.white
            };
        }
    }
}
