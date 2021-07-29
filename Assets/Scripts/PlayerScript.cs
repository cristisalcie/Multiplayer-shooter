using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(PlayerMotion))]
[RequireComponent(typeof(PlayerShoot))]
[RequireComponent(typeof(PlayerState))]
public class PlayerScript : NetworkBehaviour
{
    #region Motion related variables/constants

    private PlayerMotion playerMotion;
    [SerializeField]
    private float baseMoveSpeed;
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private int maxJumps;

    #endregion

    #region Camera related variables/constants

    private Vector3 cameraOffset;
    [SerializeField]
    private float lookSensitivityH;
    [SerializeField]
    private float lookSensitivityV;

    #endregion

    #region Chat and player UI variables/constants

    private CanvasInGameHUD canvasInGameHUD;
    [SerializeField]
    private Text playerNameText;
    [SerializeField]
    private RawImage playerNameTextBackground;
    [SerializeField]
    private GameObject nameTag;
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;
    [SerializeField]
    [SyncVar(hook = nameof(OnColorChanged))]
    private Color playerColor = Color.white;
    public static event Action<PlayerScript, string, bool> OnMessage;

    #endregion

    private PlayerState playerState;
    private PlayerShoot playerShoot;
    private int selectedWeaponLocal;

    [SyncVar]
    private int kills;
    [SyncVar]
    private int deaths;

    private void Awake()
    {
        // Find canvasInGameHUD script
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        #region Initialize motion variables/constants

        // Find playerMotion script
        playerMotion = GetComponent<PlayerMotion>();
        baseMoveSpeed = 6f;
        moveSpeed = baseMoveSpeed;
        maxJumps = 1;

        #endregion

        // Find PlayerShoot script
        playerShoot = GetComponent<PlayerShoot>();
        selectedWeaponLocal = 1;

        // Find PlayerState script
        playerState = GetComponent<PlayerState>();

        #region Initialize camera variables/constants

        cameraOffset = new Vector3(0.5f, 1.4f, -2f);
        lookSensitivityH = 5f;
        lookSensitivityV = 5f;

        #endregion

        kills = 0;
        deaths = 0;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isLocalPlayer)
        {
            nameTag.SetActive(false);
        }

        #region Initialize camera variables/constants

        Debug.Log(Camera.main.transform.parent + ": is parent of main camera");

        // Lock player on camera. Once taken from scene it will destroy with player prefab unless we set parent back to null (give camera to scene).
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

        // Set raw camera object
        GameObject _rawCameraObject = GameObject.Find("RawCameraTransform");
        _rawCameraObject.transform.SetParent(transform);
        _rawCameraObject.transform.localPosition = cameraOffset;
        _rawCameraObject.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

        #endregion

        // Lock cursor on window blocked in the center.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string _name = "Player" + UnityEngine.Random.Range(100, 999);

        Color _color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 90);

        CmdSetupPlayer(_name, _color);
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            // Make non-local players run this. Only local player has camera
            nameTag.transform.LookAt(Camera.main.transform);
            return;
        }
        // If it is not paused(in options panel) & it had chat active & we clicked on screen 
        if (canvasInGameHUD.blockPlayerInput && !canvasInGameHUD.paused && Input.GetMouseButtonDown(0))
        {
            // Lock cursor and set it to be invisible
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Unlock player input and hide chat send text box
            canvasInGameHUD.blockPlayerInput = false;
            canvasInGameHUD.chatMessage.SetActive(false);
        }
        // If we blocked player input we return here.
        if (canvasInGameHUD.blockPlayerInput) { return; }

        if (playerState.IsDead) { return; }

        // Handle weapon switching
        int currentSelectedWeapon = playerShoot.HandleSwitchWeaponInput();

        // Check and update the moving speed depending on current weapon
        if (currentSelectedWeapon != selectedWeaponLocal)  // Weapon changed
        {
            if (currentSelectedWeapon == 0)  // Meele
            {
                moveSpeed *= 1.4f;
                maxJumps = 2;
            }
            else  // Anything else
            {
                moveSpeed = baseMoveSpeed;
                maxJumps = 1;
            }

            selectedWeaponLocal = currentSelectedWeapon;
        }

        // Handle weapon shooting
        playerShoot.HandleShootWeaponInput();

        // Handle player movement and horizontal rotation
        playerMotion.MovePlayer(moveSpeed, maxJumps, lookSensitivityH);

        if (Camera.main.transform.parent == transform)  // Camera is watching us => Allowed to receive input
        {
            // Handle camera movement (will rotate on vertical axis)
            playerMotion.MoveCamera(lookSensitivityV);
        }

        // Test scoreboard sorting by having a way of increasing kills/deaths for a player
        if (Input.GetKeyDown(KeyCode.K))
        {
            Camera.main.transform.SetParent(null);
            //CmdIncreaseKillTestFunc();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            Camera.main.transform.SetParent(transform);
            //CmdIncreaseDeathTestFunc();
        }
    }

    #region Hook functions

    private void OnNameChanged(string _Old, string _New)
    {
        playerNameText.text = playerName;
    }

    private void OnColorChanged(Color _Old, Color _New)
    {
        playerNameTextBackground.color = _New;
    }

    #endregion

    #region Commands

    [Command]
    public void CmdSendPlayerMessage(string _message)
    {
        if (_message.Trim() != "")
            RpcReceive(_message.Trim(), true);
    }

    [Command]
    public void CmdSetupPlayer(string _name, Color _col)
    {
        // Player info sent to server, then server updates sync vars which handles it on all clients
        playerState.SetupState();

        playerName = _name;
        playerColor = _col;
        RpcReceive($"{playerName} joined".Trim(), false);

        // Request player state data to be sent to the player that just joined
        foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
        {
            if (netIdentity.connectionToClient == connection.Value)  // Skip current script's attached GameObject
            {
                continue;
            }
            PlayerState _playerState = connection.Value.identity.gameObject.GetComponent<PlayerState>();

            // Goes back to owner of playerState script (the player who just joined) and sets state.
            // First parameter is sent to know what gameObject to modify and second parameter is the value
            // that needs to be set of the above gameObject because the player who just joined doesn't know it.
            playerState.TargetSetHealthPoints(_playerState.gameObject, _playerState.HealthPoints);
        }

        // Insert and update scoreboard
        ((GameNetworkManager)GameNetworkManager.singleton).InsertIntoScoreboard(playerName);
        RpcUpdateScoreboard(((GameNetworkManager)GameNetworkManager.singleton).GetScoreboardPlayerList());
    }

    [Command]
    public void CmdIncreaseKillTestFunc()
    {
        ++kills;
        ((GameNetworkManager)GameNetworkManager.singleton).IncrementScoreboardKillsOf(playerName);
        RpcUpdateScoreboard(((GameNetworkManager)GameNetworkManager.singleton).GetScoreboardPlayerList());
    }

    [Command]
    public void CmdIncreaseDeathTestFunc()
    {
        ++deaths;
        ((GameNetworkManager)GameNetworkManager.singleton).IncrementScoreboardDeathsOf(playerName);
        RpcUpdateScoreboard(((GameNetworkManager)GameNetworkManager.singleton).GetScoreboardPlayerList());
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    public void RpcReceive(string _message, bool _isPlayerMsg)
    {
        OnMessage?.Invoke(this, _message, _isPlayerMsg);
    }

    [ClientRpc]
    public void RpcUpdateScoreboard(List<GameNetworkManager.ScoreboardData> _scoreboardPlayerList)
    {
        canvasInGameHUD.UpdateScoreboardUI(_scoreboardPlayerList);
    }

    #endregion
}