using System.Collections.Generic;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 生成点提供者接口，后续生成系统通过此接口获取指定类型的生成点列表
    /// </summary>
    public interface ISpawnPointProvider
    {
        /// <summary>
        /// 获取指定类型的生成点列表
        /// </summary>
        /// <param name="type">生成点类型</param>
        /// <returns>匹配类型的生成点只读列表</returns>
        IReadOnlyList<SpawnPoint> GetSpawnPoints(SpawnType type);
    }
}
