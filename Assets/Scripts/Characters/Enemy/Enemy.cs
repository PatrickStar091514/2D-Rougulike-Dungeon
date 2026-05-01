using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using RogueDungeon.Core.Pool;

public enum EnemyType
{
    Bat, Spider, Ghost, Scorpion
}

[System.Serializable]
public class EnemyData
{
    private int _maxHP;       // ˽���ֶΣ���ֹ�ⲿ�޸�
    private int _currentHP;
    private float _moveSpeed;
    private int _damage;

    // ֻ�����ԣ��ⲿֻ�ܶ�ȡ�������޸�
    public int MaxHP => _maxHP;
    public int CurrentHP => _currentHP;
    public float MoveSpeed => _moveSpeed;
    public int Damage => _damage;

    // ��Enemy���ڲ����޸ĵķ���
    public void SetBaseStats(int maxHP, int currentHP, float moveSpeed, int damage)
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
public class Enemy : MonoBehaviour, IPoolable
{
    public EnemyType enemyType;
    public EnemyData data;

    private void Awake()
    {
        data = new EnemyData();
        InitEnemyByType();
    }

    public void OnPoolGet()
    {
        InitEnemyByType();
    }

    public void OnPoolRelease() { }

    public void InitEnemyByType()
    {
        switch(enemyType)
        {
            case EnemyType.Bat:
                data.SetBaseStats(15, 15, 8, 4);
                break;
            case EnemyType.Spider:
                data.SetBaseStats(20, 20, 3, 5);
                break;
            case EnemyType.Ghost:
                data.SetBaseStats(30, 30, 1, 2);
                break;
            case EnemyType.Scorpion:
                data.SetBaseStats(10, 10, 5, 6);
                break;
        }
    }
}
