using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core;
using RogueDungeon.Core.Buff;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Config;

namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// Buff 管理器，MonoBehaviour 单例，DontDestroyOnLoad 跨场景持久化。
    /// 负责 Buff 的应用、移除、查询、计时过期与事件广播。
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        [SerializeField] private BuffPoolSO buffPool; // 全局 Buff 掉落池

        [Header("Runtime Debug")]
        [SerializeField] private int _debugActiveBuffCount; // 当前激活 Buff 数量

        private static readonly IReadOnlyList<BuffInstance> EmptyActiveBuffs = new List<BuffInstance>(0);
        private List<BuffInstance> _activeBuffs; // 引用 RunState.ActiveBuffs

        /// <summary>
        /// 全局 Buff 掉落池（只读）
        /// </summary>
        public BuffPoolSO BuffPool => buffPool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RegisterEvents();
        }

        private void OnDestroy()
        {
            
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents(); // 防止 sceneLoaded 导致的重复订阅
            EventCenter.AddListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
            EventCenter.AddListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
            EventCenter.AddListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
        }

        private void OnEnable()
        {
            TryInitFromExistingRun();
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
            EventCenter.RemoveListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
            EventCenter.RemoveListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
        }

        private void OnRunReady(RunReadyEvent evt)
        {
            TryInitFromExistingRun();
        }

        private void Update()
        {
            if (_activeBuffs == null) return;
            TickTimedBuffs(Time.deltaTime);
            _debugActiveBuffCount = _activeBuffs.Count;
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.ToState)
            {
                case GameState.RunInit:
                    InitFromRunState();
                    break;
                case GameState.RunEnd:
                case GameState.Hub:
                    Cleanup();
                    break;
            }
        }

        private void OnRoomCleared(RoomClearedEvent evt)
        {
            TickRoomScopedBuffs();
        }

        private void InitFromRunState()
        {
            var runManager = RunManager.Instance;
            if (runManager == null || runManager.CurrentRun == null)
            {
                Debug.LogWarning("[BuffManager] InitFromRunState: RunManager 或 CurrentRun 为 null");
                _activeBuffs = null;
                return;
            }

            if (runManager.CurrentRun.ActiveBuffs == null)
                runManager.CurrentRun.ActiveBuffs = new List<BuffInstance>();

            _activeBuffs = runManager.CurrentRun.ActiveBuffs;
            _debugActiveBuffCount = _activeBuffs.Count;
        }

        private void TryInitFromExistingRun()
        {
            if (_activeBuffs != null) return;
            if (RunManager.Instance?.CurrentRun == null) return;
            InitFromRunState();
        }

        private void Cleanup()
        {
            _activeBuffs = null;
            _debugActiveBuffCount = 0;
        }

        /// <summary>
        /// 通过 BuffId 应用 Buff，包含 5 种持续类型处理逻辑。
        /// </summary>
        /// <param name="buffId">Buff 唯一标识</param>
        /// <param name="sourceId">来源标识</param>
        /// <returns>应用成功返回 true</returns>
        public bool ApplyBuff(string buffId, string sourceId = "")
        {
            if (string.IsNullOrEmpty(buffId))
            {
                Debug.LogWarning("[BuffManager] ApplyBuff: buffId 为空");
                return false;
            }

            var config = GetConfig(buffId);
            if (config == null)
            {
                Debug.LogWarning($"[BuffManager] ApplyBuff: 未找到 BuffConfig '{buffId}'");
                return false;
            }

            return ApplyBuff(config, sourceId);
        }

        /// <summary>
        /// 通过配置对象应用 Buff。
        /// </summary>
        /// <param name="config">Buff 配置</param>
        /// <param name="sourceId">来源标识</param>
        /// <returns>应用成功返回 true</returns>
        public bool ApplyBuff(BuffConfigSO config, string sourceId = "")
        {
            if (config == null)
            {
                Debug.LogWarning("[BuffManager] ApplyBuff: config 为 null");
                return false;
            }

            if (_activeBuffs == null)
            {
                Debug.LogWarning("[BuffManager] ApplyBuff: 未在 Run 中，无法应用 Buff");
                return false;
            }

            if (config.Duration == DurationType.Instant || (config.Duration == DurationType.Timed && config.DurationValue <= 0f))
            {
                BroadcastApplied(config, sourceId, 1);
                return true;
            }

            var existing = GetBuff(config.BuffId);
            if (existing != null)
                return HandleReacquire(existing, config, sourceId);

            var instance = new BuffInstance
            {
                BuffId = config.BuffId,
                StackCount = 1,
                RemainingTime = config.Duration == DurationType.Timed ? config.DurationValue : 0f,
                RemainingRooms = config.Duration == DurationType.RoomScoped ? Mathf.Max(0, Mathf.CeilToInt(config.DurationValue)) : 0,
                SourceId = sourceId
            };

            _activeBuffs.Add(instance);
            BroadcastApplied(config, sourceId, instance.StackCount);
            return true;
        }

        /// <summary>
        /// 移除指定 Buff。
        /// </summary>
        /// <param name="buffId">Buff 唯一标识</param>
        /// <returns>移除成功返回 true</returns>
        public bool RemoveBuff(string buffId)
        {
            if (_activeBuffs == null) return false;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].BuffId != buffId) continue;

                _activeBuffs.RemoveAt(i);
                var config = GetConfig(buffId);
                var duration = config != null ? config.Duration : DurationType.Permanent;
                BroadcastExpired(config);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取激活 Buff 列表（只读）。
        /// </summary>
        public IReadOnlyList<BuffInstance> GetActiveBuffs()
        {
            return _activeBuffs != null ? _activeBuffs.AsReadOnly() : EmptyActiveBuffs;
        }

        /// <summary>
        /// 按 BuffId 查询实例。
        /// </summary>
        public BuffInstance GetBuff(string buffId)
        {
            if (_activeBuffs == null) return null;

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].BuffId == buffId)
                    return _activeBuffs[i];
            }

            return null;
        }

        /// <summary>
        /// 向后兼容旧调用名。
        /// </summary>
        public BuffInstance FindBuff(string buffId)
        {
            return GetBuff(buffId);
        }

        /// <summary>
        /// 判断是否存在指定 Buff。
        /// </summary>
        public bool HasBuff(string buffId)
        {
            return GetBuff(buffId) != null;
        }

        /// <summary>
        /// 汇总指定属性与修改方式的总修正值。
        /// </summary>
        /// <param name="stat">属性类型</param>
        /// <param name="type">修改方式</param>
        /// <returns>总修正值</returns>
        public float GetTotalStatModifier(StatType stat, ModifyType type)
        {
            if (_activeBuffs == null || buffPool == null) return 0f;

            float total = 0f;
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                var instance = _activeBuffs[i];
                var config = GetConfig(instance.BuffId);
                if (config == null || config.Modifiers == null) continue;

                for (int j = 0; j < config.Modifiers.Length; j++)
                {
                    var modifier = config.Modifiers[j];
                    if (modifier.Stat != stat || modifier.Type != type) continue;
                    total += GetEffectiveValue(instance, modifier);
                }
            }

            return total;
        }

        /// <summary>
        /// 计算指定 Buff 实例上某条 Modifier 的有效值。
        /// Permanent 使用指数递减叠加，其它类型按层数线性叠加。
        /// </summary>
        /// <param name="instance">Buff 实例</param>
        /// <param name="modifier">属性修改条目</param>
        /// <returns>有效值</returns>
        public float GetEffectiveValue(BuffInstance instance, StatModifier modifier)
        {
            if (instance == null) return 0f;

            var config = GetConfig(instance.BuffId);
            if (config == null) return 0f;

            int stackCount = Mathf.Max(1, instance.StackCount);
            if (config.Duration != DurationType.Permanent || stackCount == 1)
                return modifier.Value * stackCount;

            float decay = Mathf.Clamp01(config.DecayRate);
            float sum = 0f;
            float factor = 1f;
            for (int i = 0; i < stackCount; i++)
            {
                sum += factor;
                factor *= decay;
            }

            return modifier.Value * sum;
        }

        public int ActiveBuffCount => _activeBuffs?.Count ?? 0;

        /// <summary>
        /// 若当前未配置 BuffPool，则使用外部传入的池进行绑定。
        /// </summary>
        public void BindBuffPoolIfMissing(BuffPoolSO pool)
        {
            if (buffPool == null && pool != null)
                buffPool = pool;
        }

        private bool HandleReacquire(BuffInstance existing, BuffConfigSO config, string sourceId)
        {
            int oldStack = existing.StackCount;

            if (config.MaxStack > 0 && existing.StackCount >= config.MaxStack)
            {
                Debug.LogWarning($"[BuffManager] Buff '{config.BuffId}' 已达最大叠加数 {config.MaxStack}");
                return false;
            }

            existing.StackCount = Mathf.Max(1, existing.StackCount + 1);

            switch (config.Duration)
            {
                case DurationType.Timed:
                    existing.RemainingTime = config.DurationValue;
                    break;
                case DurationType.RoomScoped:
                    existing.RemainingRooms = Mathf.Max(0, Mathf.CeilToInt(config.DurationValue));
                    break;
            }

            existing.SourceId = sourceId;

            if (existing.StackCount != oldStack)
                BroadcastStackChanged(config, oldStack, existing.StackCount);

            BroadcastApplied(config, sourceId, existing.StackCount);
            return true;
        }

        private void TickTimedBuffs(float deltaTime)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _activeBuffs[i];
                var config = GetConfig(buff.BuffId);
                if (config == null || config.Duration != DurationType.Timed) continue;

                buff.RemainingTime -= deltaTime;
                if (buff.RemainingTime > 0f) continue;

                _activeBuffs.RemoveAt(i);
                BroadcastExpired(config);
            }
        }

        private void TickRoomScopedBuffs()
        {
            if (_activeBuffs == null) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _activeBuffs[i];
                var config = GetConfig(buff.BuffId);
                if (config == null || config.Duration != DurationType.RoomScoped) continue;

                buff.RemainingRooms--;
                if (buff.RemainingRooms > 0) continue;

                _activeBuffs.RemoveAt(i);
                BroadcastExpired(config);
            }
        }

        private BuffConfigSO GetConfig(string buffId)
        {
            return buffPool != null ? buffPool.FindByBuffId(buffId) : null;
        }

        private static void BroadcastApplied(BuffConfigSO config, string sourceId, int stackCount)
        {
            EventCenter.Broadcast(GameEventType.BuffApplied, new BuffAppliedEvent
            {
                Snapshot = config.ToSnapshot(),
                SourceId = sourceId,
                StackCount = stackCount
            });
        }

        private static void BroadcastExpired(BuffConfigSO config)
        {
            EventCenter.Broadcast(GameEventType.BuffExpired, new BuffExpiredEvent
            {
                Snapshot = config.ToSnapshot()
            });
        }

        private static void BroadcastStackChanged(BuffConfigSO config, int oldStack, int newStack)
        {
            EventCenter.Broadcast(GameEventType.BuffStackChanged, new BuffStackChangedEvent
            {
                Snapshot = config.ToSnapshot(),
                OldStack = oldStack,
                NewStack = newStack
            });
        }
    }
}
