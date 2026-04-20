namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 房间可见性状态枚举，控制迷雾系统的三态显示
    /// </summary>
    public enum RoomVisibility
    {
        Hidden,      // 完全不可见（SetActive false）
        Silhouette,  // 轮廓可见（暗色调）
        Revealed     // 完全可见
    }
}
