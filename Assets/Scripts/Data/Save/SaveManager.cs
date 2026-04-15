using System.IO;
using UnityEngine;

namespace RogueDungeon.Data.Save
{
    /// <summary>
    /// 存档管理器，提供泛型 Save/Load/HasSave/DeleteSave 操作。
    /// 使用 JsonUtility 序列化，存储路径为 Application.persistentDataPath。
    /// </summary>
    public static class SaveManager
    {
        private const string FileExtension = ".json";

        /// <summary>
        /// 将 ISaveData 数据序列化为 JSON 并写入文件
        /// </summary>
        public static void Save<T>(string key, T data) where T : ISaveData
        {
            string path = GetFilePath(key);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// 将任意可序列化数据写入文件（用于中途存档等非 ISaveData 场景）
        /// </summary>
        public static void SaveRaw<T>(string key, T data)
        {
            string path = GetFilePath(key);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// 从文件加载 ISaveData 数据。若文件不存在或版本不匹配则返回默认值。
        /// </summary>
        public static T Load<T>(string key) where T : ISaveData, new()
        {
            string path = GetFilePath(key);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] 存档不存在: {key}，返回默认值");
                return new T();
            }

            string json = File.ReadAllText(path);
            T data = JsonUtility.FromJson<T>(json);

            T expected = new T();
            if (data.SaveVersion != expected.SaveVersion)
            {
                Debug.LogWarning(
                    $"[SaveManager] 存档版本不匹配: {key}（文件版本={data.SaveVersion}，预期版本={expected.SaveVersion}），返回默认值");
                return expected;
            }

            return data;
        }

        /// <summary>
        /// 从文件加载任意可序列化数据（用于中途存档等非 ISaveData 场景）
        /// </summary>
        public static T LoadRaw<T>(string key) where T : new()
        {
            string path = GetFilePath(key);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] 存档不存在: {key}，返回默认值");
                return new T();
            }

            string json = File.ReadAllText(path);
            T data = JsonUtility.FromJson<T>(json);

            return data;
        }

        /// <summary>
        /// 检查指定 key 的存档是否存在
        /// </summary>
        public static bool HasSave(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        /// <summary>
        /// 删除指定 key 的存档文件
        /// </summary>
        public static void DeleteSave(string key)
        {
            string path = GetFilePath(key);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                Debug.LogWarning($"[SaveManager] 尝试删除不存在的存档: {key}");
            }
        }

        /// <summary>
        /// 获取存档文件的完整路径
        /// </summary>
        private static string GetFilePath(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + FileExtension);
        }
    }
}
