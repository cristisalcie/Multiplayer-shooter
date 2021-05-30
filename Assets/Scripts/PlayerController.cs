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

    private PlayerMotor motor;

    private void Awake()
    {
        moveSpeed = 10f;
        lookSensitivityH = 8f;
        lookSensitivityV = 5f;

        motor = GetComponent<PlayerMotor>();
    }

    public void MovePlayer()
    {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");

        Vector3 _movHorizontal = transform.right * _movX;
        Vector3 _movVertical = transform.forward * _movZ;

        Vector3 _velocity = (_movHorizontal + _movVertical).normalized * moveSpeed;

        motor.Move(_velocity);

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxis("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * lookSensitivityH;
        motor.Rotate(_rotation);
    }

    public void MoveCamera()
    {
        float _xRot = Input.GetAxis("Mouse Y") * lookSensitivityV;
        motor.RotateCamera(_xRot);
    }
}
