using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float projectileSpeed;
    public float projectileLife;
    public int ammo;
    public float cooldown;
    public int range;

    public GameObject projectile;
    public Transform fireLocationTransform;

    private void Awake()
    {
        projectileSpeed = 800.0f;
        ammo = 15000;
        projectileLife = 10.0f;
        cooldown = 0.05f;
        range = 200;
    }
}
