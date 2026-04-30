using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Core.Buff;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    // 1. 基础属性配置（根据角色实际属性调整）
    [Header("基础属性")]
    [SerializeField] private float baseMoveSpeed = 8f;       // 基础移速
    [SerializeField] private float baseAttackDamage = 5f;   // 基础攻击力（示例扩展属性）
    [SerializeField] private float baseMaxHP = 50f;

    [Header("Buff后属性")]
    [SerializeField] private float finalSpeed;
    [SerializeField] private float finalDamage;
    [SerializeField] private float finalHP;

    // 2. 关联角色功能组件（根据实际组件名调整）
    [Header("角色组件")]
    [SerializeField] private PlayerMovement playerMovement; // 移动组件
    [SerializeField] private PlayerShoot playerShoot;     // 攻击组件（示例扩展）
    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        // 自动获取组件（避免手动赋值遗漏）
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerShoot == null)
            playerShoot = GetComponent<PlayerShoot>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        // 3. 注册Buff系统事件（核心：监听Buff变化触发属性刷新）
        EventCenter.AddListener<BuffAppliedEvent>(GameEventType.BuffApplied, OnBuffApplied);
        EventCenter.AddListener<BuffExpiredEvent>(GameEventType.BuffExpired, OnBuffExpired);
        EventCenter.AddListener<BuffStackChangedEvent>(GameEventType.BuffStackChanged, OnBuffStackChanged);
        EventCenter.AddListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
    }

    private void Start()
    {
        // 4. 初始化属性（首次刷新）
        RefreshAllStats();
    }

    private void OnDisable()
    {
        // 5.注销事件（必加：避免内存泄漏 / 空引用）
        EventCenter.RemoveListener<BuffAppliedEvent>(GameEventType.BuffApplied, OnBuffApplied);
        EventCenter.RemoveListener<BuffExpiredEvent>(GameEventType.BuffExpired, OnBuffExpired);
        EventCenter.RemoveListener<BuffStackChangedEvent>(GameEventType.BuffStackChanged, OnBuffStackChanged);
        EventCenter.RemoveListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);

    }

    // 6. 核心：属性刷新逻辑（复用示例的计算公式）
    /// 刷新所有受Buff影响的属性（可拆分单个属性刷新，如仅刷新移速）
    private void RefreshAllStats()
    {
        if (BuffManager.Instance == null)
        {
            ResetToBaseStats();
            return;
        }
        RefreshMoveSpeed();
        RefreshAttackDamage();
        RefreshPlayerHp();
    }

    private void RefreshPlayerHp()
    {
        if (playerHealth == null) return;

        float flatMod = BuffManager.Instance.GetTotalStatModifier(StatType.MaxHP, ModifyType.Flat);
        float percentMod = BuffManager.Instance.GetTotalStatModifier(StatType.MaxHP, ModifyType.Percent);
        finalHP = (baseMaxHP + flatMod) / (1f + percentMod);
        int hpOffset = (int)finalHP - playerHealth.maxHP;

        playerHealth.maxHP = (int)Mathf.Max(10f,finalHP);
        playerHealth.Health += hpOffset;
    }

    private void RefreshAttackDamage()
    {
        if (playerShoot == null) return;

        float flatMod = BuffManager.Instance.GetTotalStatModifier(StatType.Attack, ModifyType.Flat);
        float percentMod = BuffManager.Instance.GetTotalStatModifier(StatType.Attack, ModifyType.Percent);
        finalDamage = (baseAttackDamage + flatMod) * (1f + percentMod);

        playerShoot.attackDamage = Mathf.Max(1f, finalDamage); // 攻击力最低为1
    }

    public float GetCurrentAttackDamage()
    {
        if (playerShoot != null) return playerShoot.attackDamage;
        return baseAttackDamage;
    }

    private void RefreshMoveSpeed()
    {
        if (playerMovement == null) return;

        float flatMod = BuffManager.Instance.GetTotalStatModifier(StatType.MoveSpeed, ModifyType.Flat);
        float percentMod = BuffManager.Instance.GetTotalStatModifier(StatType.MoveSpeed, ModifyType.Percent);
        finalSpeed = (baseMoveSpeed + flatMod) * (1f + percentMod);

        playerMovement.speed = Mathf.Max(0f, finalSpeed);
    }

    private void ResetToBaseStats()
    {
        if (playerMovement != null)
            playerMovement.speed = baseMoveSpeed;
        if (playerShoot != null)
            playerShoot.attackDamage = baseAttackDamage;
    }

    // 7. 事件响应：Buff应用/过期/层数变化时刷新属性
    private void OnBuffApplied(BuffAppliedEvent evt)
    {
        RefreshAllStats();
    }

    private void OnBuffExpired(BuffExpiredEvent evt)
    {
        RefreshAllStats();
    }

    private void OnBuffStackChanged(BuffStackChangedEvent evt)
    {
        RefreshAllStats();
    }

    private void OnRunReady(RunReadyEvent evt)
    {
        RefreshAllStats();
    }
}
