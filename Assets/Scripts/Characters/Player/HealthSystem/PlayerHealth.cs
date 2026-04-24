using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        Init();
    }

    public TMP_Text hpText;

    public int maxHP = 10;
    private int health;

    public int Health
    {
        get
        {
            return health;
        }
        set
        {
            health = value;
            hpText.text = Health + "/" + maxHP;
        }

    }

    public void Init()
    {
        Health = maxHP;
    }
}
