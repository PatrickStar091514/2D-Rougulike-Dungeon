using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    public TMP_Text hpText;
    public int maxHP = 50;

    private int health;

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
            Destroy(gameObject); // ∑¿÷ÿ∏¥
        }

        Init();
    }

    public void Init()
    {
        Health = maxHP;
    }

    public void PlayerTakeDamage(int damage)
    {
        Health -= damage;
        Health = Mathf.Max(0, Health);

        if (Health <= 0)
        {
            Destroy(gameObject);
        }
    }
}