using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class GameNetworkManager : NetworkManager
{
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (numPlayers > 0)
        {
            PlayerScript _playerScript = null;
            /* Since the "to be disconnected connection" is not in the dictionary anymore and we have at least 1 connection
             * we access it for sending ClientRPC calls to make sure the calls get to every connection. */
            foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
            {
                _playerScript = connection.Value.identity.gameObject.GetComponent<PlayerScript>();
                Debug.Log($"Found a player script that has the name {_playerScript.playerName}");
                break;
            }
            if (_playerScript != null)
            {
                _playerScript.RpcReceive($"{conn.identity.gameObject.GetComponent<PlayerScript>().playerName} disconnected", false);
                _playerScript.RpcUpdateScoreboard();
            }
        } 

        Debug.Log($"{conn.identity.gameObject.GetComponent<PlayerScript>().playerName} disconnected and numplayers is {numPlayers}");
        base.OnServerDisconnect(conn);
    }
}
