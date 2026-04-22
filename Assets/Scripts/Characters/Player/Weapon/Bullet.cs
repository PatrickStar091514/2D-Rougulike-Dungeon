using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private CircleCollider2D bulletCollider;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifetime); // destroy bullet after lifetime has passed
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Platform") || other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
            Debug.Log("Bullet Destroyed");
        }
    }
}
