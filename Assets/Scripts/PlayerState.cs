using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    private CanvasInGameHUD canvasInGameHUD;
    private int healthPoints;
    private int maxHealthPoints;
    private bool isDead;  // This doesn't have to be sent through network. We can determine if is dead looking at healthPoints

    private void Awake()
    {
        maxHealthPoints = 1000;
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
    }

    public void SetupState()
    {
        healthPoints = maxHealthPoints;
        isDead = false;
        RpcSyncHealthPoints(healthPoints);
    }

    public int GetHealthPoints()
    {
        return healthPoints;
    }

    #region Commands

    [Command]
    public void CmdHit(GameObject _target, int _damage)
    {
        PlayerState _targetPlayerState = _target.GetComponent<PlayerState>();

        // Have server know the values of the variables as well so it can provide them for new connections directly
        if (_targetPlayerState.isDead) { return; }
        _targetPlayerState.healthPoints -= _damage;

        if (_targetPlayerState.healthPoints <= 0)
        {
            _targetPlayerState.healthPoints = 0;
            _targetPlayerState.isDead = true;
        }

        // Called from target's script so we can also update the canvas health points text by just checking if localPlayer
        _targetPlayerState.RpcSyncHealthPoints(_targetPlayerState.healthPoints);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    public void RpcSyncHealthPoints(int _healthPoints)
    {
        healthPoints = _healthPoints;
        if (healthPoints <= 0)
        {
            isDead = true;
        }
        if (isLocalPlayer)
        {
            canvasInGameHUD.UpdateHealthUI(healthPoints);
        }
    }

    #endregion

    #region TargetRpc

    /// <summary> Used on CmdSetupPlayer() in PlayerScript to gather existing modified state of all joined players </summary>
    /// <param name="_target"> The GameObject identity to be modified (will have the values of the local scene, not the ones sent from server) </param>
    /// <param name="_healthPoints"> The value of healthPoints that needs to be changed to in _target </param>
    [TargetRpc]
    public void TargetSetHealthPoints(GameObject _target, int _healthPoints)
    {
        PlayerState _targetPlayerState = _target.GetComponent<PlayerState>();
        _targetPlayerState.healthPoints = _healthPoints;
        if (_healthPoints <= 0)
        {
            _targetPlayerState.isDead = true;
        }  // Else is false (default)
    }

    #endregion
}
