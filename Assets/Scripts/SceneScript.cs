using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneScript : NetworkBehaviour
{
    public SceneReference sceneReference;

    public Text canvasStatusText;
    public PlayerScript playerScript;
    public Text canvasAmmoText;

    public void ButtonChangeScene()
    {
        if (isServer)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name == "Map1") { NetworkManager.singleton.ServerChangeScene("Map2"); }

            else { NetworkManager.singleton.ServerChangeScene("Map1"); }
        }
        else { Debug.Log("You are not Host."); }
    }

    public void UIAmmo(int _value)
    {
        canvasAmmoText.text = "Ammo: " + _value;
    }
}
