using Mirror;
using UnityEngine;

public class PlayerMotor : NetworkBehaviour
{
    private Rigidbody rb;
    [SyncVar]
    private Vector3 velocity = Vector3.zero;
    [SyncVar]
    private Vector3 rotation = Vector3.zero;
    [SyncVar]
    private float cameraRotationX = 0f;
    private float currentCameraRotationX = 0f;
    private float rotationMultiplier = 100f;

    [SyncVar]
    Vector3 jmp = Vector3.zero;
    bool isGrounded = false;
    int numberJumps = 0;
    int maxJumps = 1;

    [SerializeField]
    private GameObject weaponsHolder;
    [SerializeField]
    private float cameraRotationXLimit = 70f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }

    [Command]
    public void CmdMove(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    [Command]
    public void CmdRotate(Vector3 _rotation)
    {
        rotation = _rotation;
    }

    [Command]
    public void CmdRotateCamera(float _cameraRotationX)
    {
        cameraRotationX = _cameraRotationX;
    }

    [Command]
    public void CmdJump(Vector3 _jmp)
    {
        jmp = _jmp;
    }

    void FixedUpdate()
    {
        PerformMovement();
        PerformJump();
        PerformRotation();
    }

    void PerformMovement()
    {
        if (velocity != Vector3.zero)
        {
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
    }

    void PerformJump()
    {
        if (numberJumps > maxJumps - 1)
        {
            isGrounded = false;
        }
        if (isGrounded && jmp != Vector3.zero)
        {
            rb.AddForce(jmp * Time.fixedDeltaTime, ForceMode.Impulse);
            numberJumps += 1;
            jmp = Vector3.zero;
        }
    }

    void PerformRotation()
    {
        if (rotation != Vector3.zero)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(rotation * Time.fixedDeltaTime * rotationMultiplier));
        }

        if (cameraRotationX != 0 && isLocalPlayer)
        {
            // Set rotation and clamp it
            currentCameraRotationX -= cameraRotationX * Time.fixedDeltaTime * rotationMultiplier;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationXLimit, cameraRotationXLimit);
            Vector3 _newRotation = new Vector3(currentCameraRotationX, 0f, 0f);
            
            // Apply rotation to camera
            Camera.main.transform.localEulerAngles = _newRotation;
            // Move weapons (using network transform child with weaponsHolder as gameObject with client authority)
            weaponsHolder.transform.localEulerAngles = _newRotation;
            
        }
    }
    void OnCollisionEnter(Collision other)
    {
        isGrounded = true;
        numberJumps = 0;
    }
}
