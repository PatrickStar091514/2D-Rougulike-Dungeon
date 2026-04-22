using System.Collections.Generic;
using UnityEngine;
using RogueDungeon.Core.Events;

namespace RogueDungeon.Core
{
    /// <summary>
    /// 游戏状态机单例，管理全局游戏状态的切换与广播。
    /// 使用 DontDestroyOnLoad 跨场景存活，所有状态切换通过 ChangeState 单一入口。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState _currentState = GameState.Boot; // 当前游戏状态

        /// <summary>
        /// 当前游戏状态（只读）
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// 白名单状态迁移矩阵：Key 为当前状态，Value 为该状态允许迁移到的目标集合
        /// </summary>
        private static readonly Dictionary<GameState, HashSet<GameState>> _transitionMatrix = new()
        {
            { GameState.Boot,         new HashSet<GameState> { GameState.Hub } },
            { GameState.Hub,          new HashSet<GameState> { GameState.RunInit } },
            { GameState.RunInit,      new HashSet<GameState> { GameState.RoomPlaying } },
            { GameState.RoomPlaying,  new HashSet<GameState> { GameState.RoomClear, GameState.BossPlaying, GameState.RunEnd } },
            { GameState.RoomClear,    new HashSet<GameState> { GameState.RewardSelect } },
            { GameState.RewardSelect, new HashSet<GameState> { GameState.RoomPlaying, GameState.RunEnd } },
            { GameState.BossPlaying,  new HashSet<GameState> { GameState.RewardSelect, GameState.RunEnd } },
            { GameState.RunEnd,       new HashSet<GameState> { GameState.Hub } },
        };

        /// <summary>
        /// 活跃状态集合（timeScale = 1）。
        /// 非活跃阶段（Boot/RunInit/RoomClear/RewardSelect/RunEnd）暂停时间流动。
        /// </summary>
        private static readonly HashSet<GameState> _activeTimeStates = new()
        {
            GameState.Hub,
            GameState.RoomPlaying,
            GameState.BossPlaying,
            GameState.RewardSelect
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyTimeScale(CurrentState);
        }

        private void OnEnable()
        {
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        /// <summary>
        /// 切换游戏状态的唯一入口。非法迁移记录 LogWarning 并忽略，不抛异常。
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState == CurrentState)
            {
                Debug.LogWarning($"[GameManager] 重复切换到当前状态 {CurrentState}，已忽略");
                return;
            }

            if (!_transitionMatrix.TryGetValue(CurrentState, out var allowed) || !allowed.Contains(newState))
            {
                Debug.LogWarning($"[GameManager] 非法状态迁移: {CurrentState} → {newState}，已忽略");
                return;
            }

            GameState from = _currentState;
            _currentState = newState;

            ApplyTimeScale(newState);

            EventCenter.Broadcast(Events.GameEventType.GameStateChanged, new GameStateChangedEvent
            {
                FromState = from,
                ToState = newState,
                RunId = string.Empty
            });
        }

        /// <summary>
        /// 根据目标状态设置 Time.timeScale
        /// </summary>
        private void ApplyTimeScale(GameState state)
        {
            Time.timeScale = _activeTimeStates.Contains(state) ? 1f : 0f;
        }

        /// <summary>
        /// 地牢就绪后自动从 RunInit 进入 RoomPlaying，开启游戏内时间流动。
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            if (CurrentState != GameState.RunInit) return;
            ChangeState(GameState.RoomPlaying);
        }
    }
}
