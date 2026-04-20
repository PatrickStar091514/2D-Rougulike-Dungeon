using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class DungeonViewManagerTests
    {
        private GameObject _managerGo;
        private DungeonViewManager _manager;
        private readonly List<Object> _createdAssets = new();
        private MethodInfo _onEnableMethod;
        private MethodInfo _onDisableMethod;
        private MethodInfo _applyCellWorldSizeMethod;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            _managerGo = new GameObject("DungeonViewManager");
            _manager = _managerGo.AddComponent<DungeonViewManager>();
            _onEnableMethod = typeof(DungeonViewManager).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            _onDisableMethod = typeof(DungeonViewManager).GetMethod(
                "OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            _applyCellWorldSizeMethod = typeof(DungeonViewManager).GetMethod(
                "ApplyCellWorldSize", BindingFlags.NonPublic | BindingFlags.Instance);
            _onEnableMethod?.Invoke(_manager, null);
        }

        [TearDown]
        public void TearDown()
        {
            typeof(DungeonViewManager).GetField("_cellWorldSize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_manager, 10);
            _applyCellWorldSizeMethod?.Invoke(_manager, null);
            _onDisableMethod?.Invoke(_manager, null);
            Object.DestroyImmediate(_managerGo);
            foreach (var asset in _createdAssets)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
            EventCenter.Clear();
        }

        [Test]
        public void OnDungeonGenerated_EmptyMap_DoesNotPublishDungeonReady()
        {
            var readyCount = 0;
            EventCenter.AddListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, _ => readyCount++);

            EventCenter.Broadcast(GameEventType.DungeonGenerated,
                new DungeonGeneratedEvent { Map = null });

            Assert.AreEqual(0, readyCount);
        }

        [Test]
        public void OnDungeonGenerated_NonEmptyMap_InstantiatesRoom_AndPublishesDungeonReady()
        {
            var readyCount = 0;
            EventCenter.AddListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, _ => readyCount++);

            var map = CreateMap(new List<RoomInstance>
            {
                CreateRoom("room_a", RoomType.Start, new Vector2Int(2, 3), new List<DoorConnection>())
            }, "room_a", "room_a");

            EventCenter.Broadcast(GameEventType.DungeonGenerated,
                new DungeonGeneratedEvent { Map = map });

            Assert.AreEqual(1, readyCount);
            Assert.IsTrue(_manager.TryGetRoomView("room_a", out var roomView));
            Assert.NotNull(roomView);
            Assert.AreEqual(new Vector3(25f, 35f, 0f), roomView.transform.position);
        }

        [Test]
        public void OnDungeonGenerated_StartRoomRevealed_OthersHidden()
        {
            var map = CreateMap(new List<RoomInstance>
            {
                CreateRoom("start", RoomType.Start, Vector2Int.zero, new List<DoorConnection>()),
                CreateRoom("normal", RoomType.Normal, new Vector2Int(1, 0), new List<DoorConnection>())
            }, "start", "normal");

            EventCenter.Broadcast(GameEventType.DungeonGenerated,
                new DungeonGeneratedEvent { Map = map });

            Assert.IsTrue(_manager.TryGetRoomView("start", out var startView));
            Assert.IsTrue(_manager.TryGetRoomView("normal", out var normalView));
            Assert.AreEqual(RoomVisibility.Revealed, startView.Visibility);
            Assert.AreEqual(RoomVisibility.Hidden, normalView.Visibility);
        }

        [Test]
        public void OnRoomEntered_RevealsTarget_AndPromotesHiddenNeighborToSilhouette()
        {
            var doorE = new DoorSlot(Vector2Int.zero, Direction.East);
            var doorW = new DoorSlot(Vector2Int.zero, Direction.West);
            var doorsStart = new List<DoorConnection>
            {
                new(doorE, "middle", doorW)
            };
            var doorsMiddle = new List<DoorConnection>
            {
                new(doorW, "start", doorE),
                new(doorE, "tail", doorW)
            };
            var doorsTail = new List<DoorConnection>
            {
                new(doorW, "middle", doorE)
            };

            var map = CreateMap(new List<RoomInstance>
            {
                CreateRoom("start", RoomType.Start, Vector2Int.zero, doorsStart),
                CreateRoom("middle", RoomType.Normal, new Vector2Int(1, 0), doorsMiddle),
                CreateRoom("tail", RoomType.Normal, new Vector2Int(2, 0), doorsTail)
            }, "start", "tail");

            EventCenter.Broadcast(GameEventType.DungeonGenerated,
                new DungeonGeneratedEvent { Map = map });

            Assert.IsTrue(_manager.TryGetRoomView("middle", out var middleBefore));
            Assert.IsTrue(_manager.TryGetRoomView("tail", out var tailBefore));
            Assert.AreEqual(RoomVisibility.Silhouette, middleBefore.Visibility);
            Assert.AreEqual(RoomVisibility.Hidden, tailBefore.Visibility);

            var middleRoom = map.GetRoom("middle");
            EventCenter.Broadcast(GameEventType.RoomEntered, new RoomEnteredEvent { Room = middleRoom });

            Assert.IsTrue(_manager.TryGetRoomView("middle", out var middleAfter));
            Assert.IsTrue(_manager.TryGetRoomView("tail", out var tailAfter));
            Assert.AreEqual(RoomVisibility.Revealed, middleAfter.Visibility);
            Assert.AreEqual(RoomVisibility.Silhouette, tailAfter.Visibility);
        }

        [Test]
        public void CellWorldSize_Is10()
        {
            Assert.AreEqual(10, DungeonViewManager.CellWorldSize);
        }

        [Test]
        public void OnDungeonGenerated_UsesInspectorCellWorldSizeForPlacement()
        {
            typeof(DungeonViewManager).GetField("_cellWorldSize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_manager, 12);
            _applyCellWorldSizeMethod?.Invoke(_manager, null);

            var map = CreateMap(new List<RoomInstance>
            {
                CreateRoom("room_scaled", RoomType.Start, new Vector2Int(2, 3), new List<DoorConnection>())
            }, "room_scaled", "room_scaled");

            EventCenter.Broadcast(GameEventType.DungeonGenerated,
                new DungeonGeneratedEvent { Map = map });

            Assert.IsTrue(_manager.TryGetRoomView("room_scaled", out var roomView));
            Assert.AreEqual(new Vector3(30f, 42f, 0f), roomView.transform.position);
        }

        private RoomInstance CreateRoom(string id, RoomType type, Vector2Int gridPos, IReadOnlyList<DoorConnection> doors)
        {
            var prefab = new GameObject($"Prefab_{id}");
            prefab.AddComponent<RoomView>();
            _createdAssets.Add(prefab);

            var template = ScriptableObject.CreateInstance<RoomTemplateSO>();
            _createdAssets.Add(template);
            SetTemplateFields(template, $"template_{id}", RoomShape.Single, new[] { type }, new DoorSlot[0], prefab, 1);

            return new RoomInstance(
                id,
                type,
                RoomShape.Single,
                gridPos,
                new List<Vector2Int> { Vector2Int.zero },
                template,
                true,
                doors
            );
        }

        private static DungeonMap CreateMap(List<RoomInstance> rooms, string startId, string bossId)
        {
            var ctor = typeof(DungeonMap).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(List<RoomInstance>) },
                null);
            Assert.NotNull(ctor, "DungeonMap private ctor not found");
            return (DungeonMap)ctor.Invoke(new object[] { startId, bossId, rooms });
        }

        private static void SetTemplateFields(
            RoomTemplateSO template,
            string id,
            RoomShape shape,
            RoomType[] allowedTypes,
            DoorSlot[] doorSlots,
            GameObject prefab,
            int weight)
        {
            var type = typeof(RoomTemplateSO);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            type.GetField("templateId", flags)?.SetValue(template, id);
            type.GetField("shape", flags)?.SetValue(template, shape);
            type.GetField("allowedTypes", flags)?.SetValue(template, allowedTypes);
            type.GetField("doorSlots", flags)?.SetValue(template, doorSlots);
            type.GetField("prefab", flags)?.SetValue(template, prefab);
            type.GetField("weight", flags)?.SetValue(template, weight);
        }
    }
}
