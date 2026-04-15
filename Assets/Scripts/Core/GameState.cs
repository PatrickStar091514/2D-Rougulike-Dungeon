namespace RogueDungeon.Core
{
    /// <summary>
    /// 游戏状态枚举，定义 Roguelike 主流程的所有阶段
    /// </summary>
    public enum GameState
    {
        Boot,           // 启动初始化
        Hub,            // 大厅/准备界面
        RunInit,        // 新 Run 初始化
        RoomPlaying,    // 房间战斗中
        RoomClear,      // 房间已清空
        RewardSelect,   // 奖励选择
        BossPlaying,    // Boss 战斗中
        RunEnd          // Run 结束（死亡或通关）
    }
}
