using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreboardScript : MonoBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;

    private List<GameNetworkManager.ScoreboardData> scoreboardPlayerList;

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
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
        scoreboardListComparer = new ScoreboardListComparer();
    }

    public void SaveScoreboardOnClient(List<GameNetworkManager.ScoreboardData> _scoreboardPlayerList)
    {
        scoreboardPlayerList = _scoreboardPlayerList;
        scoreboardPlayerList.Sort(scoreboardListComparer);
        canvasInGameHUD.UpdateScoreboardUI(scoreboardPlayerList);
    }

    public void Append(string _playerName)
    {
        scoreboardPlayerList.Add(new GameNetworkManager.ScoreboardData { playerName = _playerName, kills = 0, deaths = 0 });
        canvasInGameHUD.UpdateScoreboardUI(scoreboardPlayerList);
    }

    public void Remove(string _name)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _name)
            {
                scoreboardPlayerList.RemoveAt(i);
                break;
            }
        }
        canvasInGameHUD.UpdateScoreboardUI(scoreboardPlayerList);
    }

    public void IncrementKillsOf(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                GameNetworkManager.ScoreboardData _sd = scoreboardPlayerList[i];
                _sd.kills = scoreboardPlayerList[i].kills + 1;
                scoreboardPlayerList[i] = _sd;
                break;
            }
        }
        scoreboardPlayerList.Sort(scoreboardListComparer);
        canvasInGameHUD.UpdateScoreboardUI(scoreboardPlayerList);
    }

    public void IncrementDeathsOf(string _playerName)
    {
        for (int i = 0; i < scoreboardPlayerList.Count; ++i)
        {
            if (scoreboardPlayerList[i].playerName == _playerName)
            {
                GameNetworkManager.ScoreboardData _sd = scoreboardPlayerList[i];
                _sd.deaths = scoreboardPlayerList[i].deaths + 1;
                scoreboardPlayerList[i] = _sd;
                break;
            }
        }
        scoreboardPlayerList.Sort(scoreboardListComparer);
        canvasInGameHUD.UpdateScoreboardUI(scoreboardPlayerList);
    }

}
