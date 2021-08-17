using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : NetworkBehaviour
{
    private PlayerAnimationStateController animationController;
    private CharacterController charCtrl;

    #region Movement variables/constants

    private Vector3 velocity;
    private Vector3 rotation;
    private GameObject hitBoxParent;

    #endregion

    #region Camera variables/constants

    private float cameraRotationX;
    private Transform rawCameraTransform;
    private float cameraToPlayerDistance;
    private float currentCameraRotationX;
    private float rotationMultiplier;
    private Vector3 cameraOffset;
    [SerializeField]
    private float lookUpLimit;
    [SerializeField]
    private float lookDownLimit;
    private bool mustResetCamera;

    #endregion

    #region Jump variables/constants

    private Vector3 jmp;
    private int numberJumps;
    private int maxJumps;

    #endregion

    #region Layer indexes variables/constants

    private int walkableLayerIndex;
    private int rampLayerIndex;
    private int playerLayerIndex;
    private int groundMask;

    #endregion

    #region Gravity/Momentum related variables/constants

    private bool isGrounded;
    [SerializeField]
    private float gravityConstant;
    private float gravity;
    private Vector3 inAirVelocity;  // Used to continue going into a direction while in air

    #endregion



    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationStateController>();

        #region Initialize Character Controller

        charCtrl = GetComponent<CharacterController>();
        charCtrl.stepOffset = 0.1f;
        charCtrl.skinWidth = 0.03f;
        charCtrl.minMoveDistance = 0.001f;
        charCtrl.radius = 0.3f;
        charCtrl.height = 1.68f;
        charCtrl.center = new Vector3(0.0f, charCtrl.height / 2.0f, 0.0f);

        #endregion

        #region Initialize movement variables/constants
        
        velocity = Vector3.zero;
        rotation = Vector3.zero;
        hitBoxParent = transform.Find("Root").gameObject;

        #endregion

        #region Initialize camera variables/constants

        cameraRotationX = 0f;
        currentCameraRotationX = 0f;
        rotationMultiplier = 100f;
        lookUpLimit = 60f;
        lookDownLimit = -60f;
        mustResetCamera = false;
        
        #endregion

        #region Initialize jump variables/constants
        
        jmp = Vector3.zero;
        numberJumps = 0;
        maxJumps = 1;
        
        #endregion

        #region Initialize layer indexes variables/constants
        
        walkableLayerIndex = LayerMask.NameToLayer("Walkable");
        if (walkableLayerIndex == -1) // Check if ground layer is valid
        {
            Debug.LogError("Walkable layer does not exist");
        }
        rampLayerIndex = LayerMask.NameToLayer("Ramp");
        if (rampLayerIndex == -1) // Check if ramp layer is valid
        {
            Debug.LogError("Ramp layer does not exist");
        }
        playerLayerIndex = LayerMask.NameToLayer("Player");
        if (playerLayerIndex == -1) // Check if player layer is valid
        {
            Debug.LogError("Player layer does not exist");
        }
        groundMask = 1 << walkableLayerIndex | 1 << rampLayerIndex | 1 << playerLayerIndex;
        
        #endregion

        #region Initialize gravity/momentum related variables/constants
        
        isGrounded = false;
        gravityConstant = 1f;
        gravity = 0f;
        inAirVelocity = Vector3.zero;
        
        #endregion
    }

    private void Start()
    {
        #region Initialize camera variables/constants
        if (isLocalPlayer)  // Only the script attached to local player needs to find and get the rawCameraTransform
        {
            cameraOffset = PlayerScript.cameraOffset;

            // Set raw camera object
            rawCameraTransform = GameObject.Find("RawCameraTransform").transform;
            rawCameraTransform.transform.SetParent(transform);
            rawCameraTransform.transform.localPosition = cameraOffset;
            rawCameraTransform.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

            cameraToPlayerDistance = Vector3.Distance(transform.position + Vector3.up * cameraOffset.y, rawCameraTransform.position);
        }
        #endregion
    }

    private void Update()
    {
        // Let us be local player X. There is no point in running this script section on player Y's attached script from player X client
        // In other words if we don't have authority to move this player return
        if (!hasAuthority || !isLocalPlayer) { return; }
        if (!charCtrl.enabled) { return; }
        PerformMovement();
        PerformRotation();
        PerformCameraRotation();
        //CheckCollisionFlags();
    }

    void FixedUpdate()
    {
        // Let us be local player X. There is no point in running this script section on player Y's attached script from player X client
        // In other words if we don't have authority to move this player return
        if (!hasAuthority || !isLocalPlayer) { return; }
        if (!charCtrl.enabled) { return; }
        ApplyGravity();
        PerformJump();
        FixCameraTransform();
        UpdateGrounded();
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
            transform.rotation = transform.rotation * Quaternion.Euler(rotationMultiplier * Time.deltaTime * rotation);
        }
    }

    /// <summary> Actual code for performing player camera rotation on the vertical axis </summary>
    void PerformCameraRotation()
    {
        if (Camera.main.transform.parent != transform) // It doesn't belong to our player (Is in spectate mode)
        {
            return;
        }

        if (cameraRotationX != 0)
        {
            // Set rotation and clamp it
            float prevCameraRotationX = currentCameraRotationX;
            currentCameraRotationX -= cameraRotationX * Time.deltaTime * rotationMultiplier;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, lookDownLimit, lookUpLimit);

            if (prevCameraRotationX != currentCameraRotationX)  // Camera rotation changed
            {
                // Set animation vertical aim
                float _verticalAim;
                if (currentCameraRotationX < 0)
                {
                    _verticalAim = -currentCameraRotationX / lookUpLimit;
                }
                else
                {
                    _verticalAim = currentCameraRotationX / lookDownLimit;
                }
                animationController.SetVerticalAim(_verticalAim);

                // Apply third person rotation to camera
                // Would it be cheaper if we just assign camera.main.transform.position/rotation instead of rotating twice ? Requires testing.
                Camera.main.transform.RotateAround(transform.position + Vector3.up * cameraOffset.y,
                    transform.right,
                    currentCameraRotationX - prevCameraRotationX);
                rawCameraTransform.RotateAround(transform.position + Vector3.up * cameraOffset.y,
                    transform.right,
                    currentCameraRotationX - prevCameraRotationX);
            }
        }
    }

    /// <summary> In case of camera collision this function will take control of the camera position. </summary>
    private void FixCameraTransform()
    {
        if (Camera.main.transform.parent != transform) // It doesn't belong to our player (Is in spectate mode)
        {
            mustResetCamera = true;
            return;
        }
        if (mustResetCamera)
        {
            mustResetCamera = false;
            Camera.main.transform.position = rawCameraTransform.position;
            Camera.main.transform.rotation = rawCameraTransform.rotation;
        }

        Vector3 _origin = transform.position + Vector3.up * cameraOffset.y;
        Vector3 _dir = Vector3.Normalize(rawCameraTransform.position - _origin);

        bool _hitObj = Physics.SphereCast(
            _origin,
            0.1f,  /* Sphere radius */
            _dir,
            out RaycastHit _hitInfo,
            cameraToPlayerDistance,
            ~(1 << playerLayerIndex) /* Everything but player */);

        if (_hitObj)
        {
            float _clipOffset = 0.2f;
            Vector3 _desiredPosition = _hitInfo.point + _hitInfo.normal * _clipOffset;
            if (Camera.main.transform.position != _desiredPosition)
            {
                Camera.main.transform.position = Vector3.Lerp(
                    Camera.main.transform.position,
                    _desiredPosition,
                    Time.deltaTime * 5);
            }
        }
        else if (Camera.main.transform.position != rawCameraTransform.position)
        {
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                rawCameraTransform.position,
                Time.deltaTime * 5);
        }
    }

    /// <summary> Updates isGrounded internal boolean and resets the allowed number of jumps </summary>
    private void UpdateGrounded()
    {
        hitBoxParent.SetActive(false);    // This gameobject has the weapon and hit boxes of us that we want to ignore.
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

        hitBoxParent.gameObject.SetActive(true);    // This gameobject has the weapon and hit boxes of us that we want to ignore.
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
}
