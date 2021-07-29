using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    private AnimationStateController animationController;
    private CanvasInGameHUD canvasInGameHUD;
    
    private const float respawnPlayerTime = 5.0f;
    public int HealthPoints { get; private set; }  // Server authoritive!
    private int maxHealthPoints;
    public bool IsDead { get; private set; }  // This doesn't have to be sent through network. We can determine if is dead looking at healthPoints
    private Transform spawnPoints;

    private void Awake()
    {
        spawnPoints = GameObject.Find("SpawnPoints").transform;
        animationController = GetComponent<AnimationStateController>();
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();
        maxHealthPoints = 1000;  // Only accesed from Server at the moment
    }

    /// <summary> Runs on the server </summary>
    public void SetupState()
    {
        HealthPoints = maxHealthPoints;
        IsDead = false;
        RpcSyncHealthPoints(HealthPoints);
    }

    #region Coroutines

    /// <summary> Should run on all clients on script attached to the player that requires respawn </summary>
    IEnumerator RespawnPlayer()
    {
        if (isLocalPlayer)
        {
            CharacterController charCtrl = GetComponent<CharacterController>();
            Camera.main.transform.SetParent(null);  // Give camera to scene

            /* Since we are interpolating the position from NetworkTransform we disable Character Controller
            to make sure we will not interpolate quickly to spawnpoint and then back at death location. */ 
            charCtrl.enabled = false;

            // Wait a respawn time
            yield return new WaitForSeconds(respawnPlayerTime - 1);

            // Disable visual of player before teleporting
            transform.Find("Robot2").gameObject.SetActive(false);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(false);    // This gameobject has the weapon
        
            transform.position = spawnPoints.GetChild(Random.Range(0, spawnPoints.childCount - 1)).position;  // Should be synced by NetworkTransform

            yield return new WaitForSeconds(1f);

            transform.Find("Robot2").gameObject.SetActive(true);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(true);    // This gameobject has the weapon
            charCtrl.enabled = true;
            charCtrl.Move(Vector3.zero);  // Update collision flags because they are calculated every time Move(...) is called
            Camera.main.transform.SetParent(transform);  // Give camera back to our player

            // Sync state to all clients, this way we keep the server authority for HealthPoints and IsDead variables
            CmdRespawnPlayer();
        }
        else
        {
            // Wait a respawn time
            yield return new WaitForSeconds(respawnPlayerTime - 1);
            transform.Find("Robot2").gameObject.SetActive(false);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(false);    // This gameobject has the weapon
            transform.Find("NameTag").gameObject.SetActive(false); // This gameobject has the NameTag

            yield return new WaitForSeconds(1f);
            transform.Find("Robot2").gameObject.SetActive(true);  // This gameobject has the visual robot mesh
            transform.Find("Root").gameObject.SetActive(true);    // This gameobject has the weapon
            transform.Find("NameTag").gameObject.SetActive(true); // This gameobject has the NameTag
        }
    }

    #endregion

    #region Commands

    [Command]
    public void CmdRespawnPlayer()
    {
        // Apparently if a player is the host he will run Command and ClientRpc tags.
        HealthPoints = maxHealthPoints;
        IsDead = false;
        animationController.SetIsDead(false);  // Which is why we do this on the server as well.
        RpcSyncHealthPoints(HealthPoints);
    }

    [Command]
    public void CmdHit(GameObject _target, int _damage)
    {
        PlayerState _targetPlayerState = _target.GetComponent<PlayerState>();

        // Have server know the values of the variables as well so it can provide them for new connections directly
        if (_targetPlayerState.IsDead) { return; }
        _targetPlayerState.HealthPoints -= _damage;

        if (_targetPlayerState.HealthPoints <= 0)
        {
            _targetPlayerState.HealthPoints = 0;
            _targetPlayerState.IsDead = true;
        }

        // Called from target's script so we can also update the canvas health points text by just checking if localPlayer
        _targetPlayerState.RpcSyncHealthPoints(_targetPlayerState.HealthPoints);
    }

    #endregion

    #region ClientRpc

    /// <summary> Function runs on all clients on the PlayerState script of the object that got hit </summary>
    /// <param name="_healthPoints"> New value of healthPoints class member </param>
    [ClientRpc]
    public void RpcSyncHealthPoints(int _healthPoints)
    {
        HealthPoints = _healthPoints;
        if (HealthPoints <= 0)
        {
            IsDead = true;
            animationController.SetIsDead(true);  // This runs on all clients on the hit object's animation state controller
            StartCoroutine(RespawnPlayer());
        }
        else  // Has HealthPoints left
        {
            if (IsDead)  // Was dead but now we are not anymore
            {
                /* On the host we will never get inside this if because the server and client are merged into one.
                 * Hence they are already set in [Command] function CmdRespawnPlayer() */
                IsDead = false;
                animationController.SetIsDead(false);  // This runs on all clients on the hit object's animation state controller
            }
        }

        if (isLocalPlayer)
        {
            canvasInGameHUD.UpdateHealthUI(HealthPoints);
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
        _targetPlayerState.HealthPoints = _healthPoints;
        if (_healthPoints <= 0)
        {
            _targetPlayerState.IsDead = true;
            _target.GetComponent<AnimationStateController>().SetIsDead(true);  // Set player state to dead
        }  // Else is false (default)
    }

    #endregion
}
