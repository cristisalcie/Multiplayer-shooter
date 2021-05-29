using Mirror;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScript : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;

    private SceneScript sceneScript;

    [SerializeField] private int selectedWeaponLocal = 1;
    public GameObject weaponHolder;
    public GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 1;

    private Weapon activeWeapon;
    private float weaponCooldownTime;

    private PlayerMotor motor;
    public float moveSpeed = 50.0f;

    private Vector3 cameraOffset = new Vector3(0.0f, 0.25f, 0.0f);
    public float lookSensitivityH = 8f;
    public float lookSensitivityV = 5f;
    //private float rotH = 0.0f;
    //private float rotV = 0.0f;

    public Text playerNameText;
    public RawImage playerNameTextBackground;
    public GameObject nameTag;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    public static event Action<PlayerScript, string, bool> OnMessage;

    void OnNameChanged(string _Old, string _New)
    {
        playerNameText.text = playerName;
    }

    void OnColorChanged(Color _Old, Color _New)
    {
        playerNameTextBackground.color = _New;
        //playerMaterialClone = new Material(GetComponent<Renderer>().material);
        //playerMaterialClone.color = _New;
        //GetComponent<Renderer>().material = playerMaterialClone;
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

    void Awake()
    {
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
        weaponCooldownTime = 0;

        // Find canvasInGameHUD script.
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
    }

    private void Start()
    {
        motor = GetComponent<PlayerMotor>();
    }

    public override void OnStartLocalPlayer()
    {
        // Lock player on camera. Once taken from scene it will destroy with player prefab.
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        // Have weapons be affected by camera rotation (up and down)
        weaponHolder.transform.SetParent(Camera.main.transform);


        // Lock cursor on window blocked in the center.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        sceneScript.playerScript = this;

        string name = "Player" + UnityEngine.Random.Range(100, 999);

        Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 90);
        CmdSetupPlayer(name, color);
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    [Command]
    public void CmdSendPlayerMessage(string message, bool isPlayerMsg)
    {
        if (message.Trim() != "")
            RpcReceive(message.Trim(), isPlayerMsg);
    }


    [Command]
    public void CmdSetupPlayer(string _name, Color _col)
    {
        // Player info sent to server, then server updates sync vars which handles it on all clients
        playerName = _name;
        playerColor = _col;
        canvasInGameHUD.chatWindow.GetComponent<ChatWindow>().OnPlayerJoin($"{playerName} joined.");
    }

    [Command]
    void CmdShootRay()
    {
        RpcFireWeapon();
    }
    
    [ClientRpc]
    public void RpcReceive(string message, bool isPlayerMsg)
    {
        OnMessage?.Invoke(this, message, isPlayerMsg);
    }

    [ClientRpc]
    void RpcFireWeapon()
    {
        //bulletAudio.Play(); muzzleflash  etc
        var bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
        if (bullet) { Destroy(bullet, activeWeapon.weaponLife); }
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            // Make non-local players run this
            nameTag.transform.LookAt(Camera.main.transform);
            nameTag.transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }
        // If it is not paused(in options panel) & it had chat active & we clicked on screen 
        if (canvasInGameHUD.blockPlayerInput && !canvasInGameHUD.paused && Input.GetMouseButtonDown(0))
        {
            // Lock cursor and set it to be invisible
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Unlolck player input and hide chat send text box
            canvasInGameHUD.blockPlayerInput = false;
            canvasInGameHUD.chatMessage.SetActive(false);
        }
        // If we blocked player input we return here.
        if (canvasInGameHUD.blockPlayerInput) { return; }


        MovePlayer();
        MoveCamera();


        HandleSwitchWeaponInput();
        HandleShootWeaponInput();
    }
    
    private void MovePlayer()
    {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");

        Vector3 _movHorizontal = transform.right * _movX;
        Vector3 _movVertical = transform.forward * _movZ;

        Vector3 _velocity = (_movHorizontal + _movVertical).normalized * moveSpeed;

        motor.Move(_velocity);

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxisRaw("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * lookSensitivityH;
        motor.Rotate(_rotation);
    }

    private void MoveCamera()
    {
        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _xRot = Input.GetAxisRaw("Mouse Y") * lookSensitivityV;
        Vector3 _cameraRotation = new Vector3(_xRot, 0f, 0f);
        motor.RotateCamera(_cameraRotation);
    }

    private void HandleSwitchWeaponInput()
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

    private void HandleShootWeaponInput()
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
}
