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
    private GameObject panelWaitingForPlayers;

    [SerializeField]
    private GameObject panelPreparingToStart;

    [SerializeField]
    private GameObject panelMatchWinner;

    [SerializeField]
    private GameObject chatWindow;

    public GameObject chatMessage;

    public GameObject crosshair;

    [SerializeField]
    private GameObject hitVisualEffect;

    #region Mouse settings

    public float LookSensitivityH { get; private set; }
    public float LookSensitivityV { get; private set; }
    [SerializeField]
    private Slider sliderHorizontal;
    [SerializeField]
    private Slider sliderVertical;

    #endregion

    #region Display settings

    [SerializeField]
    private Dropdown screenModeDropdown;  // value = 0 => windowed else fullscreen
    [SerializeField]
    private Dropdown resolutionDropdown;

    #endregion

    #region Text variables/constants

    [SerializeField]
    private Text serverText;
    [SerializeField]
    private Text clientText;
    [SerializeField]
    private Text ammoText;
    [SerializeField]
    private Text healthPointsText;

    private Text killedByText;
    private Text respawnSecondsText;
    private Text prepareText;
    private Text preparingToStartSecondsText;
    private Text matchWinnerText;

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
            for (int j = 0; j < (int)PlayerScoreboardItemField.GetSize; ++j)
            {
                Text t = scoreboardPlayerListUI[i].transform.GetChild(j).GetComponent<Text>();
                t.text = string.Empty;
            }
        }

        #endregion

        #region Initialize preparing to respawn panel variables/constants

        killedByText = panelRespawn.transform.Find("KilledByText").GetComponent<Text>();
        respawnSecondsText = panelRespawn.transform.Find("SecondsText").GetComponent<Text>();

        #endregion

        #region Initialize preparing to start panel variables/constants

        prepareText = panelPreparingToStart.transform.Find("PrepareText").GetComponent<Text>();
        preparingToStartSecondsText = panelPreparingToStart.transform.Find("SecondsText").GetComponent<Text>();

        #endregion

        #region Mouse settings initialization

        LookSensitivityH = 5f;
        LookSensitivityV = 5f;
        sliderHorizontal.value = 5f;
        sliderVertical.value = 5f;
        sliderHorizontal.onValueChanged.AddListener(delegate { HorizontalSensitivityChanged(); });
        sliderVertical.onValueChanged.AddListener(delegate { VerticalSensitivityChanged(); });

        #endregion

        #region Display settings initialization


        screenModeDropdown.value = Screen.fullScreen ? 1 : 0;

        if (Screen.width == 640)
        {
            resolutionDropdown.value = 0;
        }
        else if (Screen.width == 1280)
        {
            resolutionDropdown.value = 1;
        }
        else if (Screen.width == 1600)
        {
            resolutionDropdown.value = 2;
        }
        else if (Screen.width == 1920)
        {
            resolutionDropdown.value = 3;
        }

        screenModeDropdown.onValueChanged.AddListener(delegate { DisplaySettingsChanged(); });
        resolutionDropdown.onValueChanged.AddListener(delegate { DisplaySettingsChanged(); });

        #endregion

        matchWinnerText = panelMatchWinner.transform.Find("MatchWinnerText").GetComponent<Text>();
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
        panelPreparingToStart.SetActive(false);
        chatWindow.SetActive(true);
        chatMessage.SetActive(false);
        crosshair.SetActive(true);

        if (NetworkServer.active) { serverText.text = "Server: active. Transport: " + Transport.activeTransport; }
        if (NetworkClient.isConnected) { clientText.text = "Client: address=" + GameNetworkManager.singleton.networkAddress; }
    }

    private void HorizontalSensitivityChanged()
    {
        LookSensitivityH = sliderHorizontal.value;
    }

    private void VerticalSensitivityChanged()
    {
        LookSensitivityV = sliderVertical.value;
    }

    private void DisplaySettingsChanged()
    {
        int _width = 1280;
        int _height = 720;

        if (resolutionDropdown.value == 0)
        {
            _width = 640;
            _height = 360;
        }
        else if (resolutionDropdown.value == 1)
        {
            _width = 1280;
            _height = 720;
        }
        else if (resolutionDropdown.value == 2)
        {
            _width = 1600;
            _height = 900;
        }
        else if (resolutionDropdown.value == 3)
        {
            _width = 1920;
            _height = 1080;
        }

        Screen.SetResolution(_width, _height, screenModeDropdown.value != 0);
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

    public void DisplayRespawnPanel(string _killer, uint _seconds)
    {
        crosshair.SetActive(false);
        respawnSecondsText.text = _seconds.ToString();
        if (_killer == null)
        {
            killedByText.text = "Suicided";
        }
        else
        {
            killedByText.text = $"Killed by {_killer}";
        }
        panelRespawn.SetActive(true);
    }

    public void UpdateRespawnSeconds(uint _seconds)
    {
        respawnSecondsText.text = _seconds.ToString();
    }

    public void HideRespawnPanel()
    {
        panelRespawn.SetActive(false);
        crosshair.SetActive(true);
    }

    public void DisplayWaitingForPlayersPanel()
    {
        panelWaitingForPlayers.SetActive(true);
    }

    public void HideWaitingForPlayersPanel()
    {
        panelWaitingForPlayers.SetActive(false);
    }

    public void DisplayPreparingToStartPanel(uint _seconds)
    {
        prepareText.text = "Preparing to start...";
        preparingToStartSecondsText.text = _seconds.ToString();
        panelPreparingToStart.SetActive(true);
    }

    public void UpdatePreparingToStartPanel(uint _seconds)
    {
        preparingToStartSecondsText.text = _seconds.ToString();
    }

    public void HidePreparingToStartPanel()
    {
        panelPreparingToStart.SetActive(false);
    }

    public void DisplayMatchStartedPanel()
    {
        // Reusing panelPreparingToStart
        preparingToStartSecondsText.text = null;
        prepareText.text = "Match started!";
        panelPreparingToStart.SetActive(true);
    }

    public void HideMatchStartedPanel()
    {
        // Reusing panelPreparingToStart
        panelPreparingToStart.SetActive(false);
    }

    public void DisplayStartingRespawningPanel()
    {
        // Reusing panelPreparingToStart
        preparingToStartSecondsText.text = null;
        prepareText.text = "Respawning...";
        panelPreparingToStart.SetActive(true);
    }

    public void HideStartingRespawningPanel()
    {
        // Reusing panelPreparingToStart
        panelPreparingToStart.SetActive(false);
    }

    public void DisplayMatchWinnerPanel(string _winner)
    {
        matchWinnerText.text = $"Match won by {_winner}";
        panelMatchWinner.SetActive(true);
    }

    public void HideMatchWinnerPanel()
    {
        panelMatchWinner.SetActive(false);
    }

    public void InstantiateVisualHitNotification()
    {
        GameObject _hitEffect = Instantiate(hitVisualEffect, transform);
        Destroy(_hitEffect, 0.5f);
    }
}
