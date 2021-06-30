﻿using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float jumpHeight;

    private PlayerMotor motor;

    private void Awake()
    {
        jumpHeight = 18f;
        motor = GetComponent<PlayerMotor>();
    }

    public void MovePlayer(float _moveSpeed, int _maxJumps, float _lookSensitivityH)
    {
        // Handle move input
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");

        Vector3 _movHorizontal = transform.right * _movX;
        Vector3 _movVertical = transform.forward * _movZ;

        // Calculate velocity and apply movement
        Vector3 _velocity = (_movHorizontal + _movVertical).normalized * _moveSpeed;
        motor.Move(_velocity);

        // Calculate rotation only to rotate player (not camera) on horizontal axis (turning around)
        float _yRot = Input.GetAxisRaw("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * _lookSensitivityH;
        motor.Rotate(_rotation);  // Apply rotation

        // Check if the player tries to jump
        if (Input.GetButtonDown("Jump")) { motor.Jump(_maxJumps, Vector3.up * jumpHeight); }
    }

    public void MoveCamera(float _lookSensitivityV)
    {
        float _xRot = Input.GetAxisRaw("Mouse Y") * _lookSensitivityV;
        motor.RotateCamera(_xRot);
    }
}
