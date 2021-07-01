using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInGameHUD : MonoBehaviour
{
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

    private GameObject[] scoreboardPlayerList;
    private const int playerScoreboardItemCount = 4;



    private void Awake()
    {
        paused = false;
        blockPlayerInput = false;
        
        scoreboardPlayerList = new GameObject[GameNetworkManager.singleton.maxConnections];

        GameObject _playerList = transform.Find("PanelScoreboard/PlayerList").gameObject;
        for (int i = 0; i < GameNetworkManager.singleton.maxConnections; ++i)
        {
            scoreboardPlayerList[i] = _playerList.transform.GetChild(i).gameObject;
            for (int j = 0; j < playerScoreboardItemCount; ++j)
            {
                Text t = scoreboardPlayerList[i].transform.GetChild(j).GetComponent<Text>();
                t.text = string.Empty;
            }
        }
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

    public void UpdateScoreboard()
    {
        // TODO: Logic for calculating scoreboard.

        // This way we can get all connections to server.
        foreach (System.Collections.Generic.KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
        {
            //Debug.Log($"key = {connection.Key} ; value = {connection.Value}");
            //Debug.Log($"{connection.Value.identity.gameObject.GetComponent<PlayerScript>().playerName}");
            //Debug.Log(this);
        }
    }
}
