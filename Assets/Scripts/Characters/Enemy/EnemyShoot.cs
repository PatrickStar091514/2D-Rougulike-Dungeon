using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class EnemyShoot : MonoBehaviour
{
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int minBulletCount = 1; //min no. bullet to spawn
    [SerializeField] private int maxBulletCount = 5; //max no. bullet to spawn
    [SerializeField] private float bulletSpreadAngle = 3f; //spread angle for multiple bullets
    [SerializeField] private Enemy enemy;

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
        enemy = GetComponent<Enemy>();
        nextFireTime = Time.time;
        if (fireRate <= 0.1f) fireRate = 0.5f;
        if (bulletPrefab == null) enabled = false; //disable script if no bullet prefab
        if (detectionRadius <= 0) detectionRadius = 5f;
        FindPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            isPlayerDetected = false;
            return;
        }

        PlayerDetection();
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
        GameObject bullet = BulletPoolManager.Instance?.GetBulletFromPool(
            "EnemyBullet",
            rigidbody.position,
            Quaternion.identity
            );
        if (bullet == null) return;

        EnemyBullet bulletobj = bullet.GetComponent<EnemyBullet>();
        bulletobj.Init(enemy);

        if (bullet != null)
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = Vector2.zero;
                bulletRb.velocity = direction * bulletSpeed;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    // step 4
    void PlayerDetection()
    {
        float distanceToPlayer = Vector2.Distance(rigidbody.position, playerRb != null ? playerRb.position : (Vector2)player.position);
        isPlayerDetected = distanceToPlayer <= detectionRadius;
        if (isPlayerDetected && !wasPlayerDetectedLastFrame)
        {
            // shoot immediately when player enters range of detection
            nextFireTime = Time.time;
        }
        if (isPlayerDetected && Time.time >= nextFireTime)
        {
            ShootAtPlayer();
            nextFireTime = Time.time + 1f / fireRate;
        }

        wasPlayerDetectedLastFrame = isPlayerDetected;
    }

    // step 5
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    
}
