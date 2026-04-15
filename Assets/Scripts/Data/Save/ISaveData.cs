namespace RogueDungeon.Data.Save
{
    /// <summary>
    /// 存档数据接口，所有持久化数据结构必须实现此接口以支持版本化管理
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        /// 存档数据版本号，用于存档迁移和兼容性检查
        /// </summary>
        int SaveVersion { get; }
    }
}
