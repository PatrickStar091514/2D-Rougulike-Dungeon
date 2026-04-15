using System.Collections.Generic;

namespace RogueDungeon.Data.Save
{
    /// <summary>
    /// 元成长存档结构，记录跨 Run 的永久进度数据
    /// </summary>
    [System.Serializable]
    public class MetaProgressSave : ISaveData
    {
        public int saveVersion = 1; // 存档版本号

        public List<string> UnlockedWeapons = new();          // 已解锁武器
        public int Currency;                                   // 元成长货币
        public List<SerializableKeyValue<string, int>> PermanentUpgrades = new(); // 永久升级（可序列化）
        public string LastRunSummary = string.Empty;           // 上局摘要

        /// <summary>
        /// 存档版本号（ISaveData 接口实现）
        /// </summary>
        public int SaveVersion => saveVersion;
    }
}
