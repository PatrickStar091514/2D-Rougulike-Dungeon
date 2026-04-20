using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    /// <summary>
    /// DungeonCamera 的边界计算和钳制逻辑 EditMode 测试
    /// </summary>
    public class DungeonCameraTests
    {
        private readonly List<Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            SetStaticDungeonManagerInstance(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();

            SetStaticDungeonManagerInstance(null);
            EventCenter.Clear();
        }

        #region Helper

        /// <summary>
        /// 创建最小化 RoomInstance 用于测试
        /// </summary>
        private static RoomInstance MakeRoom(
            RoomShape shape,
            Vector2Int gridPos,
            string id = "test")
        {
            var cells = RoomShapeUtil.GetCells(shape);
            var cellList = new List<Vector2Int>(cells);
            return new RoomInstance(
                id, RoomType.Normal, shape, gridPos,
                cellList, null, false, new List<DoorConnection>());
        }

        #endregion

        #region CalculateRoomBounds

        [Test]
        public void CalculateRoomBounds_Single_AtOrigin_Returns10x10()
        {
            var room = MakeRoom(RoomShape.Single, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(0f, bounds.x, 0.01f);
            Assert.AreEqual(0f, bounds.y, 0.01f);
            Assert.AreEqual(10f, bounds.width, 0.01f);
            Assert.AreEqual(10f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_Single_AtOffset_ShiftsCorrectly()
        {
            var room = MakeRoom(RoomShape.Single, new Vector2Int(3, 2));
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            // gridPos(3,2): worldMin = (0*10 + 3*10, 0*10 + 2*10) = (30, 20)
            Assert.AreEqual(30f, bounds.x, 0.01f);
            Assert.AreEqual(20f, bounds.y, 0.01f);
            Assert.AreEqual(10f, bounds.width, 0.01f);
            Assert.AreEqual(10f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_BigSquare_Returns20x20()
        {
            var room = MakeRoom(RoomShape.BigSquare, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(0f, bounds.x, 0.01f);
            Assert.AreEqual(0f, bounds.y, 0.01f);
            Assert.AreEqual(20f, bounds.width, 0.01f);
            Assert.AreEqual(20f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_DoubleH_Returns20x10()
        {
            var room = MakeRoom(RoomShape.DoubleH, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(20f, bounds.width, 0.01f);
            Assert.AreEqual(10f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_DoubleV_Returns10x20()
        {
            var room = MakeRoom(RoomShape.DoubleV, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(10f, bounds.width, 0.01f);
            Assert.AreEqual(20f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_RU_Returns20x20AABB()
        {
            // RU cells: (0,0), (1,0), (1,1) → AABB: 0..2 x 0..2 → 20x20
            var room = MakeRoom(RoomShape.RU, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(0f, bounds.x, 0.01f);
            Assert.AreEqual(0f, bounds.y, 0.01f);
            Assert.AreEqual(20f, bounds.width, 0.01f);
            Assert.AreEqual(20f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_LU_Returns20x20AABB()
        {
            // LU cells: (0,0), (1,0), (0,1) → AABB: 0..2 x 0..2 → 20x20
            var room = MakeRoom(RoomShape.LU, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(20f, bounds.width, 0.01f);
            Assert.AreEqual(20f, bounds.height, 0.01f);
        }

        [Test]
        public void CalculateRoomBounds_LD_IncludesNegativeY()
        {
            // LD cells: (0,0), (1,0), (0,-1) → minY=-1, maxY=0 → worldY: -10..10 → height=20
            var room = MakeRoom(RoomShape.LD, Vector2Int.zero);
            var bounds = DungeonCamera.CalculateRoomBounds(room);

            Assert.AreEqual(-10f, bounds.y, 0.01f);
            Assert.AreEqual(20f, bounds.width, 0.01f);
            Assert.AreEqual(20f, bounds.height, 0.01f);
        }

        #endregion

        #region ClampPosition

        [Test]
        public void ClampPosition_LargeRoom_ClampsToEdge()
        {
            // 房间 40x40（4 cells），orthoSize=5（视口高10, 宽 10*1.78≈17.8）
            var bounds = new Rect(0, 0, 40, 40);
            float orthoSize = 5f;
            float aspect = 16f / 9f;

            // 玩家在右上角
            var result = DungeonCamera.ClampPosition(new Vector2(38, 38), bounds, orthoSize, aspect);

            float halfW = orthoSize * aspect; // ~8.89
            float halfH = orthoSize;          // 5
            Assert.AreEqual(40f - halfW, result.x, 0.01f);
            Assert.AreEqual(35f, result.y, 0.01f); // 40 - 5
        }

        [Test]
        public void ClampPosition_SmallRoom_CentersOnBothAxes()
        {
            // 房间 10x10，orthoSize=10（视口高20, 宽≈35.6）→ 两轴都应居中
            var bounds = new Rect(0, 0, 10, 10);
            float orthoSize = 10f;
            float aspect = 16f / 9f;

            var result = DungeonCamera.ClampPosition(new Vector2(2, 3), bounds, orthoSize, aspect);

            Assert.AreEqual(5f, result.x, 0.01f); // 居中
            Assert.AreEqual(5f, result.y, 0.01f); // 居中
        }

        [Test]
        public void ClampPosition_PerAxisIndependent_XCentered_YClamped()
        {
            // 房间 10x40，orthoSize=5, aspect=1.78
            // X: halfW=8.89 > halfRoom=5 → X 居中
            // Y: halfH=5 < halfRoom=20 → Y 钳制
            var bounds = new Rect(0, 0, 10, 40);
            float orthoSize = 5f;
            float aspect = 16f / 9f;

            var result = DungeonCamera.ClampPosition(new Vector2(2, 38), bounds, orthoSize, aspect);

            Assert.AreEqual(5f, result.x, 0.01f); // X 轴居中
            Assert.AreEqual(35f, result.y, 0.01f); // Y 轴钳制到 40-5=35
        }

        [Test]
        public void ClampPosition_TargetInsideBounds_NoChange()
        {
            var bounds = new Rect(0, 0, 40, 40);
            float orthoSize = 5f;
            float aspect = 1f;

            var target = new Vector2(20, 20);
            var result = DungeonCamera.ClampPosition(target, bounds, orthoSize, aspect);

            Assert.AreEqual(20f, result.x, 0.01f);
            Assert.AreEqual(20f, result.y, 0.01f);
        }

        [Test]
        public void ClampPosition_TargetAtMinEdge_ClampsCorrectly()
        {
            var bounds = new Rect(10, 10, 40, 40);
            float orthoSize = 5f;
            float aspect = 1f;

            // 玩家在左下角外侧
            var result = DungeonCamera.ClampPosition(new Vector2(10, 10), bounds, orthoSize, aspect);

            Assert.AreEqual(15f, result.x, 0.01f); // 10 + 5
            Assert.AreEqual(15f, result.y, 0.01f); // 10 + 5
        }

        #endregion

        #region Component behavior tests

        [Test]
        public void OnValidate_AppliesOrthographicSizeToCamera()
        {
            var (camera, controller) = CreateCameraController(7f);

            Assert.IsTrue(camera.orthographic);
            Assert.AreEqual(7f, camera.orthographicSize, 0.01f);
            Assert.NotNull(controller);
        }

        [Test]
        public void DungeonReady_WithFollowTarget_SnapsToClampedPosition()
        {
            var room = MakeRoom(RoomShape.BigSquare, Vector2Int.zero, "start");
            var map = CreateMap(new List<RoomInstance> { room }, "start", "start");
            CreateDungeonManagerWithMap(map, room);

            var (camera, controller) = CreateCameraController(5f);
            var playerGo = new GameObject("Player");
            _createdObjects.Add(playerGo);
            playerGo.transform.position = new Vector3(10f, 10f, 0f);
            controller.FollowTarget = playerGo.transform;

            EventCenter.Broadcast(GameEventType.DungeonReady, new DungeonReadyEvent());

            var expectedBounds = DungeonCamera.CalculateRoomBounds(room);
            var expected = DungeonCamera.ClampPosition(
                (Vector2)playerGo.transform.position,
                expectedBounds,
                camera.orthographicSize,
                camera.aspect);

            Assert.AreEqual(expectedBounds.x, controller.CurrentBounds.x, 0.01f);
            Assert.AreEqual(expectedBounds.y, controller.CurrentBounds.y, 0.01f);
            Assert.AreEqual(expectedBounds.width, controller.CurrentBounds.width, 0.01f);
            Assert.AreEqual(expectedBounds.height, controller.CurrentBounds.height, 0.01f);
            Assert.AreEqual(expected.x, camera.transform.position.x, 0.01f);
            Assert.AreEqual(expected.y, camera.transform.position.y, 0.01f);
        }

        [Test]
        public void DungeonReady_WithoutFollowTarget_UsesStartRoomCenterFallback()
        {
            var room = MakeRoom(RoomShape.BigSquare, Vector2Int.zero, "start");
            var map = CreateMap(new List<RoomInstance> { room }, "start", "start");
            CreateDungeonManagerWithMap(map, room);

            var (camera, _) = CreateCameraController(5f);
            EventCenter.Broadcast(GameEventType.DungeonReady, new DungeonReadyEvent());

            var bounds = DungeonCamera.CalculateRoomBounds(room);
            var expected = DungeonCamera.ClampPosition(bounds.center, bounds, camera.orthographicSize, camera.aspect);
            Assert.AreEqual(expected.x, camera.transform.position.x, 0.01f);
            Assert.AreEqual(expected.y, camera.transform.position.y, 0.01f);
        }

        [Test]
        public void RoomEntered_EventStartsPanCoroutine()
        {
            var room = MakeRoom(RoomShape.BigSquare, Vector2Int.zero, "r1");
            var (camera, controller) = CreateCameraController(5f);
            var playerGo = new GameObject("Player");
            _createdObjects.Add(playerGo);
            controller.FollowTarget = playerGo.transform;

            var initField = typeof(DungeonCamera).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance);
            initField?.SetValue(controller, true);

            EventCenter.Broadcast(GameEventType.RoomEntered, new RoomEnteredEvent { Room = room });

            var panField = typeof(DungeonCamera).GetField("_panCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            var panCoroutine = panField?.GetValue(controller);
            Assert.NotNull(panCoroutine);
            Assert.NotNull(camera);
        }

        private (Camera camera, DungeonCamera controller) CreateCameraController(float orthographicSize)
        {
            var cameraGo = new GameObject("MainCamera");
            _createdObjects.Add(cameraGo);
            var camera = cameraGo.AddComponent<Camera>();
            var controller = cameraGo.AddComponent<DungeonCamera>();

            typeof(DungeonCamera).GetField("_orthographicSize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(controller, orthographicSize);
            var onValidate = typeof(DungeonCamera).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
            onValidate?.Invoke(controller, null);

            return (camera, controller);
        }

        private void CreateDungeonManagerWithMap(DungeonMap map, RoomInstance currentRoom)
        {
            var dmGo = new GameObject("DungeonManager");
            _createdObjects.Add(dmGo);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("floorConfigs 未配置|floorConfigs 未配置或为空"));
            var manager = dmGo.AddComponent<DungeonManager>();

            SetAutoProperty(manager, "CurrentMap", map);
            SetAutoProperty(manager, "CurrentRoom", currentRoom);
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

        private static void SetAutoProperty(object instance, string propertyName, object value)
        {
            var field = instance.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{propertyName} backing field not found");
            field.SetValue(instance, value);
        }

        private static void SetStaticDungeonManagerInstance(object value)
        {
            var field = typeof(DungeonManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        #endregion
    }
}
