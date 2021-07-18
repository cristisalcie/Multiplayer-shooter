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
    private float verticalAim;
    private bool isGrounded;
    private bool mustJump;

    private int isCrouchingHash;
    private int velocityXHash;
    private int velocityZHash;
    private int verticalAimHash;
    private int isGroundedHash;
    private int mustJumpHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        acceleration = 5.0f;
        deceleration = 4.0f;
        velocityX = 0.0f;
        velocityZ = 0.0f;
        verticalAim = 0.0f;
        isGrounded = true;
        mustJump = false;
    }

    private void Start()
    {
        velocityXHash = Animator.StringToHash("velocityX");
        velocityZHash = Animator.StringToHash("velocityZ");
        isCrouchingHash = Animator.StringToHash("isCrouching");
        verticalAimHash = Animator.StringToHash("verticalAim");
        isGroundedHash = Animator.StringToHash("isGrounded");
        mustJumpHash = Animator.StringToHash("mustJump");
    }

    private void Update()
    {
        bool _isCrouching = animator.GetBool(isCrouchingHash);
        bool _crouchPressed   = Input.GetKey(KeyCode.LeftControl);
        bool _forwardPressed  = Input.GetKey(KeyCode.W);
        bool _backwardPressed = Input.GetKey(KeyCode.S);
        bool _leftPressed     = Input.GetKey(KeyCode.A);
        bool _rightPressed    = Input.GetKey(KeyCode.D);
        bool _jumpPressed     = Input.GetKey(KeyCode.Space);


        // Crouch animation implementation
        if (_crouchPressed)
        {
            if (!_isCrouching)  // Was not in crouching state
            {
                animator.SetBool(isCrouchingHash, true);
            }
        }
        else  // crouch not pressed
        {
            if (_isCrouching)  // Was in crouching state
            {
                animator.SetBool(isCrouchingHash, false);
            }
        }



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
        animator.SetFloat(velocityZHash, velocityZ);
        animator.SetFloat(verticalAimHash, verticalAim);
        animator.SetBool(isGroundedHash, isGrounded);
        if (mustJump)
        {
            animator.SetBool(mustJumpHash, mustJump);
            mustJump = false;
        } else { animator.SetBool(mustJumpHash, false); }
    }

    public void SetVerticalAim(float _verticalAim)
    {
        verticalAim = _verticalAim;
    }

    public void SetIsGrounded(bool _isGrounded)
    {
        isGrounded = _isGrounded;
    }

    public void SetMustJump(bool _mustJump)
    {
        mustJump = _mustJump;
    }
}
