using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Data.Save;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// RunManager 生命周期行为的 EditMode 单元测试。
    /// </summary>
    public class RunManagerTests
    {
        private const string RunSaveKey = "run_checkpoint";

        private GameObject _runManagerGo;
        private RunManager _runManager;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            CleanupCheckpoint();

            _runManagerGo = new GameObject("TestRunManager");

            _runManager = _runManagerGo.AddComponent<RunManager>();
        }

        [TearDown]
        public void TearDown()
        {
            EventCenter.Clear();
            CleanupCheckpoint();

            if (_runManagerGo != null)
            {
                Object.DestroyImmediate(_runManagerGo);
            }
        }

        [Test]
        public void ChangeState_ToRunEnd_ClearsCurrentRunAndCheckpoint()
        {
            SaveManager.SaveRaw(RunSaveKey, new RunState
            {
                RunId = "test_run",
                FloorIndex = 1,
                RoomIndex = 2
            });

            InvokeGameStateChanged(new GameStateChangedEvent
            {
                FromState = RogueDungeon.Core.GameState.Hub,
                ToState = RogueDungeon.Core.GameState.RunInit,
                RunId = string.Empty
            });
            Assert.IsNotNull(_runManager.CurrentRun);

            InvokeGameStateChanged(new GameStateChangedEvent
            {
                FromState = RogueDungeon.Core.GameState.RoomPlaying,
                ToState = RogueDungeon.Core.GameState.RoomClear,
                RunId = _runManager.CurrentRun.RunId
            });
            Assert.IsTrue(SaveManager.HasSave(RunSaveKey));

            InvokeGameStateChanged(new GameStateChangedEvent
            {
                FromState = RogueDungeon.Core.GameState.RoomPlaying,
                ToState = RogueDungeon.Core.GameState.RunEnd,
                RunId = _runManager.CurrentRun.RunId
            });

            Assert.IsNull(_runManager.CurrentRun);
            Assert.IsFalse(SaveManager.HasSave(RunSaveKey));
        }

        /// <summary>
        /// 清理 RunManager 使用的续关存档文件。
        /// </summary>
        private static void CleanupCheckpoint()
        {
            string path = Path.Combine(Application.persistentDataPath, RunSaveKey + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 通过反射触发 RunManager 的状态变化处理逻辑，隔离事件总线订阅时序影响。
        /// </summary>
        private void InvokeGameStateChanged(GameStateChangedEvent evt)
        {
            MethodInfo method = typeof(RunManager).GetMethod("OnGameStateChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);
            method.Invoke(_runManager, new object[] { evt });
        }

    }
}
