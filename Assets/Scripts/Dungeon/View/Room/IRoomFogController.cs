namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 房间迷雾控制器接口，抽象房间可见性的视觉表现。
    /// MVP 实现使用 SetActive + TilemapRenderer.color，后续可替换为 Shader 或 Overlay 方案。
    /// </summary>
    public interface IRoomFogController
    {
        /// <summary>
        /// 应用指定的可见性状态到房间视觉表现
        /// </summary>
        /// <param name="visibility">目标可见性状态</param>
        void ApplyVisibility(RoomVisibility visibility);
    }
}
