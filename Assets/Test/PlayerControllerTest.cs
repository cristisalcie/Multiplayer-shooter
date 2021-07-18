using UnityEngine;

public class PlayerControllerTest : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;

    private AnimationStateControllerTest animationController;
    private CharacterController charCtrl;
    private float gravity;

    [SerializeField]
    private float gravityConstant;

    [SerializeField]
    private float jumpHeight;

    public bool isGrounded;
    private Vector3 jmp;
    private int numberJumps;
    private int maxJumps;
    private int walkableLayerIndex;
    private Vector3 inAirVelocity;

    private Vector3 cameraOffset;

    [SerializeField]
    private float lookSensitivityH;

    [SerializeField]
    private float lookSensitivityV;

    [SerializeField]
    private float cameraRotationXLimit;

    private float currentCameraRotationX;
    private float rotationMultiplier;
    private float xRot;
    private Vector3 rotation;

    private bool gamePaused;

    private const float runMoveSpeed = 6f;
    //private const float crouchMoveSpeed = 3f;
    private const float crouchMoveSpeed = 1f;

    [SerializeField]
    private Transform weaponLocation;



    private void Awake()
    {
        animationController = GetComponent<AnimationStateControllerTest>();
        charCtrl = GetComponent<CharacterController>();
        charCtrl.stepOffset = 0.1f;
        charCtrl.skinWidth = 0.03f;
        charCtrl.minMoveDistance = 0.001f;
        charCtrl.radius = 0.3f;
        charCtrl.height = 1.68f;
        charCtrl.center = new Vector3(0.0f, charCtrl.height / 2.0f, 0.0f);

        moveSpeed = runMoveSpeed;
        gravity = 0f;
        gravityConstant = 1f;
        jumpHeight = 12f;
        isGrounded = false;

        jmp = Vector3.zero;
        numberJumps = 0;
        maxJumps = 2;

        walkableLayerIndex = LayerMask.NameToLayer("Walkable");
        if (walkableLayerIndex == -1) // Check if ground layer is valid
        {
            Debug.LogError("Walkable layer does not exist");
        }

        inAirVelocity = Vector3.zero;

        cameraOffset = new Vector3(0.25f, 1.85f, -3f);
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        lookSensitivityH = 10f;
        lookSensitivityV = 4f;
        cameraRotationXLimit = 60f;
        currentCameraRotationX = 0f;
        rotationMultiplier = 100f;
        xRot = 0f;
        rotation = Vector3.zero;
        gamePaused = true;
    }

    private void Start()
    {
        // Lock cursor and set it to be invisible
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        // Unlock cursor and set it to be visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!gamePaused && Input.GetKeyDown(KeyCode.Escape)) 
        {
            gamePaused = true;
            // Unlock cursor and set it to be visible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (gamePaused && Input.GetMouseButtonDown(0))
        {
            gamePaused = false;
            // Lock cursor and set it to be invisible
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        //UpdateGrounded();

        // Handle movement
        if (isGrounded)
        {
            // Handle Move input
            float _movX = Input.GetAxisRaw("Horizontal");
            float _movZ = Input.GetAxisRaw("Vertical");
            bool isCrouching = Input.GetKey(KeyCode.LeftControl);
            if (isCrouching)
            {
                moveSpeed = crouchMoveSpeed;
            }
            else
            {
                moveSpeed = runMoveSpeed;
            }

            if (_movX != 0 || _movZ != 0) // Has moved
            {
                Vector3 _movHorizontal = transform.right * _movX;
                Vector3 _movVertical = transform.forward * _movZ;

                // Calculate velocity and apply movement
                Vector3 _velocity = (_movHorizontal + _movVertical).normalized * moveSpeed;
                inAirVelocity = _velocity;
                if (!gamePaused)
                {
                    charCtrl.Move(_velocity * Time.deltaTime);
                }
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
        if (Input.GetButtonDown("Jump") && numberJumps < maxJumps && !gamePaused) { jmp = Vector3.up * jumpHeight; }

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxisRaw("Mouse X");
        rotation = new Vector3(0f, _yRot, 0f) * lookSensitivityH;
        // Calculate camera rotation
        xRot = Input.GetAxisRaw("Mouse Y") * lookSensitivityV;
        PerformRotation();


        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 100, Color.red, 0.2f);
        if (weaponLocation != null)
        {
            Debug.DrawRay(weaponLocation.transform.position, weaponLocation.transform.forward * 100, Color.blue, 0.2f);
            Debug.DrawRay(weaponLocation.transform.position, Camera.main.transform.forward * 100, Color.green, 0.2f);
        }
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        PerformJump();
        //CheckCollisionFlags();
        UpdateGrounded();
    }

    private void UpdateGrounded()
    {
        /* (charCtrl.collisionFlags == CollisionFlags.Below) means "Only touching ground, nothing else!"
        and the statement will be true only when we are on the edges of an object because of how collisions
        work in my case. I am getting the "Free floating" at all times when just moving on any surface and
        i don't know why. */
        if (charCtrl.collisionFlags == CollisionFlags.Below)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = Physics.Raycast(
                transform.position + Vector3.up * charCtrl.height / 2f,
                Vector3.down,
                charCtrl.height / 2f + charCtrl.skinWidth,
                1 << walkableLayerIndex);
        }
        Debug.DrawRay(transform.position + Vector3.up * charCtrl.height / 2f, Vector3.down * (charCtrl.height / 2f + charCtrl.skinWidth), Color.red, 0.02f);

        if (isGrounded) { numberJumps = 0; }  // Set number of jumps back to 0 because we hit the ground
        animationController.SetIsGrounded(isGrounded);
    }

    private void CheckCollisionFlags()
    {
        if (charCtrl.collisionFlags == CollisionFlags.None)
        {
            Debug.Log("Free floating!");
        }

        if ((charCtrl.collisionFlags & CollisionFlags.Sides) != 0)
        {
            Debug.Log("Touching sides!");
        }

        if (charCtrl.collisionFlags == CollisionFlags.Sides)
        {
            Debug.Log("Only touching sides, nothing else!");
        }

        if ((charCtrl.collisionFlags & CollisionFlags.Above) != 0)
        {
            Debug.Log("Touching Ceiling!");
        }

        if (charCtrl.collisionFlags == CollisionFlags.Above)
        {
            Debug.Log("Only touching Ceiling, nothing else!");
        }

        if ((charCtrl.collisionFlags & CollisionFlags.Below) != 0)
        {
            Debug.Log("Touching ground!");
        }

        if (charCtrl.collisionFlags == CollisionFlags.Below)
        {
            Debug.Log("Only touching ground, nothing else!");
        }
    }

    private void PerformRotation()
    {
        if (gamePaused) { return; }
        if (rotation != Vector3.zero)
        {  // Turn around player (horizontal axis)
            transform.rotation = (transform.rotation * Quaternion.Euler(rotation * Time.deltaTime * rotationMultiplier));
        }

        if (xRot != 0)
        {
            // Set rotation and clamp it
            float prevCameraRotationX = currentCameraRotationX;
            currentCameraRotationX -= xRot * Time.deltaTime * rotationMultiplier;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationXLimit, cameraRotationXLimit);

            if (prevCameraRotationX != currentCameraRotationX)  // Camera rotation changed
            {
                // Set animation vertical aim
                animationController.SetVerticalAim(-currentCameraRotationX / cameraRotationXLimit);

                // Apply first person rotation to camera
                //Camera.main.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
                // Apply third person rotation to camera
                Camera.main.transform.RotateAround(transform.position, transform.right, currentCameraRotationX - prevCameraRotationX);
            }
        }
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
            animationController.SetMustJump(true);
        }
    }
}
