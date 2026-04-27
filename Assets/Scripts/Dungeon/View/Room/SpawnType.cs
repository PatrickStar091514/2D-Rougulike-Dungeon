namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 生成点类型枚举，标识不同的实体生成用途
    /// </summary>
    public enum SpawnType
    {
        Player,     // 玩家出生点
        Enemy,      // 敌人生成点
        Item,       // 物品生成点
        NPC,        // NPC 生成点
        Decoration, // 装饰物生成点
        Reward      // 奖励生成点
    }
}
