namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// RunManager 在创建或恢复 Run 后广播的事件 Payload
    /// </summary>
    public struct RunReadyEvent
    {
        public RunState Run; // 当前 Run 状态
    }
}
