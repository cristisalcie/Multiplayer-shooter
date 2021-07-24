using Mirror;
using UnityEngine;

public class AnimationStateController : NetworkBehaviour
{
    private Animator animator;

    [SerializeField]
    private float acceleration;
    [SerializeField]
    private float deceleration;

    [SerializeField]
    [SyncVar]
    private float velocityX;
    [SerializeField]
    [SyncVar]
    private float velocityZ;
    [SerializeField]
    [SyncVar]
    private bool mustJump;
    [SerializeField]
    [SyncVar]
    private bool isShooting;
    private bool mustResetJump;
    [SerializeField]
    [SyncVar]
    private bool isGrounded;
    [SerializeField]
    [SyncVar]
    private float verticalAim;

    private int velocityXHash;
    private int velocityZHash;
    private int verticalAimHash;
    private int isGroundedHash;
    private int isShootingHash;
    private int mustJumpHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        acceleration = 5.0f;
        deceleration = 4.0f;
        velocityX = 0.0f;
        velocityZ = 0.0f;
        mustJump = false;
        mustResetJump = false;
        isShooting = false;
    }

    private void Start()
    {
        velocityXHash = Animator.StringToHash("velocityX");
        velocityZHash = Animator.StringToHash("velocityZ");
        verticalAimHash = Animator.StringToHash("verticalAim");
        isGroundedHash = Animator.StringToHash("isGrounded");
        isShootingHash = Animator.StringToHash("isShooting");
        mustJumpHash = Animator.StringToHash("mustJump");
    }

    private void Update()
    {
        if (hasAuthority)
        {
            Move();
            PerformJump();
            ShootWeapon();
        }
        else
        {
            // No authority over variables, however we should animate the character that has this script attached.
            animator.SetFloat(velocityXHash, velocityX);
            animator.SetFloat(velocityZHash, velocityZ);
            animator.SetBool(mustJumpHash, mustJump);
            animator.SetBool(isShootingHash, isShooting);
            animator.SetBool(isGroundedHash, isGrounded);
            animator.SetFloat(verticalAimHash, verticalAim);
        }
    }

    public void SetVerticalAim(float _verticalAim)
    {
        verticalAim = _verticalAim;
        animator.SetFloat(verticalAimHash, verticalAim);
        CmdSyncVerticalAim(_verticalAim);
    }

    public void SetIsGrounded(bool _isGrounded)
    {
        isGrounded = _isGrounded;
        animator.SetBool(isGroundedHash, isGrounded);
        CmdSyncIsGrounded(isGrounded);
    }

    public void SetMustJump(bool _mustJump)
    {
        mustJump = _mustJump;
    }

    private void Move()
    {
        MoveForwardBackward();
        MoveLeftRight();
        CmdSyncVelocity(velocityX, velocityZ);
    }

    private void MoveForwardBackward()
    {
        bool _forwardPressed = Input.GetKey(KeyCode.W);
        bool _backwardPressed = Input.GetKey(KeyCode.S);

        // Forward/Backward animation implementation
        if (_forwardPressed && _backwardPressed || !_forwardPressed && !_backwardPressed)  // Decelerate forward/backward movement
        {
            if (velocityZ < 0.0f)  // Decelerate from backward movement
            {
                velocityZ = Mathf.Clamp(velocityZ + Time.deltaTime * deceleration, -1.0f, 0.0f);
            }
            else if (velocityZ > 0.0f)  // Decelerate from forward movement
            {
                velocityZ = Mathf.Clamp(velocityZ - Time.deltaTime * deceleration, 0.0f, 1.0f);
            }
        }
        else if (_forwardPressed)  // Accelerate forward
        {
            velocityZ = Mathf.Clamp(velocityZ + Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        else if (_backwardPressed)  // Accelerate backward
        {
            velocityZ = Mathf.Clamp(velocityZ - Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        animator.SetFloat(velocityZHash, velocityZ);
    }

    private void MoveLeftRight()
    {
        bool _leftPressed = Input.GetKey(KeyCode.A);
        bool _rightPressed = Input.GetKey(KeyCode.D);

        // Left/Right animation implementation
        if (_leftPressed && _rightPressed || !_leftPressed && !_rightPressed)  // Decelerate left/right movement
        {
            if (velocityX < 0.0f)  // Decelerate from left movement
            {
                velocityX = Mathf.Clamp(velocityX + Time.deltaTime * deceleration, -1.0f, 0.0f);
            }
            else if (velocityX > 0.0f)  // Decelerate from right movement
            {
                velocityX = Mathf.Clamp(velocityX - Time.deltaTime * deceleration, 0.0f, 1.0f);
            }
        }
        else if (_leftPressed)  // Accelerate left
        {
            velocityX = Mathf.Clamp(velocityX - Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        else if (_rightPressed)  // Accelerate right
        {
            velocityX = Mathf.Clamp(velocityX + Time.deltaTime * acceleration, -1.0f, 1.0f);
        }
        animator.SetFloat(velocityXHash, velocityX);
    }

    private void PerformJump()
    {
        if (mustJump)
        {
            CmdSyncJump(true);
            animator.SetBool(mustJumpHash, true);
            mustJump = false;
            mustResetJump = true;
        }
        else if (mustResetJump)
        {
            CmdSyncJump(false);
            animator.SetBool(mustJumpHash, false);
            mustResetJump = false;
        }
    }

    private void ShootWeapon()
    {
        bool _isShooting = Input.GetKey(KeyCode.Mouse0);
        isShooting = _isShooting;
        CmdSyncShoot(isShooting);
        animator.SetBool(isShootingHash, isShooting);
    }

    private void TestShootingEvent()
    {
        Debug.Log("called TestShootingEvent in AnimationStateControllerTest");
    }

    #region Commands

    [Command]
    private void CmdSyncVelocity(float _velocityX, float _velocityZ)
    {
        velocityX = _velocityX;
        velocityZ = _velocityZ;
    }

    [Command]
    private void CmdSyncJump(bool _mustJump)
    {
        mustJump = _mustJump;
    }

    [Command]
    private void CmdSyncShoot(bool _isShooting)
    {
        isShooting = _isShooting;
    }

    [Command]
    private void CmdSyncVerticalAim(float _verticalAim)
    {
        verticalAim = _verticalAim;
    }

    [Command]
    private void CmdSyncIsGrounded(bool _isGrounded)
    {
        isGrounded = _isGrounded;
    }

    #endregion
}