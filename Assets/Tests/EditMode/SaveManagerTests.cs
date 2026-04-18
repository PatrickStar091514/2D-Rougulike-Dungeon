using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Data;
using RogueDungeon.Data.Save;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// SaveManager 存档管理器的 EditMode 单元测试
    /// </summary>
    public class SaveManagerTests
    {
        private const string TestKey = "unit_test_save";
        private const string TestRawKey = "unit_test_raw";

        [SetUp]
        public void SetUp()
        {
            Cleanup(TestKey);
            Cleanup(TestRawKey);
        }

        [TearDown]
        public void TearDown()
        {
            Cleanup(TestKey);
            Cleanup(TestRawKey);
        }

        #region Save / Load (ISaveData)

        [Test]
        public void Save_And_Load_RoundTrip()
        {
            var data = new TestSaveData
            {
                Score = 42,
                PlayerName = "Hero"
            };

            SaveManager.Save(TestKey, data);
            var loaded = SaveManager.Load<TestSaveData>(TestKey);

            Assert.AreEqual(0, loaded.SaveVersion);
            Assert.AreEqual(42, loaded.Score);
            Assert.AreEqual("Hero", loaded.PlayerName);
        }

        [Test]
        public void Load_VersionMismatch_ReturnsDefault()
        {
            var data = new TestSaveData
            {
                SaveVersion = 99,
                Score = 42,
                PlayerName = "Hero"
            };

            SaveManager.Save(TestKey, data);
            var loaded = SaveManager.Load<TestSaveData>(TestKey);

            Assert.AreEqual(0, loaded.SaveVersion); // 返回默认值（版本不匹配）
            Assert.AreEqual(0, loaded.Score);
            Assert.IsNull(loaded.PlayerName);
        }

        [Test]
        public void Load_FileNotExist_ReturnsDefault()
        {
            var loaded = SaveManager.Load<TestSaveData>(TestKey);

            Assert.AreEqual(0, loaded.SaveVersion);
            Assert.AreEqual(0, loaded.Score);
            Assert.IsNull(loaded.PlayerName);
        }

        [Test]
        public void Load_CorruptedFile_ReturnsDefault()
        {
            WriteRawJson(TestKey, "{ invalid json");

            LogAssert.Expect(LogType.Warning, new Regex("存档反序列化失败|存档反序列化结果为空"));
            var loaded = SaveManager.Load<TestSaveData>(TestKey);

            Assert.AreEqual(0, loaded.SaveVersion);
            Assert.AreEqual(0, loaded.Score);
            Assert.IsNull(loaded.PlayerName);
        }

        #endregion

        #region SaveRaw / LoadRaw

        [Test]
        public void SaveRaw_And_LoadRaw_RoundTrip()
        {
            var data = new TestRawData
            {
                FloorIndex = 3,
                HP = 85
            };

            SaveManager.SaveRaw(TestRawKey, data);
            var loaded = SaveManager.LoadRaw<TestRawData>(TestRawKey);

            Assert.AreEqual(3, loaded.FloorIndex);
            Assert.AreEqual(85, loaded.HP);
        }

        [Test]
        public void LoadRaw_FileNotExist_ReturnsDefault()
        {
            var loaded = SaveManager.LoadRaw<TestRawData>(TestRawKey);

            Assert.AreEqual(0, loaded.FloorIndex);
            Assert.AreEqual(0, loaded.HP);
        }

        [Test]
        public void LoadRaw_CorruptedFile_ReturnsDefault()
        {
            WriteRawJson(TestRawKey, "{ invalid json");

            LogAssert.Expect(LogType.Warning, new Regex("存档反序列化失败|存档反序列化结果为空"));
            var loaded = SaveManager.LoadRaw<TestRawData>(TestRawKey);

            Assert.AreEqual(0, loaded.FloorIndex);
            Assert.AreEqual(0, loaded.HP);
        }

        #endregion

        #region HasSave

        [Test]
        public void HasSave_NoFile_ReturnsFalse()
        {
            Assert.IsFalse(SaveManager.HasSave(TestKey));
        }

        [Test]
        public void HasSave_AfterSave_ReturnsTrue()
        {
            SaveManager.Save(TestKey, new TestSaveData { SaveVersion = 1 });

            Assert.IsTrue(SaveManager.HasSave(TestKey));
        }

        #endregion

        #region DeleteSave

        [Test]
        public void DeleteSave_RemovesFile()
        {
            SaveManager.Save(TestKey, new TestSaveData { SaveVersion = 1 });
            SaveManager.DeleteSave(TestKey);

            Assert.IsFalse(SaveManager.HasSave(TestKey));
        }

        [Test]
        public void DeleteSave_FileNotExist_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SaveManager.DeleteSave(TestKey));
        }

        #endregion

        #region SerializableKeyValue 序列化

        [Test]
        public void Save_SerializableKeyValue_PreservesData()
        {
            var data = new TestSaveDataWithKV
            {
                Items = new List<SerializableKeyValue<string, int>>
                {
                    new SerializableKeyValue<string, int> { Key = "sword", Value = 3 },
                    new SerializableKeyValue<string, int> { Key = "potion", Value = 10 }
                }
            };

            SaveManager.Save(TestKey, data);
            var loaded = SaveManager.Load<TestSaveDataWithKV>(TestKey);

            Assert.AreEqual(2, loaded.Items.Count);
            Assert.AreEqual("sword", loaded.Items[0].Key);
            Assert.AreEqual(3, loaded.Items[0].Value);
            Assert.AreEqual("potion", loaded.Items[1].Key);
            Assert.AreEqual(10, loaded.Items[1].Value);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 清理测试存档文件
        /// </summary>
        private static void Cleanup(string key)
        {
            string path = Path.Combine(Application.persistentDataPath, key + ".json");
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// 直接写入原始 JSON 文本，用于构造损坏存档。
        /// </summary>
        private static void WriteRawJson(string key, string rawJson)
        {
            string path = Path.Combine(Application.persistentDataPath, key + ".json");
            File.WriteAllText(path, rawJson);
        }

        #endregion

        #region 测试用数据结构

        [System.Serializable]
        private class TestSaveData : ISaveData
        {
            [field: SerializeField]
            public int SaveVersion { get; set; }
            public int Score;
            public string PlayerName;
        }

        [System.Serializable]
        private class TestRawData
        {
            public int FloorIndex;
            public int HP;
        }

        [System.Serializable]
        private class TestSaveDataWithKV : ISaveData
        {
            [field: SerializeField]
            public int SaveVersion { get; set; }
            public List<SerializableKeyValue<string, int>> Items;
        }

        #endregion
    }
}
