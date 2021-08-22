using Mirror;
using UnityEngine;
using System;

public class PlayerShoot : NetworkBehaviour
{
    private PlayerState playerState;
    private CanvasInGameHUD canvasInGameHUD;
    private CrosshairUI crosshairUI;
    private PlayerAnimationStateController animationController;
    private MatchScript matchScript;
    private GameObject hitBoxParent;

    [SerializeField]
    private int selectedWeaponLocal;
    [SerializeField]
    private GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    [SerializeField]
    private int activeWeaponSynced;

    public Weapon ActiveWeapon { get; private set; }
    private float weaponCooldownTime;
    private float weaponShootingNoiseValue;
    private const float weaponShootingMaxNoiseValue = 0.03f;
    private bool allowShooting;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        animationController = GetComponent<PlayerAnimationStateController>();
        matchScript = GameObject.Find("SceneScriptsReferences").GetComponent<SceneScriptsReferences>().matchScript;
        hitBoxParent = transform.Find("Root").gameObject;

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

        GameObject _canvas = GameObject.Find("Canvas");
        canvasInGameHUD = _canvas.GetComponent<CanvasInGameHUD>();
        crosshairUI = _canvas.GetComponent<CrosshairUI>();

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            ActiveWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
            crosshairUI.activeWeapon = ActiveWeapon;
        }
        weaponCooldownTime = 0;
        weaponShootingNoiseValue = 0;
        allowShooting = true;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.ammo);
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) { return; }
        if (canvasInGameHUD.blockPlayerInput) { return; }
        if (canvasInGameHUD.paused) { return; }
        if (playerState.IsDead) { return; }

        hitBoxParent.SetActive(false);
        allowShooting = !Physics.Raycast(
            ActiveWeapon.transform.position + Vector3.up * 0.04f,
            ActiveWeapon.fireLocationTransform.forward,
            out RaycastHit _hitInfo,
            0.85f); // Last argument stands for range (in this case how long the weapon is)
        Debug.DrawRay(
            ActiveWeapon.transform.position + Vector3.up * 0.04f,
            ActiveWeapon.fireLocationTransform.forward * 0.85f,
            Color.red,
            0.1f);
        if (!allowShooting)
        {
            Debug.Log(_hitInfo.transform.name);
        }
        crosshairUI.DisplayX(allowShooting);
        hitBoxParent.SetActive(true);
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
                if (isLocalPlayer) { canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.ammo); }
            }
        }

        if (hasAuthority)
        {
            crosshairUI.activeWeapon = ActiveWeapon;
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
        bool _isShooting = Input.GetButton("Fire1") && ActiveWeapon && ActiveWeapon.ammo > 0 && selectedWeaponLocal != 0 && Time.time > weaponCooldownTime
            && matchScript.MatchStarted && !matchScript.MatchFinished && allowShooting;
        if (_isShooting)
        {
            float _previousWeaponCooldownTime = weaponCooldownTime;

            weaponCooldownTime = Time.time + ActiveWeapon.cooldown;

            // Increase weapon fire rate noise if holding down firing
            if (weaponCooldownTime - _previousWeaponCooldownTime > 4 * ActiveWeapon.cooldown)
            {
                weaponShootingNoiseValue = 0;
            }
            else
            {
                weaponShootingNoiseValue = weaponShootingMaxNoiseValue;
            }
        }
        animationController.ShootWeapon(_isShooting);
    }

    /// <summary> Called by firing animation event but only runs on the client that has authority object </summary>
    public void ShootWeaponBullet()
    {
        if (hasAuthority)  // Meele weapon not implemented
        {
            ActiveWeapon.ammo -= 1;
            canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.ammo);

            // Calculate raycast from camera to match with crosshair location, see what/who it hits and then sync to everyone
            hitBoxParent.SetActive(false);
            bool _hasHit = Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit _hitInfo,
                ActiveWeapon.range);
            hitBoxParent.SetActive(true);

            Vector3 _bulletDir;
            if (_hasHit)
            {
                _bulletDir = (_hitInfo.point - ActiveWeapon.fireLocationTransform.position).normalized;
            } 
            else
            {
                _bulletDir = Camera.main.transform.forward;
            }

            _bulletDir += weaponShootingNoiseValue
                * new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));

            CmdShootRay(_bulletDir.normalized);  // Very important to send this argument normalized
        }
    }

    #region Commands

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    [Command]
    private void CmdShootRay(Vector3 _bulletDir)
    {
        RpcFireWeapon(_bulletDir);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void RpcFireWeapon(Vector3 _projectileDir)
    {
        // todo: audio play, draw/instantiate visual effects
        //bulletAudio.Play(); muzzleflash  etc
        GameObject _projectile = Instantiate(ActiveWeapon.projectile, ActiveWeapon.fireLocationTransform.position, ActiveWeapon.fireLocationTransform.rotation);
        _projectile.GetComponent<NailProjectile>().Setup(gameObject, ActiveWeapon.projectileLife, ActiveWeapon.projectileSpeed, _projectileDir);
    }

    #endregion
}
