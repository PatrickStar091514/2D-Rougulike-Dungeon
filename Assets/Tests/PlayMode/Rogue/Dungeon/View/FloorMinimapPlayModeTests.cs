using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;
using RogueDungeon.Dungeon.Map;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.View;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace RogueDungeon.Tests.Dungeon.View
{
    /// <summary>
    /// FloorMinimapController 的 PlayMode 场景联动测试。
    /// </summary>
    public class FloorMinimapPlayModeTests
    {
        private const string TestSceneName = "PatrickStarTest";
        private const string TestScenePath = "Assets/Scenes/PatrickStarTest.unity";

        [UnityTest]
        public IEnumerator PatrickStarTest_ContainsFloorMinimapController()
        {
            yield return LoadTestScene();

            var controller = Object.FindFirstObjectByType<FloorMinimapController>(FindObjectsInactive.Include);
            Assert.NotNull(controller);
            Assert.IsNull(controller.GetComponent<DungeonViewManager>());
            Assert.NotNull(controller.GetComponentInParent<Canvas>());
        }

        [UnityTest]
        public IEnumerator FloorMinimapPanel_IsMaskedAndLineUpdatesAfterRoomCleared()
        {
            var canvasGo = new GameObject("PlayMode_MinimapCanvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var controllerGo = new GameObject("PlayMode_FloorMinimapController");
            var controller = controllerGo.AddComponent<FloorMinimapController>();
            Assert.NotNull(controller);
            SetPrivateField(controller, "_viewManager", null);
            SetPrivateField(controller, "_targetCanvas", canvas);
            SetPrivateField(controller, "_createCanvasWhenMissing", false);

            var startDoor = new DoorSlot(Vector2Int.zero, Direction.East);
            var bossDoor = new DoorSlot(Vector2Int.zero, Direction.West);
            var start = new RoomInstance(
                "start",
                RoomType.Start,
                RoomShape.Single,
                Vector2Int.zero,
                new List<Vector2Int> { new(0, 0) },
                null,
                true,
                new List<DoorConnection> { new(startDoor, "boss", bossDoor) });
            var boss = new RoomInstance(
                "boss",
                RoomType.Boss,
                RoomShape.Single,
                new Vector2Int(1, 0),
                new List<Vector2Int> { new(1, 0) },
                null,
                true,
                new List<DoorConnection> { new(bossDoor, "start", startDoor) });
            var map = CreateMap(new List<RoomInstance> { start, boss }, "start", "boss");
            start.Visited = true;
            boss.Visited = true;

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });
            yield return null;

            var panelRect = GetPrivateField<RectTransform>(controller, "_panelRect");
            Assert.NotNull(panelRect);
            Assert.NotNull(panelRect.GetComponent<Mask>());
            Assert.AreEqual(new Vector2(1f, 1f), panelRect.anchorMin);
            Assert.AreEqual(new Vector2(1f, 1f), panelRect.anchorMax);

            Assert.Greater(controller.LineVisualCount, 0);
            Assert.AreEqual(0, controller.ActiveLineVisualCount);
            boss.Cleared = true;
            InvokePrivate(controller, "OnRoomCleared", new RoomClearedEvent { RoomId = "boss" });
            Assert.AreEqual(1, controller.ActiveLineVisualCount);
        }

        private static IEnumerator LoadTestScene()
        {
#if UNITY_EDITOR
            var loadOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                TestScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            if (loadOperation == null)
            {
                Assert.Fail($"Failed to load scene at path: {TestScenePath}");
            }
            while (!loadOperation.isDone)
            {
                yield return null;
            }
#else
            SceneManager.LoadScene(TestSceneName, LoadSceneMode.Single);
            yield return null;
#endif
        }

        private static DungeonMap CreateMap(List<RoomInstance> rooms, string startId, string bossId)
        {
            var ctor = typeof(DungeonMap).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(List<RoomInstance>) },
                null);
            Assert.NotNull(ctor);
            return (DungeonMap)ctor.Invoke(new object[] { startId, bossId, rooms });
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return field.GetValue(instance) as T;
        }

        private static void InvokePrivate(object instance, string methodName, object arg)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(instance, new[] { arg });
        }
    }
}
