using UnityEngine;
using RogueDungeon.Core.Events;
using System;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;

namespace RogueDungeon.Core
{
    /// <summary>
    /// 音频管理器，DDOL 单例，管理背景音乐和音效播放。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioClip _bgmClip; // 背景音乐
        [SerializeField] private AudioClip _attackClip; // 攻击音效（玩家射击）
        [SerializeField] private AudioClip _playerHitClip; // 玩家受击音效

        private Dictionary<SFXType, AudioClip> _sfxClips; // 音效字典

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f; // 主音量（0~1）

        private AudioSource _musicSource; // BGM 循环播放源
        private AudioSource _sfxSource; // SFX 一次性播放源
        private bool _isBGMPlaying; // BGM 是否正在播放（自有标志，不受 AudioSource 场景生命周期影响）


        /// <summary>
        /// 主音量（0~1），同时影响 BGM 和 SFX
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                ApplyVolume();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            SetupSFX(out _sfxClips);

            ApplyVolume();
        }

        private void Start()
        {
            PlayBGM(_bgmClip);
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents();
            EventCenter.AddListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                ApplyVolume();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.ToState)
            {
                case GameState.Hub:
                case GameState.RoomPlaying:
                case GameState.BossPlaying:
                    PlayBGM(_bgmClip);
                    break;
            }
        }

        private void SetupSFX(out Dictionary<SFXType, AudioClip> sfxDict)
        {
            sfxDict = new Dictionary<SFXType, AudioClip>
            {
                { SFXType.Attack, _attackClip },
                { SFXType.PlayerHit, _playerHitClip }
            };
        }

        /// <summary>
        /// 播放背景音乐（循环）
        /// </summary>
        /// <param name="clip">BGM AudioClip，为 null 时静默跳过</param>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || _musicSource == null) return;
            if (_isBGMPlaying && _musicSource.clip == clip) return;

            _musicSource.clip = clip;
            _musicSource.Play();
            _isBGMPlaying = true;
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBGM()
        {
            if (_musicSource != null)
                _musicSource.Stop();
            _isBGMPlaying = false;
        }

        /// <summary>
        /// 播放一次性音效
        /// </summary>
        /// <param name="type">SFXType，用于标识音效类型</param>
        public void PlaySFX(SFXType type)
        {
            if (_sfxClips != null && _sfxClips.TryGetValue(type, out AudioClip clip))
            {
                if (clip != null && _sfxSource != null)
                    _sfxSource.PlayOneShot(clip, _masterVolume);
            }
        }

        private void ApplyVolume()
        {
            if (_musicSource != null) _musicSource.volume = _masterVolume;
            if (_sfxSource != null) _sfxSource.volume = _masterVolume;
        }

        public void SetMasterVolume(float volumePercent)
        {
            MasterVolume = volumePercent;
            ApplyVolume();
        }

    }

    public enum SFXType
    {
        Attack,
        PlayerHit,
    }
}
