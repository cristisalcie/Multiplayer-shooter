using Mirror;
using UnityEngine;

public class PlayerAnimationStateController : NetworkBehaviour
{
    private Animator playerAnimator;
    private CanvasInGameHUD canvasInGameHUD;

    private float acceleration;
    private float deceleration;

    private float velocityX;
    private float velocityZ;
    private float verticalAim;
    private float moveSpeed;
    private bool isShooting;
    private bool isGrounded;
    private bool isDead;

    private int velocityXHash;
    private int velocityZHash;
    private int verticalAimHash;
    private int moveSpeedHash;
    private int isGroundedHash;
    private int isShootingHash;
    private int isDeadHash;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        canvasInGameHUD = GameObject.Find("Canvas").GetComponent<CanvasInGameHUD>();

        acceleration = 5.0f;
        deceleration = 4.0f;
        velocityX = 0.0f;
        velocityZ = 0.0f;
        verticalAim = 0.0f;
        moveSpeed = 1.0f;
        isShooting = false;
        isGrounded = false;
        isDead = false;

        velocityXHash = Animator.StringToHash("velocityX");
        velocityZHash = Animator.StringToHash("velocityZ");
        verticalAimHash = Animator.StringToHash("verticalAim");
        moveSpeedHash = Animator.StringToHash("moveSpeed");
        isGroundedHash = Animator.StringToHash("isGrounded");
        isShootingHash = Animator.StringToHash("isShooting");
        isDeadHash = Animator.StringToHash("isDead");
    }

    private void Update()
    {
        if (hasAuthority)
        {
            // The following functions manage the animator as well
            MoveForwardBackward();
            MoveLeftRight();
        }
        else
        {
            // No authority over variables, however we should animate the character that has this script attached.
            playerAnimator.SetFloat(velocityXHash, velocityX);
            playerAnimator.SetFloat(velocityZHash, velocityZ);
            playerAnimator.SetFloat(verticalAimHash, verticalAim);
            playerAnimator.SetBool(isShootingHash, isShooting);
            playerAnimator.SetBool(isGroundedHash, isGrounded);
            playerAnimator.SetBool(isDeadHash, isDead);
        }
    }

    #region Extern set variables

    public void SetVerticalAim(float _verticalAim)
    {
        if (_verticalAim != verticalAim)  // Value changed
        {
            // Make change
            verticalAim = _verticalAim;
            // Set in animator
            playerAnimator.SetFloat(verticalAimHash, verticalAim);
            // Sync to all clients
            CmdSyncVerticalAim(_verticalAim);
        }
        // Else do nothing and save bandwidth
    }

    public void SetIsGrounded(bool _isGrounded)
    {
        if (_isGrounded != isGrounded)  // Value changed
        {
            // Make change
            isGrounded = _isGrounded;
            // Set in animator
            playerAnimator.SetBool(isGroundedHash, isGrounded);
        }

        /* Not syncing to all clients in the above if case because of the following scenario:
        If a player that is not moving is already online when another one first joins isGrounded
        would have been the default false value even if it is supposed to be true.
        Maybe it could be optimized in the future. */
        CmdSyncIsGrounded(isGrounded);
    }

    /// <summary> Already running in a ClientRpc in PlayerState, hence synchronization already done </summary>
    /// <param name="_isDead"> New value </param>
    public void SetIsDead(bool _isDead)
    {
        if (_isDead != isDead)  // Value changed
        {
            // Make change
            isDead = _isDead;
            // Set in animator
            playerAnimator.SetBool(isDeadHash, isDead);
        }
    }

    /// <summary> Already running in a ClientRpc in PlayerShoot, hence synchronization already done </summary>
    /// <param name="_moveSpeed"> New value </param>
    public void SetMoveSpeed(float _moveSpeed)
    {
        if (_moveSpeed != moveSpeed)
        {
            // Make change
            moveSpeed = _moveSpeed;
            // Set in animator
            playerAnimator.SetFloat(moveSpeedHash, moveSpeed);
        }
    }

    public void ShootWeapon(bool _isShooting)
    {
        if (_isShooting != isShooting)  // Value changed
        {
            // Make change
            isShooting = _isShooting;
            // Set in animators
            playerAnimator.SetBool(isShootingHash, isShooting);
        }

        /* Not syncing to all clients in the above if case because of the following scenario:
        If a player that is not moving is already online when another one first joins isShooting
        would have been the default false value even if it is supposed to be true.
        Maybe it could be optimized in the future. */
        CmdSyncShoot(isShooting);
    }

    #endregion

    private void MoveForwardBackward()
    {
        bool _forwardPressed;
        bool _backwardPressed;

        if (canvasInGameHUD.blockPlayerInput)
        {
            _forwardPressed = false;
            _backwardPressed = false;
        }
        else
        {
            _forwardPressed = Input.GetKey(KeyCode.W);
            _backwardPressed = Input.GetKey(KeyCode.S);
        }
        float _velocityZ = velocityZ;

        // Forward/Backward animation implementation
        if (_forwardPressed && _backwardPressed || !_forwardPressed && !_backwardPressed)  // Decelerate forward/backward movement
        {
            if (_velocityZ < 0.0f)  // Decelerate from backward movement
            {
                _velocityZ = Mathf.Clamp(_velocityZ + Time.deltaTime * deceleration, -1.0f, 0.0f);
            }
            else if (_velocityZ > 0.0f)  // Decelerate from forward movement
            {
                _velocityZ = Mathf.Clamp(_velocityZ - Time.deltaTime * deceleration, 0.0f, 1.0f);
            }
        }
        else if (_forwardPressed)  // Accelerate forward
        {
            _velocityZ = Mathf.Clamp(_velocityZ + Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        else if (_backwardPressed)  // Accelerate backward
        {
            _velocityZ = Mathf.Clamp(_velocityZ - Time.deltaTime * acceleration, -1.0f, 1.0f);
        }

        if (_velocityZ != velocityZ)  // Value changed
        {
            // Make the change
            velocityZ = _velocityZ;
            // Set in animator
            playerAnimator.SetFloat(velocityZHash, velocityZ);
            // Sync to all clients
            CmdSyncVelocityZ(velocityZ);
        }
        // Else do nothing and save bandwidth
    }

    private void MoveLeftRight()
    {
        bool _leftPressed;
        bool _rightPressed;

        if (canvasInGameHUD.blockPlayerInput)
        {
            _leftPressed = false;
            _rightPressed = false;
        }
        else
        {
            _leftPressed = Input.GetKey(KeyCode.A);
            _rightPressed = Input.GetKey(KeyCode.D);
        }
        float _velocityX = velocityX;

        // Left/Right animation implementation
        if (_leftPressed && _rightPressed || !_leftPressed && !_rightPressed)  // Decelerate left/right movement
        {
            if (_velocityX < 0.0f)  // Decelerate from left movement
            {
                _velocityX = Mathf.Clamp(_velocityX + Time.deltaTime * deceleration, -1.0f, 0.0f);
            }
            else if (_velocityX > 0.0f)  // Decelerate from right movement
            {
                _velocityX = Mathf.Clamp(_velocityX - Time.deltaTime * deceleration, 0.0f, 1.0f);
            }
        }
        else if (_leftPressed)  // Accelerate left
        {
            _velocityX = Mathf.Clamp(_velocityX - Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        else if (_rightPressed)  // Accelerate right
        {
            _velocityX = Mathf.Clamp(_velocityX + Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        
        if (_velocityX != velocityX)  // Value changed
        {
            // Make the change
            velocityX = _velocityX;
            // Set in animator
            playerAnimator.SetFloat(velocityXHash, velocityX);
            // Sync to all clients
            CmdSyncVelocityX(velocityX);
        }
        // Else do nothing and save bandwidth
    }

    #region Commands

    [Command]
    private void CmdSyncVelocityX(float _velocityX)
    {
        RpcSyncVelocityX(_velocityX);
    }

    [Command]
    private void CmdSyncVelocityZ(float _velocityZ)
    {
        RpcSyncVelocityZ(_velocityZ);
    }

    [Command]
    private void CmdSyncShoot(bool _isShooting)
    {
        RpcSyncShoot(_isShooting);
    }

    [Command]
    private void CmdSyncVerticalAim(float _verticalAim)
    {
        RpcSyncVerticalAim(_verticalAim);
    }

    [Command]
    private void CmdSyncIsGrounded(bool _isGrounded)
    {
        RpcSyncIsGrounded(_isGrounded);
    }

    #endregion

    #region ClientRpc

    [ClientRpc(includeOwner = false)]
    private void RpcSyncVerticalAim(float _verticalAim)
    {
        verticalAim = _verticalAim;
    }

    [ClientRpc(includeOwner = false)]
    private void RpcSyncVelocityX(float _velocityX)
    {
        velocityX = _velocityX;
    }

    [ClientRpc(includeOwner = false)]
    private void RpcSyncVelocityZ(float _velocityZ)
    {
        velocityZ = _velocityZ;
    }

    [ClientRpc(includeOwner = false)]
    private void RpcSyncShoot(bool _isShooting)
    {
        isShooting = _isShooting;
    }

    [ClientRpc(includeOwner = false)]
    private void RpcSyncIsGrounded(bool _isGrounded)
    {
        isGrounded = _isGrounded;
    }

    #endregion
}