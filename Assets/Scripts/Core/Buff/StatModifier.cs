using System;
using UnityEngine;

namespace RogueDungeon.Core.Buff
{
    /// <summary>
    /// 属性修改器，描述对单个属性的一次修改（属性类型 + 修改方式 + 数值）
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("修改的属性类型")]
        public StatType Stat;     // 目标属性
        [Tooltip("修改方式：固定值/百分比")]
        public ModifyType Type;   // 修改方式（Flat / Percent）
        [Tooltip("修改数值（百分比填小数，比如20%填0.2）")]
        public float Value;       // 修改值
    }
}
