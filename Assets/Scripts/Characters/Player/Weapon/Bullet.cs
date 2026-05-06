using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private CircleCollider2D bulletCollider;
    private string poolTag = "PlayerBullet";

    private void Awake()
    {
        bulletCollider = GetComponent<CircleCollider2D>();
        bulletCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), lifetime); // ���¼���ʱ������������
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool)); // ȡ��δִ�еĻ��յ���

    }

    private void ReturnToPool() // �����ӵ�����
    {
        BulletPoolManager.Instance.ReturnBulletToPool(gameObject, poolTag);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall") || collision.CompareTag("Enemy"))
        {
            Debug.Log("Return to Pool");
            ReturnToPool(); // ��ײ����գ���������
        }
    }
}
