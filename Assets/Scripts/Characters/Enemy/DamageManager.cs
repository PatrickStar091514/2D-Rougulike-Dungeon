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
    [SerializeField] private Enemy enemy;
    [SerializeField] private int enemyHP;
    [SerializeField] private PlayerCharacter playerCha;
    [SerializeField] private int currentPlayerAttack;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyHP = enemy.data.CurrentHP;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            playerCha = player.GetComponent<PlayerCharacter>();
        }
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
        UpdatePlayerAttack();
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
