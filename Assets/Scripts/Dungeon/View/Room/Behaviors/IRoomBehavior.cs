namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 房间行为策略接口，定义房间生命周期回调。
    /// 实现类为 POCO（非 MonoBehaviour），由 RoomView 工厂创建并持有。
    /// </summary>
    public interface IRoomBehavior
    {
        /// <summary>玩家进入房间时调用。</summary>
        void OnEnter(RoomView room);

        /// <summary>房间敌人全部清除时调用。</summary>
        void OnClear(RoomView room);

        /// <summary>玩家离开房间时调用。</summary>
        void OnExit(RoomView room);
    }
}
