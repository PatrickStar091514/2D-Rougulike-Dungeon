using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public enum EnemyType
{
    Bat, Spider, Ghost, Scorpion
}

[System.Serializable]
public class EnemyData
{
    private int _maxHP;       // 私有字段，禁止外部修改
    private int _currentHP;
    private int _moveSpeed;
    private int _damage;

    // 只读属性，外部只能读取，不能修改
    public int MaxHP => _maxHP;
    public int CurrentHP => _currentHP;
    public int MoveSpeed => _moveSpeed;
    public int Damage => _damage;

    // 仅Enemy类内部可修改的方法
    public void SetBaseStats(int maxHP, int currentHP, int moveSpeed, int damage)
    {
        _maxHP = maxHP;
        _currentHP = currentHP;
        _moveSpeed = moveSpeed;
        _damage = damage;
    }

    public void TakeDamage(int damage)
    {
        _currentHP -= damage;
        _currentHP = Mathf.Max(0, _currentHP);
    }
}
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType;
    public EnemyData data;

    private void Awake()
    {
        data = new EnemyData();
        InitEnemyByType();
    }
    public void InitEnemyByType()
    {
        switch(enemyType)
        {
            case EnemyType.Bat:
                data.SetBaseStats(15, 15, 4, 3);
                break;
            case EnemyType.Spider:
                data.SetBaseStats(10, 10, 4, 5);
                break;
            case EnemyType.Ghost:
                data.SetBaseStats(30, 30, 2, 2);
                break;
            case EnemyType.Scorpion:
                data.SetBaseStats(10, 10, 3, 6);
                break;
        }
    }
}
