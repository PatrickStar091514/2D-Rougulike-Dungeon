namespace RogueDungeon.Core.Buff
{
    /// <summary>
    /// 属性类型枚举，标识 Buff 可修改的角色属性
    /// </summary>
    public enum StatType
    {
        MaxHP       = 0, // 生命上限
        Attack      = 1, // 攻击力
        Defense     = 2, // 防御力
        MoveSpeed   = 3, // 移动速度
        AttackSpeed = 4, // 攻击速度
        CritRate    = 5, // 暴击率
        CritDamage  = 6  // 暴击伤害
    }
}
