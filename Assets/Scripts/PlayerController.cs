using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float lookSensitivityH;
    [SerializeField]
    private float lookSensitivityV;
    
    [SerializeField]
    private float jumpHeight;


    private PlayerMotor motor;

    private void Awake()
    {
        moveSpeed = 10f;
        lookSensitivityH = 5f;
        lookSensitivityV = 5f;

        jumpHeight = 6000f;

        motor = GetComponent<PlayerMotor>();
    }

    public void MovePlayer()
    {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");

        Vector3 _movHorizontal = transform.right * _movX;
        Vector3 _movVertical = transform.forward * _movZ;

        Vector3 _velocity = (_movHorizontal + _movVertical).normalized * moveSpeed;

        motor.CmdMove(_velocity);

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxisRaw("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * lookSensitivityH;
        motor.CmdRotate(_rotation);

        // Control jump
        if (Input.GetButtonDown("Jump"))
        {
            motor.CmdJump(Vector3.up * jumpHeight);
        }
    }

    public void MoveCamera()
    {
        float _xRot = Input.GetAxisRaw("Mouse Y") * lookSensitivityV;
        motor.CmdRotateCamera(_xRot);
    }
}
