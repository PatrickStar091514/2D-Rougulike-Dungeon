using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private SpriteRenderer weaponSprite;
    [SerializeField] private ShootingPatterns shootingPattern;
    [SerializeField] public float attackDamage = 5f;
    private float shootTimer = 0f; // time since last shot
    
    private bool isVisible = false;
    private bool isShooting = false;

    private PlayerInput input;

    private void Awake()
    {
        //weaponSprite.enabled = false;
        input = GetComponent<PlayerInput>();
    }

    // Start is called before the first frame update
    void Start()
    {
        weaponSprite.enabled = isVisible;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Shoot"))
        {
            shootTimer = fireRate;
            weaponSprite.enabled = true;
        }

        if (Input.GetButton("Shoot"))
        {
            shootTimer += Time.deltaTime;
            if (shootTimer > fireRate)
            {
                shootTimer = 0f;
                Shoot();
            }
        }
    }

    void WeaponVisible()
    {
        if (input.shootPressed)
        {
            // toggle whether weapon is visible
            isVisible = !isVisible;

            // update sprite based on new state
            weaponSprite.enabled = isVisible;

        }
    }

    void Shoot()
    {
        // delegate shooting to modular shooting pattern
        //if (shootingPattern != null)
        //{
        //    shootingPattern.Shoot(bulletPrefab, firePoint, bulletSpeed);
        //}

        // 注意：需要修改 ShootingPatterns.Shoot 方法，使其使用对象池
        // 临时方案：直接在PlayerShoot中获取子弹（如果ShootingPatterns是自定义类）
        GameObject bullet = BulletPoolManager.Instance.GetBulletFromPool(
            "PlayerBullet",
            firePoint.position,
            firePoint.rotation
        );
        if (bullet != null)
        {
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = firePoint.right * bulletSpeed; // 按发射方向赋值速度
            }
        }
        // 原逻辑：shootingPattern.Shoot(bulletPrefab, firePoint, bulletSpeed);
        // 需同步修改 ShootingPatterns.Shoot 方法，传入池标签而非预制体
    }
}
