using Mirror;
using System;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerShoot))]
public class PlayerScript : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;

    private SceneScript sceneScript;

    private PlayerController playerController;

    private PlayerShoot playerShoot;

    private Vector3 cameraOffset;

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
    }

    void Awake()
    {
        cameraOffset = new Vector3(0.0f, 0.4f, 0.0f);

        // Allow all players to run this
        sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

        // Find canvasInGameHUD script
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        // Find PlayerController script
        playerController = GetComponent<PlayerController>();

        // Find PlayerShoot script
        playerShoot = GetComponent<PlayerShoot>();
    }

    public override void OnStartLocalPlayer()
    {
        // Lock player on camera. Once taken from scene it will destroy with player prefab.
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

        // Have weapons be affected by camera rotation (up and down)
        //weaponHolder.transform.SetParent(Camera.main.transform);

        // Lock cursor on window blocked in the center.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        sceneScript.playerScript = this;

        string name = "Player" + UnityEngine.Random.Range(100, 999);

        Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 90);
        CmdSetupPlayer(name, color);

        playerShoot.SetupPlayerShoot(sceneScript);
    }

    [Command]
    public void CmdSendPlayerMessage(string message)
    {
        if (message.Trim() != "")
            RpcReceive(message.Trim(), true);
    }


    [Command]
    public void CmdSetupPlayer(string _name, Color _col)
    {
        // Player info sent to server, then server updates sync vars which handles it on all clients
        playerName = _name;
        playerColor = _col;
        RpcReceive($"{playerName} joined.".Trim(), false);
    }

    
    [ClientRpc]
    public void RpcReceive(string message, bool isPlayerMsg)
    {
        OnMessage?.Invoke(this, message, isPlayerMsg);
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            // Make non-local players run this
            nameTag.transform.LookAt(Camera.main.transform);
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


        playerController.MovePlayer();
        playerController.MoveCamera();

        playerShoot.HandleSwitchWeaponInput();
        playerShoot.HandleShootWeaponInput();
    }
}
