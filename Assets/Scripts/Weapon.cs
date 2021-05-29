using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float weaponSpeed = 15.0f;
    public float weaponLife = 3.0f;
    public float weaponCooldown = 0.1f;
    public int weaponAmmo = 15;

    public GameObject weaponBullet;
    public Transform weaponFirePosition;
}
