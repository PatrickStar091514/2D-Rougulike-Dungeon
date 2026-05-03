using UnityEngine;

namespace RogueDungeon.Dungeon.Portal
{
    /// <summary>
    /// 传送门状态枚举
    /// </summary>
    public enum PortalState
    {
        Loading, // 下一层预加载中，不可交互（灰色）
        Ready    // 预加载完成，可交互（亮色）
    }

    /// <summary>
    /// 跨层传送门组件。挂载在房间 Prefab 的 Portal 子物体上。
    /// Boss 击杀后由 PortalManager 激活，预加载完成后变为 Ready，
    /// Player 触碰时触发跨层过渡。过渡完成后自行停用。
    /// </summary>
    /// <remarks>
    /// <b>Prefab 约定</b>：房间 Prefab 内应包含名为 "Portal" 的子 GameObject，
    /// 挂载此组件、SpriteRenderer 和 CircleCollider2D(isTrigger)。
    /// 该子物体初始 SetActive(false)。
    /// </remarks>
    public class Portal : MonoBehaviour
    {
        [SerializeField] private PortalState _state = PortalState.Loading; // 当前状态
        [SerializeField] private SpriteRenderer _spriteRenderer; // 传送门 Sprite（Loading=灰，Ready=亮）

        /// <summary>
        /// 目标楼层索引（由 PortalManager 激活时设置）
        /// </summary>
        public int TargetFloorIndex { get; private set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public PortalState State => _state;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 激活传送门（PortalManager 在 Boss 击杀后调用）
        /// </summary>
        /// <param name="targetFloorIndex">目标楼层索引</param>
        public void Activate(int targetFloorIndex)
        {
            TargetFloorIndex = targetFloorIndex;
            _state = PortalState.Loading;
            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.gray;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 设置为就绪状态（预加载完成后由 PortalManager 调用）
        /// </summary>
        public void SetReady()
        {
            _state = PortalState.Ready;
            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;
        }

        /// <summary>
        /// 过渡完成后停用传送门（由 PortalTransitCoordinator 调用）
        /// </summary>
        public void Deactivate()
        {
            _state = PortalState.Loading;
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != PortalState.Ready) return;
            if (!other.CompareTag("Player")) return;

            var coordinator = FindFirstObjectByType<PortalTransitCoordinator>();
            if (coordinator != null)
                coordinator.StartTransit(this);
        }
    }
}
