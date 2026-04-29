using RogueDungeon.Core.Buff;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DamageManager : MonoBehaviour
{
    private Enemy enemy;
    private PlayerCharacter playerCha;
    private int currentPlayerAttack;



    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            playerCha = player.GetComponent<PlayerCharacter>();
        }
    }

    private void OnEnable()
    {
        if (playerCha != null)
        {
            EventCenter.AddListener<BuffAppliedEvent>(GameEventType.BuffApplied, OnBuffApplied);
            EventCenter.AddListener<BuffExpiredEvent>(GameEventType.BuffExpired, OnBuffExpired);
            EventCenter.AddListener<BuffStackChangedEvent>(GameEventType.BuffStackChanged, OnBuffStackChanged);
            EventCenter.AddListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);

            UpdatePlayerAttack();
        }
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<BuffAppliedEvent>(GameEventType.BuffApplied, OnBuffApplied);
        EventCenter.RemoveListener<BuffExpiredEvent>(GameEventType.BuffExpired, OnBuffExpired);
        EventCenter.RemoveListener<BuffStackChangedEvent>(GameEventType.BuffStackChanged, OnBuffStackChanged);
        EventCenter.RemoveListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);

    }
    private void OnBuffApplied(BuffAppliedEvent evt)
    {
        UpdatePlayerAttack();
    }

    private void OnBuffExpired(BuffExpiredEvent evt)
    {
        UpdatePlayerAttack();
    }

    private void OnBuffStackChanged(BuffStackChangedEvent evt)
    {
        UpdatePlayerAttack();
    }

    private void OnRunReady(RunReadyEvent evt)
    {
        UpdatePlayerAttack();
    }

    private void UpdatePlayerAttack()
    {
        if (playerCha != null)
        {
            currentPlayerAttack = (int)playerCha.GetCurrentAttackDamage();
        }
        else
        {
            currentPlayerAttack = (int)GetComponent<PlayerShoot>().attackDamage;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerBullet"))
        {
            Destroy(collision.gameObject);

            enemy.data.TakeDamage(currentPlayerAttack);
            if (enemy.data.CurrentHP <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

}
