using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int minBulletCount = 1; //min no. bullet to spawn
    [SerializeField] private int maxBulletCount = 5; //max no. bullet to spawn
    [SerializeField] private float bulletSpreadAngle = 3f; //spread angle for multiple bullets

    private Transform player;
    private Rigidbody2D playerRb;
    private float nextFireTime;
    private bool isPlayerDetected;
    private bool wasPlayerDetectedLastFrame;
    private Rigidbody2D rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        nextFireTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // step 1
    void FindPlayer()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
        {
            player = obj.transform;
            playerRb = obj.GetComponent<Rigidbody2D>();
        }
    }

    // step 2
    void ShootAtPlayer()
    {
        if (player == null || bulletPrefab == null) return;
        float lerpRatio = UnityEngine.Random.Range(0f, 1f);
        int bulletCount = Mathf.RoundToInt(Mathf.Lerp(minBulletCount, maxBulletCount, lerpRatio));
        Vector2 playerPos = playerRb != null ? playerRb.position : (Vector2)player.position;
        Vector2 direction = (playerPos - rigidbody.position).normalized;

        if (bulletCount == 1)
        {
            SpawnBullet(direction);
        }
        else
        {
            float startAngle = -bulletSpreadAngle * (bulletCount - 1) / 2f;
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + i * bulletSpreadAngle;
                Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * direction;
                SpawnBullet(spreadDir);
            }
        }
    }

    // step 3
    void SpawnBullet(Vector2 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, rigidbody.position, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * bulletSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // step 4
    void PlayerDetection()
    {

    }
}
