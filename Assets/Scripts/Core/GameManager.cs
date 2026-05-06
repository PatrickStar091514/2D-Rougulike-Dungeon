using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [SerializeField] private int seed = 0;

        public int Seed {get; set;}

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
            SceneManager.sceneLoaded += OnSceneLoaded;

            ApplyTimeScale(CurrentState);
        }

        private void OnEnable()
        {
            Debug.Log($"[DEBUG] GameManager.OnEnable");
            RegisterEvents();
        }

        private void OnDisable()
        {
            Debug.Log($"[DEBUG] GameManager.OnDisable");
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnregisterEvents();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[DEBUG] GameManager.OnSceneLoaded scene={scene.name} mode={mode} CurrentState={CurrentState}");
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents();
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
            Debug.Log($"[DEBUG] GameManager.RegisterEvents 完成");
        }

        private void UnregisterEvents()
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

            Debug.Log($"[DEBUG] GameManager.ChangeState: {from} → {newState}");

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
        /// 从 StartPanel 调用，加载地牢场景并启动游戏流程。
        /// 使用协程等一帧，确保场景内所有组件 Awake/OnEnable 及 sceneLoaded 回调全部完成。
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log($"[DEBUG] GameManager.StartNewGame: 加载场景1, CurrentState={CurrentState}");
            SceneManager.LoadScene(1);
            Debug.Log($"[DEBUG] GameManager.StartNewGame: LoadScene 返回, 启动协程, CurrentState={CurrentState}");
            StartCoroutine(StartNewGameRoutine());
        }

        private System.Collections.IEnumerator StartNewGameRoutine()
        {
            Debug.Log($"[DEBUG] GameManager.StartNewGameRoutine: 开始等待一帧, CurrentState={CurrentState}");
            yield return null;
            Debug.Log($"[DEBUG] GameManager.StartNewGameRoutine: 等待结束, CurrentState={CurrentState}, 开始推进状态");
            ChangeState(GameState.Hub);
            ChangeState(GameState.RunInit);
        }

        /// <summary>
        /// 地牢就绪后自动从 RunInit 进入 RoomPlaying，开启游戏内时间流动。
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            Debug.Log($"[DEBUG] GameManager.OnDungeonReady 收到, CurrentState={CurrentState}");
            if (CurrentState != GameState.RunInit) return;
            ChangeState(GameState.RoomPlaying);
        }
    }
}
