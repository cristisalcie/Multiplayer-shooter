using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameNetworkManager : NetworkManager
{
    public struct ScoreboardData
    {
        public string playerName;
        public int kills;
        public int deaths;
    }

    private List<ScoreboardData> scoreboardPlayerList;


    public override void Awake()
    {
        base.Awake();

        scoreboardPlayerList = new List<ScoreboardData> { Capacity = maxConnections };
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
                //Debug.Log($"Found a player script that has the name {_playerScript.playerName}");
                break;
            }
            if (_playerScript != null)
            {
                _playerScript.RpcReceive($"{_dcPlayerScript.playerName} disconnected", false);

                RemoveFromScoreboard(_dcPlayerScript.playerName);
                _playerScript.RpcUpdateScoreboard(scoreboardPlayerList);
            }
        }

        //Debug.Log($"{_dcPlayerScript.playerName} disconnected and numplayers is {numPlayers}");
        base.OnServerDisconnect(conn);
    }

    // Awaiting optimization in the future to dictionary or different way of holding scoreboard
    #region Scoreboard operation functions

    public void InsertIntoScoreboard(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                return;
            }
        }
        scoreboardPlayerList.Add(new ScoreboardData { playerName = _playerName, kills = 0, deaths = 0 });
    }

    public void RemoveFromScoreboard(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                scoreboardPlayerList.RemoveAt(i);
                break;
            }
        }
    }

    public List<ScoreboardData> GetScoreboardPlayerList()
    {
        return scoreboardPlayerList;
    }

    public void IncrementScoreboardKillsOf(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                ScoreboardData _sd = scoreboardPlayerList[i];
                _sd.kills = scoreboardPlayerList[i].kills + 1;
                scoreboardPlayerList[i] = _sd;
                break;
            }
        }
    }

    public void IncrementScoreboardDeathsOf(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                ScoreboardData _sd = scoreboardPlayerList[i];
                _sd.deaths = scoreboardPlayerList[i].deaths + 1;
                scoreboardPlayerList[i] = _sd;
                break;
            }
        }
    }

    #endregion
}