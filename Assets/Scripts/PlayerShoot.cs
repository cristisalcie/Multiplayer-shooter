using Mirror;
using UnityEngine;
using System;

public class PlayerShoot : NetworkBehaviour
{
    private PlayerState playerState;
    private CanvasInGameHUD canvasInGameHUD;
    private AnimationStateController animationController;
    private MatchScript matchScript;

    [SerializeField]
    private int selectedWeaponLocal;
    [SerializeField]
    private GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    [SerializeField]
    private int activeWeaponSynced;

    public Weapon ActiveWeapon { get; private set; }
    private float weaponCooldownTime;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        animationController = GetComponent<AnimationStateController>();
        matchScript = GameObject.Find("SceneScriptsReferences").GetComponent<SceneScriptsReferences>().matchScript;

        selectedWeaponLocal = 1;
        activeWeaponSynced = 1;

        // Disable all weapons
        foreach (GameObject item in weaponArray)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
        // Enable only the selected one
        weaponArray[selectedWeaponLocal].SetActive(true);

        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            ActiveWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
        }
        weaponCooldownTime = 0;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.weaponAmmo);
    }

    private void OnWeaponChanged(int _Old, int _New)
    {
        // Disable old weapon
        if (0 <= _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)  // In range and not null
        {
            weaponArray[_Old].SetActive(false);
        }

        // Enable new weapon
        if (0 <= _New && _New < weaponArray.Length && weaponArray[_New] != null)  // In range and not null
        {
            weaponArray[_New].SetActive(true);
            if (_New != 0)  // Meele weapon not implemented
            {
                ActiveWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                if (isLocalPlayer) { canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.weaponAmmo); }
            }
        }
    }


    /// <summary> Handles weapon switching </summary>
    /// <returns> Selected weapon </returns>
    public int HandleSwitchWeaponInput()
    {
        float _mouseScrollWheelInput = Input.GetAxisRaw("Mouse ScrollWheel");
        if (_mouseScrollWheelInput > 0)
        {
            selectedWeaponLocal += 1;

            if (selectedWeaponLocal >= weaponArray.Length)
            {
                selectedWeaponLocal = 0;
            }

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        else if (_mouseScrollWheelInput < 0)
        {
            selectedWeaponLocal -= 1;

            if (selectedWeaponLocal < 0)
            {
                selectedWeaponLocal = weaponArray.Length - 1;
            }

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedWeaponLocal = 0;
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedWeaponLocal = 1;
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedWeaponLocal = 2;
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        return selectedWeaponLocal;
    }

    /// <summary> Called in PlayerScript update function </summary>
    public void HandleShootWeaponInput()
    {
        bool _isShooting = Input.GetButton("Fire1") && ActiveWeapon && ActiveWeapon.weaponAmmo > 0 && selectedWeaponLocal != 0 && Time.time > weaponCooldownTime
            && matchScript.MatchStarted && !matchScript.MatchFinished;
        if (_isShooting)
        {
            weaponCooldownTime = Time.time + ActiveWeapon.weaponCooldown;
        }
        animationController.ShootWeapon(_isShooting);
    }

    /// <summary> Called by firing animation event </summary>
    public void ShootWeaponBullet()
    {
        if (selectedWeaponLocal != 0 && hasAuthority)  // Meele weapon not implemented
        {
            ActiveWeapon.weaponAmmo -= 1;
            canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.weaponAmmo);

            // Calculate raycast from camera to match with crosshair location, see what/who it hits and then sync to everyone
            bool _hasHit = Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit _hitInfo,
                ActiveWeapon.weaponRange);

            #region Debug code

            if (_hasHit)
            {
                Debug.Log(_hitInfo.transform.name);
            }
            // For visual trail of where bullet is supposed to hit
            Debug.DrawRay(
                    Camera.main.transform.position,
                    Camera.main.transform.forward * ActiveWeapon.weaponRange,
                    Color.yellow,
                    30f
                );


            // For visual trail of the bullet trajectory:
            Debug.DrawLine(
                ActiveWeapon.weaponFireTransform.position,
                _hitInfo.point,
                Color.red,
                30f);

            #endregion

            if (_hasHit)
            {
                if (_hitInfo.transform.CompareTag("Player"))
                {
                    int _damage = 0;

                    //Debug.Log($"Hit player {_hitInfo.transform.GetComponentInParent<PlayerScript>().playerName} in {_hitInfo.transform.name}");
                    if (_hitInfo.transform.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log("hit player head inside if");
                        _damage = 340;
                    }
                    else if (_hitInfo.transform.name.IndexOf("ribs", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log("hit player ribs inside if");
                        _damage = 170;
                    }
                    else if (_hitInfo.transform.name.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log("hit player hips inside if");
                        _damage = 120;
                    }
                    else if (_hitInfo.transform.name.IndexOf("thigh", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 100;
                        Debug.Log("hit player thigh inside if");
                    }
                    else if (_hitInfo.transform.name.IndexOf("knee", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 80;
                        Debug.Log("hit player knee inside if");
                    }
                    else if (_hitInfo.transform.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 50;
                        Debug.Log("hit player toe inside if");
                    }
                    else if (_hitInfo.transform.name.IndexOf("forearm", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 80;
                        Debug.Log("hit player forearm inside if");
                    }
                    else if (_hitInfo.transform.name.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 100;
                        Debug.Log("hit player arm inside if");
                    }
                    else if (_hitInfo.transform.name.IndexOf("wrist", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _damage = 50;
                        Debug.Log("hit player wrist inside if");
                    }

                    //GameObject _parent = _hitInfo.transform.GetComponentInParent<PlayerScript>().gameObject;
                    // Or like this
                    GameObject _parent = _hitInfo.transform.root.gameObject;
                    playerState.CmdHit(_parent, _damage);
                }
            }

            CmdShootRay();
        }
    }

    #region Commands

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    [Command]
    private void CmdShootRay()
    {
        RpcFireWeapon();
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void RpcFireWeapon()
    {
        // todo: audio play, draw/instantiate visual effects of raycast
        //bulletAudio.Play(); muzzleflash  etc
        var bullet = Instantiate(ActiveWeapon.weaponBullet, ActiveWeapon.weaponFireTransform.position, ActiveWeapon.weaponFireTransform.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * ActiveWeapon.weaponBulletSpeed;
        if (bullet) { Destroy(bullet, ActiveWeapon.weaponBulletLife); }
    }

    #endregion
}
