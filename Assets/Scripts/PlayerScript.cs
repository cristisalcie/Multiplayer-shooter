using Mirror;
using System;
using System.Collections.Generic;
using System.Collections;
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

    //public static Vector3 cameraOffset = new Vector3(0.6f, 1.35f, -2f);  // Aligned vertical
    //public static Vector3 cameraOffset = new Vector3(0.095f, 1.35f, -2f);  // Aligned both horizontal and vertical
    public static Vector3 cameraOffset = new Vector3(0.095f, 2f, -2.55f);  // Aligned horizontal
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
    public ScoreboardScript scoreboard;
    private int selectedWeaponLocal;

    private MatchScript matchScript;
    private Transform spawnPoints;

    private void Awake()
    {
        // Find canvasInGameHUD script
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
        scoreboard = GameObject.Find("ScoreboardScript").GetComponent<ScoreboardScript>();

        #region Initialize motion variables/constants

        // Find playerMotion script
        playerMotion = GetComponent<PlayerMotion>();
        baseMoveSpeed = 5f;
        moveSpeed = baseMoveSpeed;
        maxJumps = 1;

        #endregion

        // Find PlayerShoot script
        playerShoot = GetComponent<PlayerShoot>();
        selectedWeaponLocal = 1;

        // Find PlayerState script
        playerState = GetComponent<PlayerState>();

        #region Initialize camera variables/constants

        lookSensitivityH = 5f;
        lookSensitivityV = 5f;

        #endregion

        // Set match script reference
        matchScript = GameObject.Find("SceneScriptsReferences").GetComponent<SceneScriptsReferences>().matchScript;
        spawnPoints = GameObject.Find("SpawnPoints").transform;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isLocalPlayer)
        {
            nameTag.SetActive(false);
        }

        #region Initialize camera variables/constants

        // Lock player on camera. Once taken from scene it will destroy with player prefab unless we set parent back to null (give camera to scene).
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

        #endregion

        // Lock cursor on window blocked in the center.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string _name = "Player" + netIdentity.netId;
        if (((GameNetworkManager)GameNetworkManager.singleton).playerName == null)  // Name was not set by client in Lobby scene
        {
            ((GameNetworkManager)GameNetworkManager.singleton).playerName = _name;
        }
        else  // Name was set by client in Lobby scene
        {
            _name = ((GameNetworkManager)GameNetworkManager.singleton).playerName;
        }

        Color _color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 90);

        matchScript.SetLocalPlayerScript(this);

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
    }

    public void StartForceRespawn()
    {
        CmdForceRespawn();
    }

    private IEnumerator ForceRespawn()
    {
        if (isLocalPlayer)
        {
            CharacterController charCtrl = GetComponent<CharacterController>();
            Camera.main.transform.SetParent(null);  // Give camera to scene
            canvasInGameHUD.crosshair.SetActive(false);

            /* Since we are interpolating the position from NetworkTransform we disable Character Controller
             * to make sure we will not interpolate quickly to spawnpoint and then back at death location. */
            charCtrl.enabled = false;

            // Disable visual of player before teleporting
            transform.Find("Robot2").gameObject.SetActive(false);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(false);    // This gameobject has the weapon

            transform.position = spawnPoints.GetChild(UnityEngine.Random.Range(0, spawnPoints.childCount - 1)).position;  // Should be synced by NetworkTransform

            yield return new WaitForSeconds(MatchScript.respawnDisplayTime);

            transform.Find("Robot2").gameObject.SetActive(true);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(true);    // This gameobject has the weapon
            charCtrl.enabled = true;
            Camera.main.transform.SetParent(transform);  // Give camera back to our player
            charCtrl.Move(Vector3.zero);

            canvasInGameHUD.crosshair.SetActive(true);
            playerState.CmdRespawnPlayer();  // This will reset healthPoints
        }
        else
        {
            transform.Find("Robot2").gameObject.SetActive(false);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(false);    // This gameobject has the weapon
            transform.Find("NameTag").gameObject.SetActive(false); // This gameobject has the NameTag

            yield return new WaitForSeconds(MatchScript.respawnDisplayTime);

            transform.Find("Robot2").gameObject.SetActive(true);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(true);    // This gameobject has the weapon
            transform.Find("NameTag").gameObject.SetActive(true); // This gameobject has the NameTag
        }
    }

    #region Hook functions

    private void OnNameChanged(string _Old, string _New)
    {
        playerNameText.text = playerName;
        playerState.SetPlayerName(playerName);
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
        playerState.SetPlayerName(_name);  // Because on client is changed on hook

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


        // Request the useful scripts to be activated for localPlayer (motor, etc)
        TargetActivateUsefulScripts();

        // Insert and update scoreboard to everyone
        ((GameNetworkManager)GameNetworkManager.singleton).OnServerAppendToScoreboard(netIdentity.netId, playerName);
        RpcInsertIntoScoreboard(netIdentity.netId, playerName);
        TargetSaveScoreboard(((GameNetworkManager)GameNetworkManager.singleton).OnServerGetScoreboardPlayerList());

        // TODO: uncomment this before finishing project
        // Get the match state and make a decision based on it.
        if (NetworkServer.connections.Count < 2)  // Minimum players to start match
        {
            // In here we know we are the first player to join this game
            matchScript.OnServerWaitForPlayers(netIdentity.connectionToClient);
        }
        else
        {
            // Check if the match has started and we finished showing the display sequence
            if (matchScript.MatchStarted && !matchScript.preparingMatch)
            {
                // If it did, update matchStarted boolean on this client then spawn and play
                matchScript.TargetUpdateMatchStarted(netIdentity.connectionToClient, true);

                if (matchScript.preparingFinish)
                {
                    // The match has also finished (player joined at the end)
                    matchScript.OnServerMatchFinished(netIdentity.connectionToClient, null);
                }
            }
            else
            {
                if (matchScript.preparingFinish)
                {
                    matchScript.OnServerMatchFinished(netIdentity.connectionToClient, null);
                }
                else
                {
                    // If it did NOT, get the current countdown and display the appropiate panel
                    matchScript.OnServerPrepareToStart(netIdentity.connectionToClient);
                }
            }
        }
    }

    [Command]
    public void CmdForceRespawn()
    {
        RpcForceRespawn();
    }

    [Command]
    private void CmdChangeMyName(uint _uniqueId, string _newPlayerName)
    {
        RpcReceive($"{playerName} changed name to {_newPlayerName} since there is a name conflict with another client", false);
        playerState.SetPlayerName(_newPlayerName);
        playerName = _newPlayerName;
        RpcChangeNameInScoreboard(_uniqueId, _newPlayerName);
    }

    [Command]
    public void CmdIncreaseKillTestFunc()
    {
        ((GameNetworkManager)GameNetworkManager.singleton).OnServerIncrementScoreboardKillsOf(netIdentity.netId);
        RpcIncrementScoreboardKillsOf(netIdentity.netId);
    }

    [Command]
    public void CmdIncreaseDeathTestFunc()
    {
        ((GameNetworkManager)GameNetworkManager.singleton).OnServerIncrementScoreboardDeathsOf(netIdentity.netId);
        RpcIncrementScoreboardDeathsOf(netIdentity.netId);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    public void RpcReceive(string _message, bool _isPlayerMsg)
    {
        OnMessage?.Invoke(this, _message, _isPlayerMsg);
    }

    [ClientRpc]
    public void RpcForceRespawn()
    {
        StartCoroutine(ForceRespawn());
    }

    [ClientRpc]
    public void RpcIncrementScoreboardKillsOf(uint _uniqueId)
    {
        scoreboard.IncrementKillsOf(_uniqueId);
    }

    [ClientRpc]
    public void RpcIncrementScoreboardDeathsOf(uint _uniqueId)
    {
        scoreboard.IncrementDeathsOf(_uniqueId);
    }

    [ClientRpc]
    public void RpcRemoveFromScoreboard(uint _uniqueId)
    {
        scoreboard.Remove(_uniqueId);
    }

    [ClientRpc(includeOwner = false)]
    public void RpcInsertIntoScoreboard(uint _uniqueId, string _playerName)
    {
        scoreboard.Append(_uniqueId, _playerName);
    }

    [ClientRpc]
    public void RpcChangeNameInScoreboard(uint _uniqueId, string _newPlayerName)
    {
        scoreboard.ChangePlayerNameInScoreboard(netIdentity.netId, _newPlayerName);
    }

    #endregion

    #region TargetRpc

    [TargetRpc]
    public void TargetActivateUsefulScripts()
    {
        GetComponent<CharacterController>().enabled = true;
        GetComponent<PlayerMotor>().enabled = true;
    }

    [TargetRpc]
    private void TargetSaveScoreboard(List<GameNetworkManager.ScoreboardData> _scoreboardPlayerList)
    {
        string _playerName = ((GameNetworkManager)GameNetworkManager.singleton).playerName;
        string _newPlayerName = $"{_playerName}_{netIdentity.netId}";

        scoreboard.SaveScoreboardOnClient(_scoreboardPlayerList);
        if (scoreboard.PlayerNameExists(netIdentity.netId, _playerName))
        {
            CmdChangeMyName(netIdentity.netId, $"{_playerName}_{netIdentity.netId}");
        }
    }

    #endregion
}