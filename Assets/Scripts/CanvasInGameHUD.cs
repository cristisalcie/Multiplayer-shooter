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
	private GameObject PanelOptions;

	public GameObject PanelScoreboard;

    [SerializeField]
	private GameObject chatWindow;

	public GameObject chatMessage;

    [SerializeField]
	private Text serverText;

    [SerializeField]
	private Text clientText;

    [SerializeField]
    private Text ammoText;

    private GameObject[] scoreboardPlayerListUI;

    private ScoreboardListComparer scoreboardListComparer;

    private class ScoreboardListComparer : IComparer<GameNetworkManager.ScoreboardData>
    {
        public int Compare(GameNetworkManager.ScoreboardData x, GameNetworkManager.ScoreboardData y)
        {
            if (x.kills < y.kills)
            {
                return 1;
            }
            return 0;
        }
    }


    private void Awake()
    {
        paused = false;
        blockPlayerInput = false;
        
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

        scoreboardListComparer = new ScoreboardListComparer();
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
        PanelOptions.SetActive(false);
        PanelScoreboard.SetActive(false);

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

    public void UIAmmo(int _value)
    {
        ammoText.text = "Ammo: " + _value;
    }

    private void HandlePauseResumeInput()
    {
        // Handle pause/resume input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            PanelOptions.SetActive(paused);
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
            PanelScoreboard.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))  // "Tab" key is released
        {
            PanelScoreboard.SetActive(false);
        }
    }

    public void UpdateScoreboard(List<GameNetworkManager.ScoreboardData> _scoreboardPlayerList)
    {
        // Called when a player connects/disconnects/gets killed/ makes kill
        // TODO: Logic for calculating scoreboard.
        // TODO: Optimize this and comment it

        _scoreboardPlayerList.Sort(scoreboardListComparer); // maybe have it sorted on the server

        foreach (GameNetworkManager.ScoreboardData p in _scoreboardPlayerList)
        {
            Debug.Log($"[on client] {p.playerName} has {p.kills} kills");
        }

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
}

