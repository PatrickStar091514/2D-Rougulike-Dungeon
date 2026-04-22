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
    //[SerializeField] private PlayerMovement flip;

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
        //if (shootTimer > 0f)
        //{
        //    shootTimer -= Time.deltaTime;
        //}
        //ShootBullet();
        //WeaponVisible();

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

    void ShootBullet()
    {
        if (isVisible)
        {
            if (input.shootPressed && shootTimer <= 0)
            {
                isShooting = true;
                shootTimer = fireRate;
                Shoot();
            }
            else if (input.shootPressed == false)
            {
                isShooting = false;
            }
        }
        else { return; }
    }

    void Shoot()
    {
        // delegate shooting to modular shooting pattern
        if (shootingPattern != null)
        {
            shootingPattern.Shoot(bulletPrefab, firePoint, bulletSpeed);
        }
    }
}
