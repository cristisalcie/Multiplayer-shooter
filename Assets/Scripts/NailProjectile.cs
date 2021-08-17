using System;
using UnityEngine;

public class NailProjectile : MonoBehaviour
{
    private GameObject owner;
    private PlayerState ownerState;
    private Rigidbody rb;

    private float movementMagnitude;  // How much projectile moves in between fixed frames
    private Vector3 direction;
    private float life;  // Received from weapon script

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (owner == null) { return; }
        DontGoThroughThings();
    }

    public void Setup(GameObject _owner, float _projectileLife, float _projectileSpeed, Vector3 _projectileDirection)
    {
        owner = _owner;
        ownerState = _owner.GetComponent<PlayerState>();
        life = _projectileLife;
        direction = _projectileDirection;
        movementMagnitude = Time.fixedDeltaTime * _projectileSpeed;
        rb.velocity = _projectileDirection * _projectileSpeed;
    }

    /// <summary> Collision checking function that triggers OnColliderHit that handles what happens on hit </summary>
    private void DontGoThroughThings()
    {
        // Check for obstructions we might miss next fixed frame 
        if (Physics.Raycast(rb.position, direction, out RaycastHit _hitInfo, movementMagnitude))
        {
            if (!_hitInfo.collider)
            {
                return;
            }

            // Stop projectile
            rb.velocity = Vector3.zero;

            // Fix position
            rb.position = _hitInfo.point;

            // Call collision function
            OnColliderHit(_hitInfo.collider);
        }
    }

    /// <summary>
    /// Called on every single connected client
    /// </summary>
    private void OnColliderHit(Collider _other)
    {
        bool _hitPlayer = _other.CompareTag("Player");
        if (ownerState.hasAuthority && _hitPlayer)  // Only executes on the player that shot the projectile
        {
            int _damage = 0;

            if (_other.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log("hit player head inside if");
                _damage = 340;
            }
            else if (_other.name.IndexOf("ribs", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log("hit player ribs inside if");
                _damage = 170;
            }
            else if (_other.name.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log("hit player hips inside if");
                _damage = 120;
            }
            else if (_other.name.IndexOf("thigh", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 100;
                Debug.Log("hit player thigh inside if");
            }
            else if (_other.name.IndexOf("knee", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 80;
                Debug.Log("hit player knee inside if");
            }
            else if (_other.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 50;
                Debug.Log("hit player toe inside if");
            }
            else if (_other.name.IndexOf("forearm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 80;
                Debug.Log("hit player forearm inside if");
            }
            else if (_other.name.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 100;
                Debug.Log("hit player arm inside if");
            }
            else if (_other.name.IndexOf("wrist", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _damage = 50;
                Debug.Log("hit player wrist inside if");
            }

            // Get root of collider (parent)
            GameObject _parent = _other.transform.root.gameObject;
            // Hit target
            ownerState.CmdHit(_parent, _damage);
        }
        if (_hitPlayer)
        {
            GetComponent<MeshRenderer>().enabled = false;  // Make projectile invisible
        }

        // Set destroy time
        Destroy(gameObject, life);

        // Disable script
        enabled = false;
    }
}
