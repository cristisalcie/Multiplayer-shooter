using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneScript : NetworkBehaviour
{
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
}
