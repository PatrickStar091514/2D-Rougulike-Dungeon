using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    /// <summary>
    /// FloorMinimapController 的 EditMode 规则测试。
    /// </summary>
    public class FloorMinimapControllerTests
    {
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            SetStaticDungeonManagerInstance(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _created.Clear();
            var generatedCanvas = GameObject.Find("MinimapCanvas");
            if (generatedCanvas != null)
                Object.DestroyImmediate(generatedCanvas);
            EventCenter.Clear();
            SetStaticDungeonManagerInstance(null);
        }

        [Test]
        public void OnDungeonGenerated_BuildsCellsFromRoomInstanceCells()
        {
            var start = CreateRoom("start", RoomType.Start, RoomShape.Single,
                new List<Vector2Int> { new(0, 0) }, new List<DoorConnection>());
            var boss = CreateRoom("boss", RoomType.Boss, RoomShape.BigSquare,
                new List<Vector2Int> { new(2, 0), new(3, 0), new(2, 1), new(3, 1) },
                new List<DoorConnection>());
            var map = CreateMap(new List<RoomInstance> { start, boss }, "start", "boss");

            var viewManager = CreateViewManagerWithRooms(
                ("start", start, RoomVisibility.Revealed),
                ("boss", boss, RoomVisibility.Silhouette));
            var controller = CreateController(viewManager);
            CreateDungeonManager(map, start);

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });

            Assert.AreEqual(5, controller.CellVisualCount);
        }

        [Test]
        public void RefreshVisuals_CurrentRoomIsWhite_OtherVisibleRoomsAreGray()
        {
            var start = CreateRoom("start", RoomType.Start, RoomShape.Single,
                new List<Vector2Int> { new(0, 0) }, new List<DoorConnection>());
            var normal = CreateRoom("normal", RoomType.Normal, RoomShape.Single,
                new List<Vector2Int> { new(1, 0) }, new List<DoorConnection>());
            var map = CreateMap(new List<RoomInstance> { start, normal }, "start", "normal");

            var viewManager = CreateViewManagerWithRooms(
                ("start", start, RoomVisibility.Revealed),
                ("normal", normal, RoomVisibility.Silhouette));
            var controller = CreateController(viewManager);
            CreateDungeonManager(map, start);

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });

            Assert.IsTrue(controller.TryGetRoomColors("start", out var startColors));
            Assert.IsTrue(controller.TryGetRoomColors("normal", out var normalColors));
            Assert.IsTrue(startColors.Count > 0);
            Assert.IsTrue(normalColors.Count > 0);

            foreach (var color in startColors)
                Assert.AreEqual(Color.white, color);
            foreach (var color in normalColors)
                Assert.AreEqual(new Color(0.6f, 0.6f, 0.6f, 1f), color);
        }

        [Test]
        public void BossIcon_VisibleOnlyWhenBossRoomVisible()
        {
            var startDoor = new DoorSlot(Vector2Int.zero, Direction.East);
            var bossDoor = new DoorSlot(Vector2Int.zero, Direction.West);
            var start = CreateRoom("start", RoomType.Start, RoomShape.Single,
                new List<Vector2Int> { new(0, 0) },
                new List<DoorConnection> { new(startDoor, "boss", bossDoor) });
            var boss = CreateRoom("boss", RoomType.Boss, RoomShape.Single,
                new List<Vector2Int> { new(1, 0) },
                new List<DoorConnection> { new(bossDoor, "start", startDoor) });
            var map = CreateMap(new List<RoomInstance> { start, boss }, "start", "boss");

            var viewManager = CreateViewManagerWithRooms(
                ("start", start, RoomVisibility.Revealed),
                ("boss", boss, RoomVisibility.Silhouette));
            var controller = CreateController(viewManager);
            var roomViews = GetRoomViews(viewManager);
            CreateDungeonManager(map, start);

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });
            Assert.IsTrue(controller.IsBossIconVisible);

            roomViews["boss"].SetVisibility(RoomVisibility.Hidden);
            InvokePrivate(controller, "OnRoomEntered", new RoomEnteredEvent { Room = start });
            Assert.IsFalse(controller.IsBossIconVisible);
        }

        [Test]
        public void Lines_ShownOnlyWhenBothEndsCleared_AndStartTreatedCleared()
        {
            var startDoor = new DoorSlot(Vector2Int.zero, Direction.East);
            var nextDoor = new DoorSlot(Vector2Int.zero, Direction.West);
            var start = CreateRoom("start", RoomType.Start, RoomShape.Single,
                new List<Vector2Int> { new(0, 0) },
                new List<DoorConnection> { new(startDoor, "next", nextDoor) });
            var next = CreateRoom("next", RoomType.Normal, RoomShape.Single,
                new List<Vector2Int> { new(1, 0) },
                new List<DoorConnection> { new(nextDoor, "start", startDoor) });
            var map = CreateMap(new List<RoomInstance> { start, next }, "start", "next");

            var viewManager = CreateViewManagerWithRooms(
                ("start", start, RoomVisibility.Revealed),
                ("next", next, RoomVisibility.Silhouette));
            var controller = CreateController(viewManager);
            CreateDungeonManager(map, start);

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });
            Assert.AreEqual(1, controller.LineVisualCount);
            Assert.AreEqual(0, controller.ActiveLineVisualCount);

            next.Cleared = true;
            InvokePrivate(controller, "OnRoomCleared", new RoomClearedEvent { RoomId = "next" });
            Assert.AreEqual(1, controller.ActiveLineVisualCount);
        }

        [Test]
        public void OnDungeonGenerated_AppliesScaleFrame_AndRendersContiguousRoomShape()
        {
            var start = CreateRoom("start", RoomType.Start, RoomShape.DoubleH,
                new List<Vector2Int> { new(0, 0), new(1, 0) }, new List<DoorConnection>());
            var map = CreateMap(new List<RoomInstance> { start }, "start", "start");

            var viewManager = CreateViewManagerWithRooms(
                ("start", start, RoomVisibility.Revealed));
            var controller = CreateController(viewManager);
            CreateDungeonManager(map, start);

            SetPrivateField(controller, "_cellSize", 10f);
            SetPrivateField(controller, "_mapScale", 1.5f);
            SetPrivateField(controller, "_cellPadding", 0f);
            SetPrivateField(controller, "_frameThickness", 3f);
            SetPrivateField(controller, "_frameColor", new Color(0.2f, 0.9f, 0.2f, 1f));

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });

            var contentRect = GetPrivateField<RectTransform>(controller, "_contentRect");
            var panelRect = GetPrivateField<RectTransform>(controller, "_panelRect");
            var panelOutline = panelRect.GetComponent<Outline>();
            Assert.NotNull(contentRect);
            Assert.NotNull(panelRect);
            Assert.NotNull(panelOutline);
            Assert.AreEqual(new Vector2(30f, 15f), contentRect.sizeDelta);
            Assert.AreEqual(new Vector2(3f, 3f), panelOutline.effectDistance);
            Assert.AreEqual(new Color(0.2f, 0.9f, 0.2f, 1f), panelOutline.effectColor);

            var roomCellViews = GetPrivateField<Dictionary<string, List<Image>>>(controller, "_roomCellViews");
            Assert.IsTrue(roomCellViews.TryGetValue("start", out var cells));
            Assert.AreEqual(2, cells.Count);
            var firstRect = cells[0].rectTransform;
            var secondRect = cells[1].rectTransform;
            Assert.AreEqual(15f, firstRect.sizeDelta.x);
            Assert.AreEqual(15f, secondRect.sizeDelta.x);
            Assert.AreEqual(15f, Mathf.Abs(secondRect.anchoredPosition.x - firstRect.anchoredPosition.x));
        }

        [Test]
        public void OnRoomEntered_CentersMinimapOnCurrentRoom()
        {
            var leftDoor = new DoorSlot(Vector2Int.zero, Direction.East);
            var rightDoor = new DoorSlot(Vector2Int.zero, Direction.West);
            var left = CreateRoom("left", RoomType.Start, RoomShape.DoubleH,
                new List<Vector2Int> { new(0, 0), new(1, 0) },
                new List<DoorConnection> { new(leftDoor, "right", rightDoor) });
            var right = CreateRoom("right", RoomType.Normal, RoomShape.DoubleH,
                new List<Vector2Int> { new(8, 0), new(9, 0) },
                new List<DoorConnection> { new(rightDoor, "left", leftDoor) });
            var map = CreateMap(new List<RoomInstance> { left, right }, "left", "right");

            var viewManager = CreateViewManagerWithRooms(
                ("left", left, RoomVisibility.Revealed),
                ("right", right, RoomVisibility.Silhouette));
            var controller = CreateController(viewManager);
            var manager = CreateDungeonManager(map, left);

            SetPrivateField(controller, "_panelSize", new Vector2(40f, 40f));
            SetPrivateField(controller, "_cellSize", 10f);
            SetPrivateField(controller, "_mapScale", 1f);
            SetPrivateField(controller, "_cellPadding", 0f);

            InvokePrivate(controller, "OnDungeonGenerated", new DungeonGeneratedEvent { Map = map });
            var contentRect = GetPrivateField<RectTransform>(controller, "_contentRect");
            Assert.NotNull(contentRect);
            Assert.AreEqual(0f, contentRect.anchoredPosition.x);

            SetAutoProperty(manager, "CurrentRoom", right);
            InvokePrivate(controller, "OnRoomEntered", new RoomEnteredEvent { Room = right });
            Assert.AreEqual(-60f, contentRect.anchoredPosition.x);
        }

        private FloorMinimapController CreateController(DungeonViewManager viewManager)
        {
            var host = new GameObject("MinimapHost");
            _created.Add(host);
            var controller = host.AddComponent<FloorMinimapController>();
            SetPrivateField(controller, "_viewManager", viewManager);
            return controller;
        }

        private DungeonViewManager CreateViewManagerWithRooms(params (string id, RoomInstance room, RoomVisibility visibility)[] rooms)
        {
            var go = new GameObject("DungeonViewManager_Test");
            _created.Add(go);
            var manager = go.AddComponent<DungeonViewManager>();

            var dict = GetRoomViews(manager);
            dict.Clear();
            foreach (var entry in rooms)
            {
                var roomGo = new GameObject($"RoomView_{entry.id}");
                _created.Add(roomGo);
                var roomView = roomGo.AddComponent<RoomView>();
                roomView.Initialize(entry.room);
                roomView.SetVisibility(entry.visibility);
                dict[entry.id] = roomView;
            }

            return manager;
        }

        private static Dictionary<string, RoomView> GetRoomViews(DungeonViewManager manager)
        {
            var field = typeof(DungeonViewManager).GetField("_roomViews", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var dict = field.GetValue(manager) as Dictionary<string, RoomView>;
            Assert.NotNull(dict);
            return dict;
        }

        private DungeonManager CreateDungeonManager(DungeonMap map, RoomInstance currentRoom)
        {
            var go = new GameObject("DungeonManager_Test");
            _created.Add(go);
            var manager = go.AddComponent<DungeonManager>();
            SetStaticDungeonManagerInstance(manager);
            SetAutoProperty(manager, "CurrentMap", map);
            SetAutoProperty(manager, "CurrentRoom", currentRoom);
            return manager;
        }

        private static RoomInstance CreateRoom(
            string id,
            RoomType type,
            RoomShape shape,
            IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<DoorConnection> doors)
        {
            return new RoomInstance(
                id,
                type,
                shape,
                cells[0],
                cells,
                null,
                true,
                doors);
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

        private static void InvokePrivate(object instance, string methodName, object arg)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(instance, new[] { arg });
        }

        private static void SetAutoProperty(object instance, string propertyName, object value)
        {
            var field = instance.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(instance, value);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field.GetValue(instance) as T;
        }

        private static void SetStaticDungeonManagerInstance(object value)
        {
            var field = typeof(DungeonManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }
    }
}
