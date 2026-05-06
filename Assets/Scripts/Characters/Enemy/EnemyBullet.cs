using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private string poolTag = "EnemyBullet";
    private bool hasHit = false;

    public Enemy owner;   // �� ˭�����
    public int damage;
    public void Init(Enemy owner)
    {
        this.owner = owner;
        this.damage = owner.data.Damage;
    }
    private void OnEnable()
    {
        hasHit = false;
        Invoke(nameof(ReturnToPool), lifetime); // ���¼���ʱ������������
    }
    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool)); // ȡ��δִ�еĻ��յ���
    }
    private void ReturnToPool() // �����ӵ�����
    {
        BulletPoolManager.Instance?.ReturnBulletToPool(gameObject, poolTag);
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
        if (collision.CompareTag("Wall"))
        {
            hasHit = true;
            ReturnToPool();
        }
    }
}
