using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private CircleCollider2D bulletCollider;
    private string poolTag = "PlayerBullet";
    private float attackDamage;

    public void SetAttackDamage(float damage)
    {
        attackDamage = damage;
    }

    private void Awake()
    {
        bulletCollider = GetComponent<CircleCollider2D>();
        bulletCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), lifetime); // 重新激活时重置生命周期
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool)); // 取消未执行的回收调用

    }

    private void ReturnToPool() // 回收子弹到池
    {
        BulletPoolManager.Instance.ReturnBulletToPool(gameObject, poolTag);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Platform"))
        {
            ReturnToPool(); // 碰撞后回收，而非销毁
        }

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.data.TakeDamage((int)attackDamage);
            }
            ReturnToPool();
        }

    }
}
