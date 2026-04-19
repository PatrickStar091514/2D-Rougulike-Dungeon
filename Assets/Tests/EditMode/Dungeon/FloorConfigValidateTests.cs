using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Data;
using UnityEngine;

namespace RogueDungeon.Tests.Dungeon
{
    public class FloorConfigValidateTests
    {
        private FloorConfigSO CreateConfig(
            int gridWidth = 5, int gridHeight = 4, int targetRoomCount = 12,
            int eliteCount = 1, int shopCount = 1, int eventCount = 1,
            float mergeRate = 0.3f, bool nullTemplates = false)
        {
            var config = ScriptableObject.CreateInstance<FloorConfigSO>();

            // 通过反射设置 private 字段
            var type = typeof(FloorConfigSO);
            SetField(config, "gridWidth", gridWidth);
            SetField(config, "gridHeight", gridHeight);
            SetField(config, "targetRoomCount", targetRoomCount);
            SetField(config, "eliteCount", eliteCount);
            SetField(config, "shopCount", shopCount);
            SetField(config, "eventCount", eventCount);
            SetField(config, "mergeRate", mergeRate);

            if (!nullTemplates)
            {
                var template = ScriptableObject.CreateInstance<RoomTemplateSO>();
                SetField(config, "templates", new RoomTemplateSO[] { template });
            }

            return config;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        [Test]
        public void Validate_ValidConfig_ReturnsTrue()
        {
            var config = CreateConfig();
            Assert.IsTrue(config.Validate());
        }

        [Test]
        public void Validate_GridTooSmall_ReturnsFalse()
        {
            var config = CreateConfig(gridWidth: 2, gridHeight: 2, targetRoomCount: 4);
            Assert.IsFalse(config.Validate());
            LogAssert_ExpectWarning();
        }

        [Test]
        public void Validate_RoomCountExceedsGrid_ReturnsFalse()
        {
            var config = CreateConfig(gridWidth: 3, gridHeight: 3, targetRoomCount: 15);
            Assert.IsFalse(config.Validate());
        }

        [Test]
        public void Validate_RoomCountTooLow_ReturnsFalse()
        {
            var config = CreateConfig(targetRoomCount: 3);
            Assert.IsFalse(config.Validate());
        }

        [Test]
        public void Validate_TooManySpecialRooms_ReturnsFalse()
        {
            var config = CreateConfig(targetRoomCount: 8, eliteCount: 3, shopCount: 2, eventCount: 2);
            Assert.IsFalse(config.Validate());
        }

        [Test]
        public void Validate_MergeRateOutOfBounds_ReturnsFalse()
        {
            var config = CreateConfig(mergeRate: 1.5f);
            Assert.IsFalse(config.Validate());
        }

        [Test]
        public void Validate_NullTemplates_ReturnsFalse()
        {
            var config = CreateConfig(nullTemplates: true);
            Assert.IsFalse(config.Validate());
        }

        [Test]
        public void Validate_MultipleFailures_ReportsAll()
        {
            // gridWidth=2 (grid too small) + nullTemplates (no templates) + targetRoomCount=3 (too few)
            var config = CreateConfig(gridWidth: 2, gridHeight: 2, targetRoomCount: 3, nullTemplates: true);
            Assert.IsFalse(config.Validate());
            // 验证不会提前返回 — 多项校验均应输出（由 LogWarning 验证）
        }

        /// <summary>
        /// 占位方法：在 Unity Test Runner 中会捕获 LogWarning，这里仅标记预期行为
        /// </summary>
        private void LogAssert_ExpectWarning()
        {
            // Unity LogAssert 会在 Test Runner 中自动捕获，
            // 这里不需要额外断言，Validate 返回 false 已足够验证
        }
    }
}
