using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class CanvasLobbyHUD : MonoBehaviour
{
	public Button buttonHost;
	public Button buttonServer;
	public Button buttonClient;

    public GameObject PanelStart;

	public InputField inputFieldAddress;

	public Text clientText;

    private void Start()
    {
        // Update the canvas text if you have manually changed network managers address from the game object before starting the game scene
        if (GameNetworkManager.singleton.networkAddress != "localhost") { inputFieldAddress.text = GameNetworkManager.singleton.networkAddress; }

        // Adds a listener to the main input field and invokes a method when the value changes.
        inputFieldAddress.onValueChanged.AddListener(delegate { ValueChangeCheck(); });

        // Make sure to attach these Buttons in the Inspector
        buttonHost.onClick.AddListener(ButtonHost);
        buttonServer.onClick.AddListener(ButtonServer);
        buttonClient.onClick.AddListener(ButtonClient);

        // This updates the Unity canvas, we have to manually call it every change, unlike legacy OnGUI.
        SetupCanvas();
    }

    // Invoked when the value of the text field changes.
    public void ValueChangeCheck()
    {
        GameNetworkManager.singleton.networkAddress = inputFieldAddress.text;
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
