using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float weaponBulletSpeed;
    public float weaponBulletLife;
    public int weaponAmmo;
    public float weaponCooldown;
    public int weaponRange;

    public GameObject weaponBullet;
    public Transform weaponFireTransform;

    private void Awake()
    {
        weaponBulletSpeed = 30.0f;
        weaponAmmo = 15000;
        weaponBulletLife = 3.0f;
        weaponCooldown = 0.05f;
        weaponRange = 200;
    }
}
