using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ShootingPatterns : ScriptableObject
{
    public abstract void Shoot(GameObject bulletPrefab, Transform muzzle, float bulletSpeed);

}
