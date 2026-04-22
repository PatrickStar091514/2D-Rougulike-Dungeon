namespace RogueDungeon.Core.Buff
{
    /// <summary>
    /// 属性修改方式枚举，区分加法修改与百分比修改
    /// </summary>
    public enum ModifyType
    {
        Flat    = 0, // 加法修改（直接加减固定值）
        Percent = 1  // 百分比修改（乘以百分比系数）
    }
}
