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

    void FixedUpdate()
    {
        PerformMovement();
        PerformRotation();
    }

    void PerformMovement()
    {
        if (velocity != Vector3.zero)
        {
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
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
}
