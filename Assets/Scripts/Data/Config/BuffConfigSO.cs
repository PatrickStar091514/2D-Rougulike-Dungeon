using UnityEngine;
using RogueDungeon.Core.Buff;

namespace RogueDungeon.Data.Config
{
    /// <summary>
    /// 单个 Buff 的配置数据，作为 ScriptableObject 在 Inspector 中编辑。
    /// 运行时只读，不可修改字段值。
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffConfig", menuName = "Data/Buff Config")]
    public class BuffConfigSO : ScriptableObject
    {
        [SerializeField] private string buffId;           // 唯一标识符
        [SerializeField] private string displayName;      // 显示名称
        [TextArea(2, 4)]
        [SerializeField] private string description;      // 效果描述
        [SerializeField] private Sprite icon;             // 图标（可为 null）
        [SerializeField] private Rarity rarity;           // 固有稀有度
        [SerializeField] private DurationType duration;   // 持续类型
        [SerializeField] private float durationValue;     // 持续值（Timed=秒, RoomScoped=房间数, Stack=最大层数）
        [Range(0f, 1f)]
        [SerializeField] private float decayRate = 0.7f;  // 指数递减系数（仅 Permanent 有效）
        [Min(0)]
        [SerializeField] private int maxStack;            // 最大叠加数（0=无限）
        [SerializeField] private string dropSortingLayer = "Drop"; // 掉落图标排序层
        [Min(0.01f)]
        [SerializeField] private float dropSpriteScale = 1f; // 掉落图标缩放
        [Min(0.01f)]
        [SerializeField] private float dropColliderRadius = 0.5f; // 掉落拾取半径
        [SerializeField] private StatModifier[] modifiers; // 属性修改列表

        public string BuffId => buffId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Rarity Rarity => rarity;
        public DurationType Duration => duration;
        public float DurationValue => durationValue;
        public float DecayRate => decayRate;
        public int MaxStack => maxStack;
        public string DropSortingLayer => string.IsNullOrEmpty(dropSortingLayer) ? "Drop" : dropSortingLayer;
        public float DropSpriteScale => Mathf.Max(0.01f, dropSpriteScale);
        public float DropColliderRadius => Mathf.Max(0.01f, dropColliderRadius);
        public StatModifier[] Modifiers => modifiers;
    }
}
