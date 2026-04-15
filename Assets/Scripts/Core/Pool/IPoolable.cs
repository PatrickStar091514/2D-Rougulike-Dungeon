namespace RogueDungeon.Core.Pool
{
    /// <summary>
    /// 可池化对象接口，实现此接口的组件在对象池获取/回收时收到回调
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从对象池获取时调用，用于重置运行时状态
        /// </summary>
        void OnPoolGet();

        /// <summary>
        /// 回收到对象池时调用，用于清理运行时状态
        /// </summary>
        void OnPoolRelease();
    }
}
