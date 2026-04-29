using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAttributes : MonoBehaviour
{
    [Header("基础属性（可在Inspector配置）")]
    public int characterID;
    public float baseAttack = 10f;
    public float baseDefense = 2f;
    public float baseMaxHP = 100f;
    public float baseMoveSpeed = 5f;
    public float currentHP;

    [Header("缓存最终属性（基础+Buff加成）")]
    public float finalAttack;
    public float finalDefense;
    public float finalMoveSpeed;


    private void Awake()
    {
        currentHP = baseMaxHP;
        RefreshFinalAttributes();

    }

    public void RefreshFinalAttributes()
    {
        finalAttack = baseAttack;
        finalDefense = baseDefense;
        finalMoveSpeed = baseMoveSpeed;

        
    }
}
