using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RogueDungeon.Core.Events;


public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    public TMP_Text hpText;
    public int maxHP = 50;

    private int health;

    public int playerScore = 10;
    [SerializeField] private AudioClip _playerHitClip; // 玩家受击音效

    public int Health
    {
        get => health;
        set
        {
            health = value;
            hpText.text = health + "/" + maxHP;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // ���ظ�
        }

        Init();
    }

    public void Init()
    {
        Health = maxHP;
    }

    public void PlayerTakeDamage(int damage)
    {
        if (Health <= 0) return; // 已经死亡，不再处理伤害
        Health -= damage;
        Health = Mathf.Max(0, Health);
        RogueDungeon.Core.AudioManager.Instance?.PlaySFX(RogueDungeon.Core.SFXType.PlayerHit); // 播放受击音效

        if (Health <= 0)
        {
            EventCenter.Broadcast(GameEventType.GetScore, new GetScoreEvent { 
                ScoreDelta = -playerScore,
                SourceType = GetScoreEvent.ScoreSourceType.PlayerDeath }); // 触发结算分数事件
            EventCenter.Broadcast(GameEventType.PlayerDied); // 触发游戏结束事件
            Destroy(gameObject);
        }
    }
}