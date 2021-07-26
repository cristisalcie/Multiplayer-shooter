﻿using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(PlayerMotion))]
[RequireComponent(typeof(PlayerShoot))]
public class PlayerScript : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;

    private PlayerMotion playerMotion;

    private PlayerShoot playerShoot;

    private Vector3 cameraOffset;


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

    [SerializeField]
    private float baseMoveSpeed;

    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private float lookSensitivityH;

    [SerializeField]
    private float lookSensitivityV;

    [SerializeField]
    private int maxJumps;

    private int selectedWeaponLocal;

    [SyncVar]
    private int kills;

    [SyncVar]
    private int deaths;

    private void Awake()
    {
        // Find canvasInGameHUD script
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        // Find playerMotion script
        playerMotion = GetComponent<PlayerMotion>();

        // Find PlayerShoot script
        playerShoot = GetComponent<PlayerShoot>();

        cameraOffset = new Vector3(0.5f, 1.4f, -2f);

        baseMoveSpeed = 6f;
        moveSpeed = baseMoveSpeed;
        lookSensitivityH = 5f;
        lookSensitivityV = 5f;
        selectedWeaponLocal = 1;
        maxJumps = 1;
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

        // Lock player on camera. Once taken from scene it will destroy with player prefab
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

        // Set raw camera object
        GameObject _rawCameraObject = GameObject.Find("RawCameraTransform");
        _rawCameraObject.transform.SetParent(transform);
        _rawCameraObject.transform.localPosition = cameraOffset;
        _rawCameraObject.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);


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

        // Handle weapon switching
        int currentSelectedWeapon = playerShoot.HandleSwitchWeaponInput();

        // Check and update the moving speed depending on current weapon
        if (currentSelectedWeapon != selectedWeaponLocal)  // Weapon changed
        {
            if (currentSelectedWeapon == 0)  // Meele
            {
                moveSpeed *= 1.2f;
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

        // Handle camera movement (will rotate on vertical axis)
        playerMotion.MoveCamera(lookSensitivityV);

        // Test scoreboard sorting by having a way of increasing kills/deaths for a player
        if (Input.GetKeyDown(KeyCode.K))
        {
            CmdIncreaseKillTestFunc();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            CmdIncreaseDeathTestFunc();
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
        playerName = _name;
        playerColor = _col;
        RpcReceive($"{playerName} joined".Trim(), false);

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