using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SingleShotPattern", menuName = "Shooting Patterns/Single Shot")]

public class SingleShotPattern : ShootingPatterns
{
    public override void Shoot(GameObject bulletPrefab, Transform muzzle, float bulletSpeed)
    {
        if (bulletPrefab == null || muzzle == null) return;
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = muzzle.right * bulletSpeed;
        }
    }
}
