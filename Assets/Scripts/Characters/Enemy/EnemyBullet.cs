using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private string poolTag = "EnemyBullet";
    private bool hasHit = false;

    public Enemy owner;   // ← 谁发射的
    public int damage;
    public void Init(Enemy owner)
    {
        this.owner = owner;
        this.damage = owner.data.Damage;
    }
    private void OnEnable()
    {
        hasHit = false;
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
        if (hasHit) return;
        if (collision.CompareTag("Player"))
        {
            hasHit = true;
            PlayerHealth player = collision.GetComponent<PlayerHealth>();

            if (player != null)
            {
                player.PlayerTakeDamage(damage);
            }
            ReturnToPool();
        }
        if (collision.CompareTag("Platform"))
        {
            hasHit = true;
            ReturnToPool();
        }
    }
}
