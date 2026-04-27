using System;

namespace RogueDungeon.Core.Buff
{
    /// <summary>
    /// Buff 运行时快照，由 BuffConfigSO 创建，承载事件 Payload 中消费者所需的所有信息。
    /// 事件消费者无需反向查找 BuffPoolSO / BuffConfigSO。
    /// </summary>
    [Serializable]
    public class BuffSnapshot
    {
        public string BuffId;
        public string DisplayName;
        public string Description;
        public Rarity Rarity;
        public DurationType Duration;
        public float DurationValue;
        public float DecayRate;
        public int MaxStack;
        public StatModifier[] Modifiers;

        // BuffDrop 渲染参数
        public string DropSortingLayer;
        public float DropSpriteScale;
        public float DropColliderRadius;
    }
}
