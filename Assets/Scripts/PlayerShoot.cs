using Mirror;
using UnityEngine;
using System;

public class PlayerShoot : NetworkBehaviour
{
    private PlayerState playerState;
    GameObject canvas;
    private CanvasInGameHUD canvasInGameHUD;
    private CrosshairUI crosshairUI;
    private PlayerAnimationStateController animationController;
    private MatchScript matchScript;
    private GameObject hitboxParent;

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
        
        hitboxParent = transform.Find("Root").gameObject;

        canvas = GameObject.Find("Canvas");
        canvasInGameHUD = canvas.GetComponent<CanvasInGameHUD>();

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


        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            ActiveWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
        }
        weaponCooldownTime = 0;
        weaponShootingNoiseValue = 0;
        allowShooting = true;
        animationController.SetCurrentWeapon(selectedWeaponLocal);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.ammo);

        if (hasAuthority)
        {
            crosshairUI = canvas.GetComponent<CrosshairUI>();
            crosshairUI.hitboxParent = hitboxParent;
            crosshairUI.activeWeapon = ActiveWeapon;
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) { return; }
        if (canvasInGameHUD.blockPlayerInput) { return; }
        if (canvasInGameHUD.paused) { return; }
        if (playerState.IsDead) { return; }

        hitboxParent.SetActive(false);
        allowShooting = !Physics.Raycast(
            ActiveWeapon.transform.position + Vector3.up * 0.04f,
            ActiveWeapon.fireLocationTransform.forward,
            out RaycastHit _hitInfo,
            0.85f); // Last argument stands for range (in this case how long the weapon is)

        crosshairUI.DisplayX(allowShooting);
        hitboxParent.SetActive(true);
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

            // Handle animation move speed
            if (_New == 0)
            {
                animationController.SetMoveSpeed(1.4f);
            }
            else
            {
                animationController.SetMoveSpeed(1.2f);
            }

            animationController.SetCurrentWeapon(_New);
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
            canvasInGameHUD.HideCrosshair();
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            canvasInGameHUD.ShowCrosshair();
            selectedWeaponLocal = 1;
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Weapon not properly implemented
            //selectedWeaponLocal = 2;
            //CmdChangeActiveWeapon(selectedWeaponLocal);
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
            hitboxParent.SetActive(false);
            bool _hasHit = Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit _hitInfo,
                ActiveWeapon.range);
            hitboxParent.SetActive(true);

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

    public void ResetWeaponsAmmo()
    {
        foreach (GameObject currentWeaponGameObject in weaponArray)
        {
            Weapon currentWeapon = currentWeaponGameObject.GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.ammo = currentWeapon.totalAmmo;
            }
            Debug.Log(currentWeapon);
        }
        if (ActiveWeapon != null)
        {
            canvasInGameHUD.UpdateAmmoUI(ActiveWeapon.ammo);
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

        // MuzzleFlash
        Instantiate(ActiveWeapon.muzzleFlash, ActiveWeapon.fireLocationTransform.position, Quaternion.LookRotation(-_projectileDir, -_projectileDir));

        GameObject _projectile = Instantiate(ActiveWeapon.projectile, ActiveWeapon.fireLocationTransform.position, ActiveWeapon.fireLocationTransform.rotation);
        StartCoroutine(_projectile.GetComponent<NailProjectile>().Setup(gameObject, ActiveWeapon.projectileLife, ActiveWeapon.projectileSpeed, _projectileDir));
    }

    #endregion
}
