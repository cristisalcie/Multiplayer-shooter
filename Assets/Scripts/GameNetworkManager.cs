using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class GameNetworkManager : NetworkManager
{
    private List<PlayerScript.ScoreboardData> scoreboardPlayerList;


    public override void Awake()
    {
        base.Awake();

        scoreboardPlayerList = new List<PlayerScript.ScoreboardData> { Capacity = maxConnections };
    }


    public override void OnServerDisconnect(NetworkConnection conn)
    {
        PlayerScript _dcPlayerScript = conn.identity.gameObject.GetComponent<PlayerScript>();
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
                _playerScript.RpcReceive($"{_dcPlayerScript.playerName} disconnected", false);

                RemoveFromScoreboard(_dcPlayerScript.scoreboardData);
                _playerScript.RpcUpdateScoreboard(scoreboardPlayerList);
            }
        } 

        Debug.Log($"{_dcPlayerScript.playerName} disconnected and numplayers is {numPlayers}");
        base.OnServerDisconnect(conn);
    }

    public void InsertIntoScoreboard(PlayerScript.ScoreboardData _scoreboardData)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _scoreboardData.playerName)
            {
                return;
            }
        }
        scoreboardPlayerList.Add(_scoreboardData);
    }

    public void RemoveFromScoreboard(PlayerScript.ScoreboardData _scoreboardData)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _scoreboardData.playerName)
            {
                scoreboardPlayerList.RemoveAt(i);
                break;
            }
        }
    }

    public List<PlayerScript.ScoreboardData> GetScoreboardPlayerList()
    {
        return scoreboardPlayerList;
    }

    public void UpdateScoreboardList(PlayerScript.ScoreboardData _scoreboardData)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _scoreboardData.playerName)
            {
                PlayerScript.ScoreboardData _sd = scoreboardPlayerList[i];
                _sd.kills = _scoreboardData.kills;
                _sd.deaths = _scoreboardData.deaths;
                scoreboardPlayerList[i] = _sd;
                break;
            }
        }
    }
}
