using System;
using RogueDungeon.Core.Buff;

namespace RogueDungeon.Data.Config
{
    /// <summary>
    /// 属性修改器，描述对单个属性的一次修改（属性类型 + 修改方式 + 数值）
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType Stat;     // 目标属性
        public ModifyType Type;   // 修改方式（Flat / Percent）
        public float Value;       // 修改值
    }
}
