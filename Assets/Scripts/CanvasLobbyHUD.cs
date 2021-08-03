using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasLobbyHUD : MonoBehaviour
{
    [Header("Buttons")]
    public Button buttonHost;
	public Button buttonServer;
	public Button buttonClient;
	public Button buttonMenu;

    [Header("Panels")]
    public GameObject PanelStart;

    [Header("InputFields")]
	public InputField inputFieldAddress;
    public InputField inputFieldPlayerName;

	public Text clientText;

    private void Start()
    {
        // Update the canvas text if you have manually changed network manager's address from the game object before starting the game scene
        if (GameNetworkManager.singleton.networkAddress != "localhost") { inputFieldAddress.text = GameNetworkManager.singleton.networkAddress; }

        // Adds a listener to the main input field and invokes a method when the value changes.
        inputFieldAddress.onValueChanged.AddListener(delegate { AddressChangeCheck(); });
        inputFieldPlayerName.onValueChanged.AddListener(delegate { PlayerNameChangeCheck(); });

        // Make sure to attach these Buttons in the Inspector
        buttonHost.onClick.AddListener(ButtonHost);
        buttonServer.onClick.AddListener(ButtonServer);
        buttonClient.onClick.AddListener(ButtonClient);
        buttonMenu.onClick.AddListener(ButtonMenu);

        // This updates the Unity canvas, we have to manually call it every change, unlike legacy OnGUI.
        SetupCanvas();
    }

    // Invoked when the value of the text field changes.
    public void AddressChangeCheck()
    {
        GameNetworkManager.singleton.networkAddress = inputFieldAddress.text;
    }

    // Invoked when the value of the text field changes.
    public void PlayerNameChangeCheck()
    {
        ((GameNetworkManager)GameNetworkManager.singleton).playerName = inputFieldPlayerName.text;
    }
    
    public void ButtonHost()
    {
        GameNetworkManager.singleton.StartHost();
        SetupCanvas();
    }

    public void ButtonServer()
    {
        GameNetworkManager.singleton.StartServer();
        SetupCanvas();
    }

    public void ButtonClient()
    {
        GameNetworkManager.singleton.StartClient();
        SetupCanvas();
    }
    
    public void ButtonMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void SetupCanvas()
    {
        // Here we will dump majority of the canvas UI that may be changed.
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (NetworkClient.active)
            {
                PanelStart.SetActive(false);
                clientText.text = "Connecting to " + GameNetworkManager.singleton.networkAddress + "..";
            }
            else
            {
                PanelStart.SetActive(true);
            }
        }
    }
}
