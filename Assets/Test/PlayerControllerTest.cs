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
    private int rampLayerIndex;
    private Vector3 inAirVelocity;

    private Vector3 cameraOffset;

    [SerializeField]
    private float lookSensitivityH;

    [SerializeField]
    private float lookSensitivityV;

    [SerializeField]
    private float cameraRotationXLimitDown;
    [SerializeField]
    private float cameraRotationXLimitUp;

    private float currentCameraRotationX;
    private float rotationMultiplier;
    private float xRot;
    private Vector3 rotation;

    private bool gamePaused;

    private const float runMoveSpeed = 6f;

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
        rampLayerIndex = LayerMask.NameToLayer("Ramp");
        if (rampLayerIndex == -1) // Check if ground layer is valid
        {
            Debug.LogError("Ramp layer does not exist");
        }

        inAirVelocity = Vector3.zero;

        cameraOffset = new Vector3(0.5f, 1.4f, -2f);
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = cameraOffset;
        Camera.main.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        lookSensitivityH = 10f;
        lookSensitivityV = 4f;
        cameraRotationXLimitDown = 75f;
        cameraRotationXLimitUp = 75f;
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


        //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 100, Color.red, 0.2f);
        if (weaponLocation != null)
        {
            //Debug.DrawRay(weaponLocation.transform.position, weaponLocation.transform.forward * 100, Color.blue, 0.2f);
            //Debug.DrawRay(weaponLocation.transform.position, Camera.main.transform.forward * 100, Color.green, 0.2f);
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
        int groundMask = 1 << walkableLayerIndex | 1 << rampLayerIndex;
        /* First if is going to sometimes detect that we are touching the ground, but not always. Hence the
        need for the else statement. */
        if ((charCtrl.collisionFlags & CollisionFlags.Below) != 0)
        {
            isGrounded = true;
        }
        else
        {
            // Raycast at circle center
            isGrounded = Physics.Raycast(
                transform.position + Vector3.up * charCtrl.height / 2f,
                Vector3.down,
                charCtrl.height / 2f + charCtrl.skinWidth * 2f,
                groundMask);

            if (!isGrounded)
            {
                int _circlePrecision = 4;
                float _angleOffset = 360f / _circlePrecision;
                float _currentAngle = 0f;

                // Raycast close to circle edges
                for (int i = 0; i < _circlePrecision; ++i)
                {
                    Vector3 _rayPosition = transform.position + Vector3.up * charCtrl.height / 2f;
                    _rayPosition.x += Mathf.Cos(Mathf.Deg2Rad * _currentAngle) * charCtrl.radius * 0.8f;
                    _rayPosition.z += Mathf.Sin(Mathf.Deg2Rad * _currentAngle) * charCtrl.radius * 0.8f;

                    isGrounded = Physics.Raycast(
                        _rayPosition,
                        Vector3.down,
                        charCtrl.height / 2f + charCtrl.skinWidth * 2,
                        groundMask);

                    if (isGrounded) { break; }
                    _currentAngle += _angleOffset;
                }
            }
        }

        if (isGrounded) { numberJumps = 0; }  // Set number of jumps back to 0 because we hit the ground

        // Determine if we are on a ramp object.
        bool _onRamp = Physics.Raycast(
            transform.position + Vector3.up * charCtrl.height / 2f,
            Vector3.down,
            charCtrl.height / 2f + charCtrl.skinWidth * 10f,
            1 << rampLayerIndex);

        // The following if will make sure we don't transit between airborne and stand locomotion animations when going down a ramp.
        if (_onRamp)
        {
            animationController.SetIsGrounded(true);
        }
        else
        {
            animationController.SetIsGrounded(isGrounded);
        }

        // Debug rays
        //Debug.DrawRay(transform.position + Vector3.up * charCtrl.height / 2f,
        //    Vector3.down * (charCtrl.height / 2f + charCtrl.skinWidth * 10f),
        //    Color.yellow,
        //    0.02f);
        //Debug.DrawRay(transform.position + Vector3.up * charCtrl.height / 2f,
        //    Vector3.down * (charCtrl.height / 2f + charCtrl.skinWidth * 2f),
        //    Color.red,
        //    0.02f);
        //{
        //    int _circlePrecision = 4;
        //    float _angleOffset = 360f / _circlePrecision;
        //    float _currentAngle = 0f;

        //    // Raycast close to circle edges
        //    for (int i = 0; i < _circlePrecision; ++i)
        //    {
        //        Vector3 _rayPosition = transform.position + Vector3.up * charCtrl.height / 2f;
        //        _rayPosition.x += Mathf.Cos(Mathf.Deg2Rad * _currentAngle) * charCtrl.radius * 0.8f;
        //        _rayPosition.z += Mathf.Sin(Mathf.Deg2Rad * _currentAngle) * charCtrl.radius * 0.8f;

        //        Debug.DrawRay(_rayPosition,
        //            Vector3.down * (charCtrl.height / 2f + charCtrl.skinWidth * 2f),
        //            Color.blue,
        //            0.02f);
        //        _currentAngle += _angleOffset;
        //    }
        //}
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
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationXLimitUp, cameraRotationXLimitDown);

            if (prevCameraRotationX != currentCameraRotationX)  // Camera rotation changed
            {
                // Set animation vertical aim
                float _verticalAim = 0f;
                if (currentCameraRotationX < 0)
                {
                    _verticalAim = -currentCameraRotationX / cameraRotationXLimitUp;
                }
                else
                {
                    _verticalAim = -currentCameraRotationX / cameraRotationXLimitDown;
                }
                animationController.SetVerticalAim(_verticalAim);

                // Apply first person rotation to camera
                //Camera.main.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
                // Apply third person rotation to camera
                Camera.main.transform.RotateAround(transform.position + Vector3.up * charCtrl.height / 2f,
                    transform.right,
                    currentCameraRotationX - prevCameraRotationX);
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
    private void PerformJump()
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
