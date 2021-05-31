using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindow : MonoBehaviour
{
    public InputField chatMessage;
    public Text chatHistory;
    public Scrollbar scrollbar;

    public void Awake()
    {
        PlayerScript.OnMessage += OnPlayerMessage;
    }

    void OnPlayerMessage(PlayerScript player, string message, bool isPlayerMsg)
    {
        if (isPlayerMsg)
        {
            string prettyMessage = player.isLocalPlayer ?
                $"<color=blue>{player.playerName}: </color> {message}" :
                $"<color=red>{player.playerName}: </color> {message}";
            AppendMessage(prettyMessage);
        }
        else
        {
            string prettyMessage = $"<color=yellow>{message}</color>";
            AppendMessage(prettyMessage);
        }
        Debug.Log(message);
    }

    public void OnSend()
    {
        if (chatMessage.text.Trim() == "")
            return;

        // Get our player
        PlayerScript player = NetworkClient.connection.identity.GetComponent<PlayerScript>();

        // Send a message
        player.CmdSendPlayerMessage(chatMessage.text.Trim());

        chatMessage.text = "";
    }

    internal void AppendMessage(string message)
    {
        StartCoroutine(AppendAndScroll(message));
    }

    IEnumerator AppendAndScroll(string message)
    {
        chatHistory.text += message + "\n";

        // It takes 2 frames for the UI to update ?!?!
        yield return null;
        yield return null;

        // Slam the scrollbar down
        scrollbar.value = 0;
    }

    private void OnDestroy()
    {
        PlayerScript.OnMessage -= OnPlayerMessage;
    }
}
