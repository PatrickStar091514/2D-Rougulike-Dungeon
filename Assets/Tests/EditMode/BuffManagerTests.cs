using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core.Buff;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Config;
using RogueDungeon.Data.Runtime;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// BuffManager 核心行为的 EditMode 单元测试。
    /// </summary>
    public class BuffManagerTests
    {
        private GameObject _buffManagerGo;
        private BuffManager _buffManager;
        private BuffConfigSO _permanentDecayConfig;
        private BuffConfigSO _permanentLinearConfig;
        private BuffConfigSO _timedConfig;
        private BuffConfigSO _stackConfig;
        private BuffConfigSO _instantConfig;
        private BuffConfigSO _attackFlatConfig;
        private BuffPoolSO _pool;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();

            if (BuffManager.Instance != null)
                Object.DestroyImmediate(BuffManager.Instance.gameObject);

            _buffManagerGo = new GameObject("TestBuffManager");
            _buffManager = _buffManagerGo.AddComponent<BuffManager>();

            _permanentDecayConfig = CreateBuffConfig("perm_decay", DurationType.Permanent, 0f, 0, 0.7f, CreateModifier(StatType.Attack, ModifyType.Flat, 10f));
            _permanentLinearConfig = CreateBuffConfig("perm_linear", DurationType.Permanent, 0f, 0, 1f, CreateModifier(StatType.Attack, ModifyType.Flat, 10f));
            _timedConfig = CreateBuffConfig("timed_buff", DurationType.Timed, 5f, 0);
            _stackConfig = CreateBuffConfig("stack_buff", DurationType.Stack, 0f, 2, 1f, CreateModifier(StatType.Attack, ModifyType.Flat, 5f));
            _instantConfig = CreateBuffConfig("instant_buff", DurationType.Instant, 0f, 0);
            _attackFlatConfig = CreateBuffConfig("attack_flat", DurationType.Permanent, 0f, 0, 1f, CreateModifier(StatType.Attack, ModifyType.Flat, 3f));

            _pool = ScriptableObject.CreateInstance<BuffPoolSO>();
            SetPoolEntries(_pool, new[]
            {
                _permanentDecayConfig, _permanentLinearConfig, _timedConfig, _stackConfig, _instantConfig, _attackFlatConfig
            });
            SetBuffPool(_buffManager, _pool);
            SetField(_buffManager, "_activeBuffs", new List<BuffInstance>());
        }

        [TearDown]
        public void TearDown()
        {
            EventCenter.Clear();

            if (_buffManagerGo != null) Object.DestroyImmediate(_buffManagerGo);

            Object.DestroyImmediate(_permanentDecayConfig);
            Object.DestroyImmediate(_permanentLinearConfig);
            Object.DestroyImmediate(_timedConfig);
            Object.DestroyImmediate(_stackConfig);
            Object.DestroyImmediate(_instantConfig);
            Object.DestroyImmediate(_attackFlatConfig);
            Object.DestroyImmediate(_pool);
        }

        [Test]
        public void ApplyBuff_Permanent_AddsToActiveBuffs()
        {
            bool result = _buffManager.ApplyBuff("perm_decay", "test");
            Assert.IsTrue(result);
            Assert.AreEqual(1, _buffManager.GetActiveBuffs().Count);
            Assert.IsTrue(_buffManager.HasBuff("perm_decay"));
        }

        [Test]
        public void ApplyBuff_Permanent_Reacquire_IncreasesStack()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));
            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));

            var buff = _buffManager.GetBuff("perm_decay");
            Assert.IsNotNull(buff);
            Assert.AreEqual(2, buff.StackCount);
        }

        [Test]
        public void ApplyBuff_Stack_MaxStack_ReturnsFalse()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("stack_buff", "test"));
            Assert.IsTrue(_buffManager.ApplyBuff("stack_buff", "test"));
            Assert.IsFalse(_buffManager.ApplyBuff("stack_buff", "test"));

            var buff = _buffManager.GetBuff("stack_buff");
            Assert.IsNotNull(buff);
            Assert.AreEqual(2, buff.StackCount);
        }

        [Test]
        public void ApplyBuff_Instant_DoesNotAddToActiveBuffs()
        {
            bool applied = false;
            EventCenter.AddListener<BuffAppliedEvent>(GameEventType.BuffApplied, e =>
            {
                if (e.BuffId == "instant_buff")
                    applied = true;
            });

            bool result = _buffManager.ApplyBuff("instant_buff", "test");
            Assert.IsTrue(result);
            Assert.IsTrue(applied);
            Assert.AreEqual(0, _buffManager.GetActiveBuffs().Count);
        }

        [Test]
        public void ApplyBuff_Timed_Reacquire_RefreshesTime()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("timed_buff", "test"));
            var buff = _buffManager.GetBuff("timed_buff");
            Assert.IsNotNull(buff);
            buff.RemainingTime = 1f;

            Assert.IsTrue(_buffManager.ApplyBuff("timed_buff", "test"));
            Assert.AreEqual(2, buff.StackCount);
            Assert.AreEqual(5f, buff.RemainingTime, 0.01f);
        }

        [Test]
        public void RemoveBuff_ExistingBuff_RemovesSuccessfully()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));
            Assert.AreEqual(1, _buffManager.GetActiveBuffs().Count);

            bool result = _buffManager.RemoveBuff("perm_decay");
            Assert.IsTrue(result);
            Assert.AreEqual(0, _buffManager.GetActiveBuffs().Count);
        }

        [Test]
        public void RemoveBuff_NonExisting_ReturnsFalse()
        {
            bool result = _buffManager.RemoveBuff("non_existent");
            Assert.IsFalse(result);
        }

        [Test]
        public void GetEffectiveValue_PermanentDecay_WorksForStack1And3()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));
            var instance = _buffManager.GetBuff("perm_decay");
            Assert.IsNotNull(instance);

            var modifier = new StatModifier
            {
                Stat = StatType.Attack,
                Type = ModifyType.Flat,
                Value = 10f
            };
            Assert.AreEqual(10f, _buffManager.GetEffectiveValue(instance, modifier), 0.001f);

            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));
            Assert.IsTrue(_buffManager.ApplyBuff("perm_decay", "test"));
            Assert.AreEqual(21.9f, _buffManager.GetEffectiveValue(instance, modifier), 0.001f);

            Assert.IsTrue(_buffManager.ApplyBuff("perm_linear", "test"));
            Assert.IsTrue(_buffManager.ApplyBuff("perm_linear", "test"));
            var linear = _buffManager.GetBuff("perm_linear");
            Assert.AreEqual(20f, _buffManager.GetEffectiveValue(linear, modifier), 0.001f);
        }

        [Test]
        public void GetTotalStatModifier_MultiBuffSummation_Works()
        {
            Assert.IsTrue(_buffManager.ApplyBuff("perm_linear", "test"));
            Assert.IsTrue(_buffManager.ApplyBuff("perm_linear", "test")); // 20
            Assert.IsTrue(_buffManager.ApplyBuff("attack_flat", "test")); // +3
            Assert.IsTrue(_buffManager.ApplyBuff("stack_buff", "test"));  // +5

            float total = _buffManager.GetTotalStatModifier(StatType.Attack, ModifyType.Flat);
            Assert.AreEqual(28f, total, 0.001f);
        }

        [Test]
        public void ApplyBuff_UnknownBuffId_ReturnsFalse()
        {
            bool result = _buffManager.ApplyBuff("not_exists", "test");
            Assert.IsFalse(result);
        }

        private static BuffConfigSO CreateBuffConfig(
            string buffId,
            DurationType duration,
            float durationValue,
            int maxStack,
            float decayRate = 1f,
            params StatModifier[] modifiers)
        {
            var config = ScriptableObject.CreateInstance<BuffConfigSO>();
            SetField(config, "buffId", buffId);
            SetField(config, "duration", duration);
            SetField(config, "durationValue", durationValue);
            SetField(config, "maxStack", maxStack);
            SetField(config, "decayRate", decayRate);
            SetField(config, "modifiers", modifiers ?? new StatModifier[0]);
            return config;
        }

        private static StatModifier CreateModifier(StatType stat, ModifyType type, float value)
        {
            return new StatModifier
            {
                Stat = stat,
                Type = type,
                Value = value
            };
        }

        private static void SetPoolEntries(BuffPoolSO pool, BuffConfigSO[] configs)
        {
            var entries = new BuffEntry[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                entries[i] = new BuffEntry { Buff = configs[i], Weight = 1 };
            }

            SetField(pool, "entries", entries);
        }

        private static void SetBuffPool(BuffManager manager, BuffPoolSO pool)
        {
            SetField(manager, "buffPool", pool);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"字段 {fieldName} 未找到");
            field.SetValue(target, value);
        }
    }
}
