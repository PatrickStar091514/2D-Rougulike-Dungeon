using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private string poolTag = "EnemyBullet";

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
        if (collision.CompareTag("Player"))
        {
            PlayerHealth.Instance.Health -= 1;
            ReturnToPool();
        }
        if (collision.CompareTag("Platform"))
        {
            ReturnToPool();
        }
    }
}
