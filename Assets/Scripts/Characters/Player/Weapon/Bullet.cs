using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private CircleCollider2D bulletCollider;

    private void Awake()
    {
        bulletCollider = GetComponent<CircleCollider2D>();
        bulletCollider.isTrigger = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifetime); // destroy bullet after lifetime has passed
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Platform") || collision.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }

    }
}
