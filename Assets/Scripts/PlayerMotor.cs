using Mirror;
using UnityEngine;

public class PlayerMotor : NetworkBehaviour
{
    private Rigidbody rb;
    private CharacterController charCtrl;

    [SyncVar]
    private Vector3 velocity;

    [SyncVar]
    private Vector3 rotation;

    [SyncVar]
    private float cameraRotationX;

    private float currentCameraRotationX;
    private float rotationMultiplier;

    [SyncVar]
    private Vector3 jmp;

    private int groundLayerIndex;
    private bool isGrounded;
    private int numberJumps;

    private int maxJumps;

    [SerializeField]
    private GameObject weaponsHolder;

    [SerializeField]
    private float cameraRotationXLimit;

    [SerializeField]
    private float gravityConstant;

    private float gravity;
    private Vector3 inAirVelocity;  // Used to continue going into a direction while in air


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // Deactivate rigidbody
        charCtrl = GetComponent<CharacterController>();
        charCtrl.stepOffset = 0.1f;
        velocity = Vector3.zero;
        rotation = Vector3.zero;
        cameraRotationX = 0f;
        currentCameraRotationX = 0f;
        rotationMultiplier = 100f;
        jmp = Vector3.zero;

        groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex == -1) // Check if ground layer is valid
        {
            Debug.LogError("Ground layer does not exist");
        }

        isGrounded = false;
        numberJumps = 0;
        maxJumps = 1;
        cameraRotationXLimit = 70f;
        gravityConstant = 1f;
        gravity = 0f;
        inAirVelocity = Vector3.zero;
    }

    private void Update()
    {
        UpdateGrounded();
        PerformMovement();
        PerformRotation();
        PerformCameraRotation();
    }

    void FixedUpdate()
    {
        ApplyGravity();
        PerformJump();
    }

    /// <summary> This function is responsible for movement </summary>
    /// <param name="_velocity"> Frame dependent velocity (not multiplied by deltaTime) </param>
    public void Move(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    /// <summary> This function is responsible for player rotation </summary>
    /// <param name="_rotation"> Frame dependent rotation (not multiplied by deltaTime) </param>
    public void Rotate(Vector3 _rotation)
    {
        rotation = _rotation;
    }

    /// <summary> This function is responsible for camera rotation </summary>
    /// <param name="_cameraRotationX"> Frame dependent camera rotation (not multiplied by deltaTime) </param>
    public void RotateCamera(float _cameraRotationX)
    {
        cameraRotationX = _cameraRotationX;
    }

    /// <summary> This function is responsible for jumping </summary>
    public void Jump(int _maxJumps, Vector3 _jmp)
    {
        maxJumps = _maxJumps;
        if (numberJumps < maxJumps)
        {
            jmp = _jmp;
        }
    }

    /// <summary> Actual code for performing movement </summary>
    void PerformMovement()
    {
        if (isGrounded)
        {
            if (velocity != Vector3.zero)  // Has moved
            {
                charCtrl.Move(velocity * Time.deltaTime);
            }

            // Regardless if has moved or not set inAirVelocity
            inAirVelocity = velocity;
        }
        else  // Is not grounded
        {
            if (inAirVelocity != Vector3.zero)
            {
                if (charCtrl.collisionFlags == CollisionFlags.None)
                {  // If player is free floating (not colliding with anything)
                    charCtrl.Move(inAirVelocity * Time.deltaTime);
                }
            }
        }
    }

    /// <summary> Actual code for performing jump </summary>
    void PerformJump()
    {
        // This evaluates to true if we are still allowed to jump
        //            |
        //            v
        //  |--------------------|    |-----------------|
        if (numberJumps < maxJumps && jmp != Vector3.zero)
        {
            gravity = jmp.y * Time.deltaTime;
            charCtrl.Move(jmp * Time.deltaTime);
            ++numberJumps;
            jmp = Vector3.zero;  // Used to know that we "consumed" the jump so we don't keep jumping after we hit the ground
        }
    }
    
    /// <summary> Actual code for performing rotation of the player on the horizontal axis </summary>
    void PerformRotation()
    {
        if (rotation != Vector3.zero)
        {
            transform.rotation = (transform.rotation * Quaternion.Euler(rotation * Time.deltaTime * rotationMultiplier));
        }
    }

    /// <summary> Actual code for performing player camera rotation on the vertical axis </summary>
    void PerformCameraRotation()
    {
        if (cameraRotationX != 0 && isLocalPlayer)
        {
            // Set rotation and clamp it
            currentCameraRotationX -= cameraRotationX * Time.deltaTime * rotationMultiplier;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationXLimit, cameraRotationXLimit);
            Vector3 _newRotation = new Vector3(currentCameraRotationX, 0f, 0f);
            
            // Apply rotation to camera
            Camera.main.transform.localEulerAngles = _newRotation;
            // Move weapons (using network transform child with weaponsHolder as gameObject with client authority)
            weaponsHolder.transform.localEulerAngles = _newRotation;
            
        }
    }

    /// <summary> Updates isGrounded internal boolean and resets the allowed number of jumps </summary>
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
        if (isGrounded) { numberJumps = 0; }  // Set number of jumps back to 0 because we hit the ground
    }

    /// <summary> Applies gravity to player </summary>
    private void ApplyGravity()
    {
        if (isGrounded)
        {
            gravity = 0f;
        }
        else
        {
            gravity -= gravityConstant * Time.deltaTime;
            charCtrl.Move(Vector3.up * gravity);
        }
    }
}
