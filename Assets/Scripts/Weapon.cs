﻿using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float projectileSpeed;
    public float projectileLife;
    public int ammo;
    public float cooldown;
    public int range;  // Only used as a distance for raycasts/linecasts

    public GameObject projectile;
    public Transform fireLocationTransform;

    public GameObject muzzleFlash;

    private void Awake()
    {
        projectileSpeed = 800.0f;
        ammo = 15000;
        projectileLife = 10.0f;
        cooldown = 0.05f;
        range = 300;  // Should be the entire map from a corner to another
    }
}
