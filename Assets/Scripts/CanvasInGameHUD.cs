using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CanvasInGameHUD : MonoBehaviour
{
    private enum PlayerScoreboardItemField {
        Rank,
        Username,
        Kills,
        Deaths,
        GetSize
    }

    public bool paused;
    public bool blockPlayerInput;

    [SerializeField]
	private Button buttonStop;
	
    [SerializeField]
	private GameObject panelOptions;

	public GameObject panelScoreboard;

    [SerializeField]
	private GameObject panelRespawn;

    [SerializeField]
	private GameObject crosshair;

    [SerializeField]
	private GameObject chatWindow;

	public GameObject chatMessage;

    #region Text variables/constants

    [SerializeField]
	private Text serverText;
    [SerializeField]
	private Text clientText;
    [SerializeField]
    private Text ammoText;
    [SerializeField]
    private Text healthPointsText;

    public Text killedByText;
    public Text respawnSecondsText;

    #endregion

    #region Scoreboard variables/constants/classes

    private GameObject[] scoreboardPlayerListUI;

    #endregion

    private void Awake()
    {
        paused = false;
        blockPlayerInput = false;

        #region Initialize scoreboard list

        scoreboardPlayerListUI = new GameObject[GameNetworkManager.singleton.maxConnections];

        GameObject _playerList = transform.Find("PanelScoreboard/PlayerList").gameObject;
        for (int i = 0; i < GameNetworkManager.singleton.maxConnections; ++i)
        {
            scoreboardPlayerListUI[i] = _playerList.transform.GetChild(i).gameObject;
            for (int j = 0; j < (int)PlayerScoreboardItemField.GetSize ; ++j)
            {
                Text t = scoreboardPlayerListUI[i].transform.GetChild(j).GetComponent<Text>();
                t.text = string.Empty;
            }
        }

        #endregion
    }

    private void Start()
    {
        // Make sure to attach these Buttons in the Inspector
        buttonStop.onClick.AddListener(ButtonStop);
        SetupCanvas();
    }

    private void Update()
    {
        HandlePauseResumeInput();
        HandleChatInput();
        HandleGameScoreboardInput();
    }

    private void SetupCanvas()
    {
        panelOptions.SetActive(false);
        panelScoreboard.SetActive(false);
        panelRespawn.SetActive(false);

        if (NetworkServer.active) { serverText.text = "Server: active. Transport: " + Transport.activeTransport; }
        if (NetworkClient.isConnected) { clientText.text = "Client: address=" + GameNetworkManager.singleton.networkAddress; }
    }

    private void ButtonStop()
    {
        // Stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            GameNetworkManager.singleton.StopHost();
        }
        // Stop client if client-only
        else if (NetworkClient.isConnected)
        {
            GameNetworkManager.singleton.StopClient();
        }
        // Stop server if server-only
        else if (NetworkServer.active)
        {
            GameNetworkManager.singleton.StopServer();
        }
    }

    private void HandlePauseResumeInput()
    {
        // Handle pause/resume input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            panelOptions.SetActive(paused);
            crosshair.SetActive(!paused && !panelRespawn.activeSelf);
            if (paused)
            {
                if (blockPlayerInput)
                {
                    // If we just paused and we had chat text box active then deactivate it.
                    chatMessage.GetComponent<InputField>().DeactivateInputField();
                    chatMessage.SetActive(false);
                }
                else
                {
                    // Block player input
                    blockPlayerInput = true;
                }
                // Unlock cursor and set it to be visible
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Additional information
                if (NetworkServer.active) { serverText.text = "Server: active. Transport: " + Transport.activeTransport; }
                if (NetworkClient.isConnected) { clientText.text = "Client: address=" + GameNetworkManager.singleton.networkAddress; }
            }
            else
            {
                blockPlayerInput = false;
                chatMessage.GetComponent<InputField>().DeactivateInputField();
                chatMessage.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleChatInput()
    {
        // Handle chat input
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (!paused)
            {
                // Unlock cursor and set it to be visible
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Block player input
                blockPlayerInput = true;
            }
            chatMessage.SetActive(true);
            chatMessage.GetComponent<InputField>().ActivateInputField();

        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!paused)
            {
                // Lock cursor and set it to be invisible
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Unlock player input
                blockPlayerInput = false;
            }
            chatMessage.GetComponent<InputField>().DeactivateInputField();
            chatMessage.SetActive(false);
            chatWindow.GetComponent<ChatWindow>().OnSend();
        }
    }

    private void HandleGameScoreboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))  // "Tab" key is being pressed
        {
            panelScoreboard.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))  // "Tab" key is released
        {
            panelScoreboard.SetActive(false);
        }
    }

    public void UpdateAmmoUI(int _value)
    {
        ammoText.text = "Ammo: " + _value;
    }

    public void UpdateHealthUI(int _value)
    {
        healthPointsText.text = _value.ToString();
    }

    public void UpdateScoreboardUI(List<GameNetworkManager.ScoreboardData> _scoreboardPlayerList)
    {
        // Called when scoreboard changed and it must be updated into UI
        for (int i = 0; i < GameNetworkManager.singleton.maxConnections; ++i)
        {
            if (i < _scoreboardPlayerList.Count)  // Set player information entry
            {
                Text _rank = scoreboardPlayerListUI[i].transform.GetChild((int)PlayerScoreboardItemField.Rank).GetComponent<Text>();
                _rank.text = (i + 1).ToString();

                Text _username = scoreboardPlayerListUI[i].transform.GetChild((int)PlayerScoreboardItemField.Username).GetComponent<Text>();
                _username.text = _scoreboardPlayerList[i].playerName;

                Text _kills = scoreboardPlayerListUI[i].transform.GetChild((int)PlayerScoreboardItemField.Kills).GetComponent<Text>();
                _kills.text = _scoreboardPlayerList[i].kills.ToString();

                Text _deaths = scoreboardPlayerListUI[i].transform.GetChild((int)PlayerScoreboardItemField.Deaths).GetComponent<Text>();
                _deaths.text = _scoreboardPlayerList[i].deaths.ToString();
            }
            else  // Delete entries
            {
                for (int j = 0; j < (int)PlayerScoreboardItemField.GetSize; ++j)
                {
                    Text t = scoreboardPlayerListUI[i].transform.GetChild(j).GetComponent<Text>();
                    t.text = string.Empty;
                }
            }
        }
    }

    public void DisplayRespawnPanel(string _killer)
    {
        crosshair.SetActive(false);
        panelRespawn.SetActive(true);
        if (_killer == null)
        {
            killedByText.text = "Suicided";
        }
        else
        {
            killedByText.text = $"Killed by {_killer}";
        }
    }

    public void UpdateRespawnSeconds(int _seconds)
    {
        respawnSecondsText.text = _seconds.ToString();
    }

    public void HideRespawnPanel()
    {
        panelRespawn.SetActive(false);
        crosshair.SetActive(true);
    }
}

