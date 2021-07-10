using Mirror;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;

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

    private void Awake()
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

        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
            canvasInGameHUD.UpdateAmmoUI(activeWeapon.weaponAmmo);
        }
        weaponCooldownTime = 1;
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
                activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                if (isLocalPlayer) { canvasInGameHUD.UpdateAmmoUI(activeWeapon.weaponAmmo); }
            }
        }
    }

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

    [ClientRpc]
    private void RpcFireWeapon()
    {
        //bulletAudio.Play(); muzzleflash  etc
        var bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
        if (bullet) { Destroy(bullet, activeWeapon.weaponLife); }
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
                    canvasInGameHUD.UpdateAmmoUI(activeWeapon.weaponAmmo);
                    CmdShootRay();
                }
            }
        }
    }
}
