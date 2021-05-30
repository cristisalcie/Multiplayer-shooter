using Mirror;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    private SceneScript sceneScript;

    [SerializeField]
    private int selectedWeaponLocal;
    [SerializeField]
    private GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    [SerializeField]
    private int activeWeaponSynced;

    private Weapon activeWeapon;
    [SerializeField]
    private float weaponCooldownTime;

    void Awake()
    {
        selectedWeaponLocal = 1;
        activeWeaponSynced = 1;

        // Disable all weapons
        foreach (var item in weaponArray)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
        weaponArray[selectedWeaponLocal].SetActive(true);

        // Allow all players to run this
        sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
            sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }
        weaponCooldownTime = 1;
    }

    void OnWeaponChanged(int _Old, int _New)
    {
        // Disable old weapon
        // in range and not null
        if (0 <= _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)
        {
            weaponArray[_Old].SetActive(false);
        }

        // Enable new weapon
        // in range and not null
        if (0 <= _New && _New < weaponArray.Length && weaponArray[_New] != null)
        {
            weaponArray[_New].SetActive(true);
            if (_New != 0)  // Meele weapon not implemented
            {
                activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                if (isLocalPlayer) { sceneScript.UIAmmo(activeWeapon.weaponAmmo); }
            }
        }
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    [Command]
    void CmdShootRay()
    {
        RpcFireWeapon();
    }

    [ClientRpc]
    void RpcFireWeapon()
    {
        //bulletAudio.Play(); muzzleflash  etc
        var bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
        if (bullet) { Destroy(bullet, activeWeapon.weaponLife); }
    }

    public void HandleSwitchWeaponInput()
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
    }

    public void HandleShootWeaponInput()
    {
        if (Input.GetButtonDown("Fire1"))  // Fire1 is mouse 1st click
        {
            if (activeWeapon && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0)
            {
                if (selectedWeaponLocal != 0)  // Meele weapon not implemented
                {
                    weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                    activeWeapon.weaponAmmo -= 1;
                    sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                    CmdShootRay();
                }
            }
        }
    }

    public void SetupPlayerShoot(SceneScript _sceneScript)
    {
        sceneScript = _sceneScript;
    }
}
