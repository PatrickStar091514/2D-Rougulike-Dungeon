using UnityEngine;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Save;

namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// Run 生命周期管理器，负责 RunState 的创建、中途存档、归档和恢复。
    /// 监听 GameStateChanged 事件，在 RunInit 时创建或恢复 RunState，在 RoomClear 时中途存档，在 RunEnd 后清理。
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        private const string RunSaveKey = "run_checkpoint"; // 续关存档 key
        [SerializeField] private int _randomSeed; // 可配置的随机种子，0 表示使用随机生成的种子

        [SerializeField] private RunState _currentRun; // 当前 Run 状态（仅 Run 进行中有效）

        /// <summary>
        /// 当前 Run 状态（仅在 Run 进行中有效，Hub 阶段为 null）
        /// </summary>
        public RunState CurrentRun => _currentRun;

        /// <summary>
        /// 是否存在可续关的 Run 存档
        /// </summary>
        public bool HasCheckpoint => SaveManager.HasSave(RunSaveKey);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventCenter.AddListener<GameStateChangedEvent>(
                Core.Events.GameEventType.GameStateChanged, OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<GameStateChangedEvent>(
                Core.Events.GameEventType.GameStateChanged, OnGameStateChanged);
        }

        /// <summary>
        /// 响应游戏状态切换，管理 RunState 生命周期
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.ToState)
            {
                case GameState.RunInit:
                    CreateOrResumeRun();
                    break;
                case GameState.RoomClear:
                    SaveCheckpoint();
                    break;
                case GameState.RunEnd:
                    ArchiveRun();
                    break;
                case GameState.Hub:
                    ReleaseRun();
                    break;
            }
        }

        /// <summary>
        /// 创建新 Run 或从存档恢复续关
        /// </summary>
        private void CreateOrResumeRun()
        {
            if (HasCheckpoint)
            {
                _currentRun = SaveManager.LoadRaw<RunState>(RunSaveKey);
                Debug.Log($"[RunManager] Run 已恢复: {_currentRun.RunId}, Floor: {_currentRun.FloorIndex}");
            }
            else
            {
                _currentRun = new RunState
                {
                    RunId = System.Guid.NewGuid().ToString("N"),
                    // Seed = _randomSeed != 0 ? Random.Range(int.MinValue, int.MaxValue) : _randomSeed
                    Seed = Random.Range(int.MinValue, int.MaxValue)
                };
                _randomSeed = _currentRun.Seed; // 同步随机种子到 Inspector 显示
                Debug.Log($"[RunManager] Run 已创建: {_currentRun.RunId}, Seed: {_currentRun.Seed}");
            }

            EventCenter.Broadcast(GameEventType.RunReady, new RunReadyEvent { Run = CurrentRun });
        }

        /// <summary>
        /// 在房间清空时保存中途存档（续关点）
        /// </summary>
        private void SaveCheckpoint()
        {
            if (_currentRun == null) return;
            SaveManager.SaveRaw(RunSaveKey, _currentRun);
            Debug.Log($"[RunManager] 存档点已保存: Floor {_currentRun.FloorIndex}, Room {_currentRun.RoomIndex}");
        }

        /// <summary>
        /// Run 结束时归档并删除续关存档，同时释放当前 Run 引用
        /// </summary>
        private void ArchiveRun()
        {
            if (_currentRun == null)
            {
                Debug.LogWarning("[RunManager] ArchiveRun 时 CurrentRun 为 null");
                return;
            }

            // 删除续关存档（Run 已正常结束）
            string runId = _currentRun.RunId;
            SaveManager.DeleteSave(RunSaveKey);
            _currentRun = null;
            Debug.Log($"[RunManager] Run 已归档并释放: {runId}");
        }

        /// <summary>
        /// 释放 RunState 引用
        /// </summary>
        private void ReleaseRun()
        {
            _currentRun = null;
        }
    }
}
