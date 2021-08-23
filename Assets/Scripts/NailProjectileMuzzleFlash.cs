using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NailProjectileMuzzleFlash : MonoBehaviour
{
    [SerializeField]
    private GameObject muzzleFlash;

    private void Start()
    {
        Vector3 _direction = -GetComponent<NailProjectile>().direction;
        Instantiate(muzzleFlash, transform.position, Quaternion.LookRotation(_direction, _direction));
    }
}
