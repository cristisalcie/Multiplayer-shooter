using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerTest : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;

    private CharacterController charCtrl;
    private float gravity;

    [SerializeField]
    private float gravityConstant;

    [SerializeField]
    private float jumpConstant;

    private bool isGrounded;
    private bool mustJump;
    private int groundLayerIndex;
    private Vector3 inAirVelocity;

    private Vector3 cameraOffset;

    [SerializeField]
    private float lookSensitivityH;

    [SerializeField]
    private float lookSensitivityV;

    [SerializeField]
    private float cameraRotationXLimit;

    private float currentCameraRotationX = 0f;
    private float rotationMultiplier;
    private float xRot;
    private Vector3 rotation;

    private void Awake()
    {
        charCtrl = GetComponent<CharacterController>();
        moveSpeed = 6;
        gravity = 0f;
        gravityConstant = 1f;
        jumpConstant = 18f;
        isGrounded = true;
        mustJump = false;

        groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex == -1) // Check if ground layer is valid
        {
            Debug.LogError("Ground layer does not exist");
        }

        inAirVelocity = Vector3.zero;

        cameraOffset = new Vector3(0.0f, 0.4f, 0.0f);
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        lookSensitivityH = 5f;
        lookSensitivityV = 5f;
        cameraRotationXLimit = 70f;
        currentCameraRotationX = 0f;
        rotationMultiplier = 100f;
        xRot = 0f;
        rotation = Vector3.zero;
}

    // Update is called once per frame
    void Update()
    {
        UpdateGrounded();

        // Handle movement
        if (isGrounded)
        {
            // Handle Move input
            float _movX = Input.GetAxisRaw("Horizontal");
            float _movZ = Input.GetAxisRaw("Vertical");

            if (_movX != 0 || _movZ != 0) // Has moved
            {
                Vector3 movHorizontal = transform.right * _movX;
                Vector3 movVertical = transform.forward * _movZ;

                // Calculate velocity and apply movement
                Vector3 _velocity = (movHorizontal + movVertical).normalized * moveSpeed;
                inAirVelocity = _velocity;
                charCtrl.Move(_velocity * Time.deltaTime);
            }
            else /* Hasn't moved but is grounded */ { inAirVelocity = Vector3.zero; }
        }
        else  // Is not grounded
        {
            if (inAirVelocity != Vector3.zero)  // Has xOz velocity
            {
                if (charCtrl.collisionFlags == CollisionFlags.None)
                {  // If player is free floating (not colliding with anything)
                    charCtrl.Move(inAirVelocity * Time.deltaTime);
                }
            }
        }

        // Check if the player tries to jump
        if (Input.GetButtonDown("Jump") && isGrounded) { mustJump = true; }

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxisRaw("Mouse X");
        rotation = new Vector3(0f, _yRot, 0f) * lookSensitivityH;

        // Calculate camera rotation
        xRot = Input.GetAxisRaw("Mouse Y") * lookSensitivityV;
        PerformRotation();
    }

    private void FixedUpdate()
    {
        HandleGravityAndJump();
    }

    private void HandleGravityAndJump()
    {
        // Handle jump & gravity calculations here for consistency (Always jump the same height / Always have the gravity pull the same)
        if (mustJump)
        {
            mustJump = false;
            isGrounded = false;
            gravity = jumpConstant * Time.deltaTime;
        }
        else
        {
            gravity = isGrounded ? 0f : (gravity - gravityConstant * Time.deltaTime);
        }
        if (gravity != 0)
        {
            charCtrl.Move(Vector3.up * gravity);
        }
    }

    private void UpdateGrounded()
    {
        // Check if the player is grounded
        if (charCtrl.collisionFlags == CollisionFlags.Below)  // Touching the edge of any object (not completely on but on the edge)
        {  // If we are completely on an object the if block will return false
            isGrounded = true;
        }  // I need the above check because i figured it will return true only when collider is on the edge of an object
        else  // We are not touching the edge so check if we are completely on an object that is labeled as "Ground"
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, charCtrl.height / 2f + charCtrl.skinWidth, 1 << groundLayerIndex);
        }
    }

    private void PerformRotation()
    {
        if (rotation != Vector3.zero)
        {
            transform.rotation = (transform.rotation * Quaternion.Euler(rotation * Time.deltaTime * rotationMultiplier));
        }

        if (xRot != 0)
        {
            // Set rotation and clamp it
            currentCameraRotationX -= xRot * Time.fixedDeltaTime * rotationMultiplier;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationXLimit, cameraRotationXLimit);
            Vector3 _newRotation = new Vector3(currentCameraRotationX, 0f, 0f);

            // Apply rotation to camera
            Camera.main.transform.localEulerAngles = _newRotation;
        }
    }
}
