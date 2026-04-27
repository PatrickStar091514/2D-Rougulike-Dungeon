using System;
using UnityEngine;
using RogueDungeon.Core.Buff;
using RogueDungeon.Core.Pool;
using RogueDungeon.Data.Runtime;

namespace RogueDungeon.Dungeon.Reward
{
    /// <summary>
    /// Buff 掉落物。玩家进入触发器后立即尝试应用 Buff，并通知互斥组与生成器。
    /// </summary>
    public class BuffDrop : MonoBehaviour, IPoolable
    {
        [SerializeField] private string buffId; // 掉落 Buff ID
        [SerializeField] private SpriteRenderer iconRenderer; // 图标渲染器
        [SerializeField] private CircleCollider2D triggerCollider; // 拾取触发器
        [SerializeField] private ExclusivePickupGroup group; // 所属互斥组
        [SerializeField] private bool isPicked; // 本实例是否已被处理
        [SerializeField] private string visibleSortingLayer = "Drop"; // 显示排序层（由 SO 下发）
        [SerializeField] private int visibleSortingOrder = 900; // 显示优先级（避免被房间地表遮挡）
        [SerializeField] private float defaultSpriteScale = 1f; // 默认图标缩放
        [SerializeField] private float defaultColliderRadius = 0.5f; // 默认拾取半径

        private Action<BuffDrop> _onPicked; // 拾取成功回调

        /// <summary>
        /// 当前掉落的 Buff ID。
        /// </summary>
        public string BuffId => buffId;

        /// <summary>
        /// 初始化掉落物数据。
        /// </summary>
        /// <param name="snapshot">Buff 运行时快照</param>
        /// <param name="icon">显示图标</param>
        /// <param name="pickupGroup">互斥组（可为 null）</param>
        /// <param name="onPicked">拾取回调</param>
        public void Init(
            BuffSnapshot snapshot,
            Sprite icon,
            ExclusivePickupGroup pickupGroup,
            Action<BuffDrop> onPicked)
        {
            if (snapshot == null) return;

            buffId = snapshot.BuffId;
            group = pickupGroup;
            _onPicked = onPicked;
            isPicked = false;

            string sortingLayer = snapshot.DropSortingLayer;
            float spriteScale = snapshot.DropSpriteScale;
            float colliderRadius = snapshot.DropColliderRadius;

            if (!string.IsNullOrEmpty(sortingLayer))
                visibleSortingLayer = sortingLayer;

            if (iconRenderer == null)
                iconRenderer = GetComponent<SpriteRenderer>();
            if (triggerCollider == null)
                triggerCollider = GetComponent<CircleCollider2D>();

            if (iconRenderer != null)
            {
                iconRenderer.sprite = icon;
                iconRenderer.enabled = icon != null;
                if (!string.IsNullOrEmpty(visibleSortingLayer))
                    iconRenderer.sortingLayerName = visibleSortingLayer;
                iconRenderer.sortingOrder = Mathf.Max(iconRenderer.sortingOrder, visibleSortingOrder);
            }

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.enabled = true;
                triggerCollider.radius = Mathf.Max(0.01f, colliderRadius > 0f ? colliderRadius : defaultColliderRadius);
            }

            float finalScale = Mathf.Max(0.01f, spriteScale > 0f ? spriteScale : defaultSpriteScale);
            var scale = transform.localScale;
            transform.localScale = new Vector3(finalScale, finalScale, scale.z == 0f ? 1f : scale.z);
        }

        /// <summary>
        /// 更新掉落物所属互斥组。
        /// </summary>
        public void SetGroup(ExclusivePickupGroup pickupGroup)
        {
            group = pickupGroup;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPicked) return;
            if (other == null || !other.CompareTag("Player")) return;

            isPicked = true;

            bool applied = BuffManager.Instance != null && BuffManager.Instance.ApplyBuff(buffId, "RewardDrop");
            if (!applied)
                Debug.LogWarning($"[BuffDrop] 拾取失败，ApplyBuff 返回 false: {buffId}");

            _onPicked?.Invoke(this);
            group?.OnMemberPicked(this);
            ReleaseToPool();
        }

        /// <summary>
        /// 由互斥组触发的强制回收（未被玩家拾取）。
        /// </summary>
        public void ReleaseFromGroup()
        {
            if (isPicked) return;
            isPicked = true;
            ReleaseToPool();
        }

        /// <inheritdoc />
        public void OnPoolGet()
        {
            isPicked = false;
            if (triggerCollider == null)
                triggerCollider = GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
                triggerCollider.enabled = true;
        }

        /// <inheritdoc />
        public void OnPoolRelease()
        {
            isPicked = false;
            group = null;
            _onPicked = null;
            buffId = string.Empty;
            if (iconRenderer != null)
                iconRenderer.sprite = null;
            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }

        private void ReleaseToPool()
        {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(RewardSpawner.PoolKey, gameObject);
                return;
            }

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }
    }
}
