using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationStateControllerTest : MonoBehaviour
{
    private Animator animator;

    [SerializeField]
    private float acceleration;
    [SerializeField]
    private float deceleration;

    private float velocityX;
    private float velocityZ;
    private bool mustJump;
    private bool mustResetJump;

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
        MoveForwardBackward();
        MoveLeftRight();
        PerformJump();
        ShootWeapon();
    }

    public void SetVerticalAim(float _verticalAim)
    {
        animator.SetFloat(verticalAimHash, _verticalAim);
    }

    public void SetIsGrounded(bool _isGrounded)
    {
        animator.SetBool(isGroundedHash, _isGrounded);
    }

    public void SetMustJump(bool _mustJump)
    {
        mustJump = _mustJump;
    }

    private void MoveForwardBackward()
    {
        bool _forwardPressed  = Input.GetKey(KeyCode.W);
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
        bool _leftPressed  = Input.GetKey(KeyCode.A);
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
            animator.SetBool(mustJumpHash, true);
            mustJump = false;
            mustResetJump = true;
        }
        else if (mustResetJump)
        {
            animator.SetBool(mustJumpHash, false);
            mustResetJump = false;
        }
    }

    private void ShootWeapon()
    {
        animator.SetBool(isShootingHash, Input.GetKey(KeyCode.Mouse0));
    }

    private void TestShootingEvent()
    {
        Debug.Log("called TestShootingEvent in AnimationStateControllerTest");
    }
}
