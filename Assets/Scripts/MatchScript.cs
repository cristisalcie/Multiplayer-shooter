using System.Collections;
using Mirror;
using UnityEngine;

public class MatchScript : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;
    private PlayerScript localPlayerScript;  // Only for clients since server has 1 instance and >= 1 players.

    public bool MatchStarted { get; private set; }
    public bool MatchFinished { get; private set; }
    public string MatchWinner { get; private set; }

    [Header("Match Settings")]
    public const uint matchCountdown = 5;
    private const uint matchStartedDisplayTime = 3;
    private const uint matchFinishedDisplayTime = 3;
    public const uint respawnDisplayTime = 3;
    public float currentStartCountdown;  // Initially zero
    public float currentFinishCountdown;  // Initially zero
    public bool preparingMatch;  // Only server uses this bool. Will be true if the display sequence hasn't finished
    public bool preparingFinish;  // Only server uses this bool. Will be true if the display sequence hasn't finished
    private Coroutine onClientUpdateCurrentStartCountdownReference;
    private Coroutine onServerUpdateCurrentStartCountdownReference;

    private void Awake()
    {
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
        currentStartCountdown = 0;
        currentFinishCountdown = 0;
        MatchStarted = false;  // todo: Uncomment this before finishing off project
        //MatchStarted = true;  // todo: Delete this before finishing off project
        MatchFinished = false;
        preparingMatch = false;
        preparingFinish = false;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServer)
        {
            ((GameNetworkManager)GameNetworkManager.singleton).OnServerSetMatchScript(this);
        }
    }

    public void SetLocalPlayerScript(PlayerScript _playerScript)
    {
        localPlayerScript = _playerScript;
    }

    public void OnServerWaitForPlayers(NetworkConnection _source)
    {
        TargetWaitForPlayers(_source);
    }

    public void OnServerPrepareToStart(NetworkConnection _source)
    {
        ((GameNetworkManager)GameNetworkManager.singleton).OnServerResetKDScoreboardPlayerList();
        if (preparingMatch)  // Client connected while countdown was on going
        {
            if (_source != null)
            {
                TargetPrepareToStart(_source, currentStartCountdown);
            }
        }
        else
        {
            preparingMatch = true;
            currentStartCountdown = matchCountdown + respawnDisplayTime + matchStartedDisplayTime;

            RpcPrepareToStart(currentStartCountdown);
            onServerUpdateCurrentStartCountdownReference = StartCoroutine(OnServerUpdateCurrentStartedCountdown());
        }
    }

    /// <param name="_source"> caller's Network Connection </param>
    /// <param name="_winner"> is null if preparing to finish true </param>
    public void OnServerMatchFinished(NetworkConnection _source, string _winner)
    {
        if (preparingFinish)  // Client connected while countdown was on going
        {
            if (preparingMatch)  // Premature end of the preparing match sequence
            {
                preparingMatch = false;
                MatchStarted = false;
                RpcInterruptMatchStart();
                StopCoroutine(onServerUpdateCurrentStartCountdownReference);
            }
            TargetMatchFinished(_source, currentFinishCountdown, MatchWinner);
        }
        else
        {
            if (preparingMatch)  // Premature end of the preparing match sequence
            {
                RpcInterruptMatchStart();
                StopCoroutine(onServerUpdateCurrentStartCountdownReference);
            }

            MatchWinner = _winner;
            preparingMatch = false;
            MatchStarted = false;
            preparingFinish = true;
            MatchFinished = true;
            currentStartCountdown = 0;
            currentFinishCountdown = matchFinishedDisplayTime;
            RpcMatchFinished(currentFinishCountdown, _winner);
            StartCoroutine(OnServerUpdateCurrentFinishCountdown(_source));
        }
    }

    private IEnumerator OnServerUpdateCurrentFinishCountdown(NetworkConnection _source)
    {

        // Server needs to know with precision in case a new client connects while this is going on
        while (currentFinishCountdown > 0)
        {
            currentFinishCountdown -= Time.deltaTime;
            yield return null;  // Wait a frame
        }

        MatchFinished = false;
        preparingFinish = false;
        currentStartCountdown = 0;
        currentFinishCountdown = 0;

        if (NetworkServer.connections.Count == 1)
        {
            TargetWaitForPlayers(_source);
        }
        else if (NetworkServer.connections.Count > 1)
        {
            OnServerPrepareToStart(_source);
        }
    }

    private IEnumerator OnServerUpdateCurrentStartedCountdown()
    {
        // Server needs to know with precision in case a new client connects while this is going on
        while (currentStartCountdown > matchStartedDisplayTime)
        {
            currentStartCountdown -= Time.deltaTime;
            yield return null;  // Wait a frame
        }

        MatchStarted = true;

        while (currentStartCountdown > 0)
        {
            currentStartCountdown -= Time.deltaTime;
            yield return null;  // Wait a frame
        }

        currentStartCountdown = 0;
        preparingMatch = false;
    }

    private IEnumerator OnClientUpdateCurrentStartCountdown()
    {
        canvasInGameHUD.HideWaitingForPlayersPanel();
        float _currentCountdown = currentStartCountdown;  // This way host is safe as well (From race condition problem)

        // Preparing to start panel logic
        if (_currentCountdown > respawnDisplayTime + matchStartedDisplayTime)
        {
            float _currentPreparingMatchCountdown = _currentCountdown - matchStartedDisplayTime - respawnDisplayTime;

            canvasInGameHUD.DisplayPreparingToStartPanel((uint)_currentPreparingMatchCountdown + 1);

            yield return new WaitForSeconds(_currentPreparingMatchCountdown - (uint)_currentPreparingMatchCountdown);

            for (uint i = 0; i < _currentPreparingMatchCountdown; ++i)
            {
                canvasInGameHUD.UpdatePreparingToStartPanel((uint)_currentPreparingMatchCountdown - i);
                yield return new WaitForSeconds(1.0f);
            }

            canvasInGameHUD.HidePreparingToStartPanel();
        }

        // Respawning panel logic
        if (_currentCountdown > respawnDisplayTime)
        {
            /* If a clients gets here just before the respawn time is over, it is his problem.
             * He will have to do the entire respawn sequence. He will not get damaged during this state
             * because he will have the hit boxes disabled and will be invisible to others. */
            canvasInGameHUD.DisplayStartingRespawningPanel();
            localPlayerScript.StartForceRespawn();

            yield return new WaitForSeconds(respawnDisplayTime);

            canvasInGameHUD.HideStartingRespawningPanel();
        }

        MatchStarted = true;
        canvasInGameHUD.DisplayMatchStartedPanel();

        yield return new WaitForSeconds(matchStartedDisplayTime);

        canvasInGameHUD.HideMatchStartedPanel();
    }

    private IEnumerator OnClientUpdateCurrentFinishCountdown()
    {
        float _currentCountdown = currentFinishCountdown;

        canvasInGameHUD.DisplayMatchWinnerPanel(MatchWinner);

        yield return new WaitForSeconds(_currentCountdown);

        canvasInGameHUD.HideMatchWinnerPanel();
        MatchFinished = false;
        currentStartCountdown = 0;
        currentFinishCountdown = 0;
    }

    #region ClientRpc

    [ClientRpc]
    private void RpcMatchFinished(float _currentCountdown, string _winner)
    {
        currentFinishCountdown = _currentCountdown;
        MatchWinner = _winner;
        MatchStarted = false;
        MatchFinished = true;
        StartCoroutine(OnClientUpdateCurrentFinishCountdown());
    }

    [ClientRpc]
    private void RpcPrepareToStart(float _currentCountdown)
    {
        localPlayerScript.scoreboard.ResetKD();
        currentStartCountdown = _currentCountdown;
        canvasInGameHUD.HideWaitingForPlayersPanel();
        canvasInGameHUD.DisplayPreparingToStartPanel((uint)currentStartCountdown);
        onClientUpdateCurrentStartCountdownReference = StartCoroutine(OnClientUpdateCurrentStartCountdown());
    }

    [ClientRpc]
    private void RpcInterruptMatchStart()
    {
        preparingMatch = false;
        MatchStarted = false;
        currentStartCountdown = 0;
        StopCoroutine(onClientUpdateCurrentStartCountdownReference);
        canvasInGameHUD.HidePreparingToStartPanel();
    }

    #endregion

    #region TargetRpc

    [TargetRpc]
    private void TargetWaitForPlayers(NetworkConnection _target)
    {
        canvasInGameHUD.DisplayWaitingForPlayersPanel();
    }

    [TargetRpc]
    private void TargetPrepareToStart(NetworkConnection _target, float _currentCountdown)
    {
        currentStartCountdown = _currentCountdown;
        onClientUpdateCurrentStartCountdownReference = StartCoroutine(OnClientUpdateCurrentStartCountdown());
    }

    [TargetRpc]
    public void TargetUpdateMatchStarted(NetworkConnection _target, bool _matchStarted)
    {
        MatchStarted = _matchStarted;
    }

    [TargetRpc]
    private void TargetMatchFinished(NetworkConnection _target, float _currentCountdown, string _winner)
    {
        currentFinishCountdown = _currentCountdown;
        preparingMatch = false;
        MatchStarted = false;
        MatchWinner = _winner;
        MatchFinished = true;
        StartCoroutine(OnClientUpdateCurrentFinishCountdown());
    }

    #endregion
}
