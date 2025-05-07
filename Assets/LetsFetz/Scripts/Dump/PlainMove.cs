using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlainMove : MonoBehaviour {
    // Player Movement
    private Rigidbody playerRb;
    private NewPlayerInputActions _playerInputActions;

    private float _thresholdMovement = 0.05f;
    [SerializeField] private float _cooldownJump = 0.75f;
    private bool _isMoving;
    private bool _isSprinting;
    private bool _isGrounded = true;
    private bool _isJumping = false;

    [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    // Player Movement Values
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float breakForce = 0.2f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float maxSpeed = 10f;

    // Camera Movement
    private const float _thresholdCamera = 0.01f;
    private float _rotationVelocity;
    private float _cinemachineTargetPitch;

    // Camera Movement Values
    [Tooltip("How fast the player rotates")] [SerializeField]
    private float rotationSpeed = 0.1f;

    [Tooltip("How far in degrees can you move the camera up")] [SerializeField]
    private float topClamp = 85.0f;

    [Tooltip("How far in degrees can you move the camera down")] [SerializeField]
    private float bottomClamp = -85.0f;

    // Camera attach
    private GameObject _cinemachineVirtualCamera;
    [SerializeField] private GameObject _cinemachineCameraTarget;

    private Collider lastCollision;
    
    // Random Property Section
    [SerializeField] private GameObject[] goPovElements;

    private void Awake() {
        playerRb = GetComponent<Rigidbody>();
    }

    private void Start() {
        Initialize();
        //_mainCamera = GameObject.FindWithTag("MainCamera");
    }

    private void Initialize() {
        //Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} joined");
        
        //TODO: Adjust Time.fixeddeltatime according to application framerate

        foreach (var go in goPovElements) {
            go.layer = LayerMask.NameToLayer("POV");
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Jump.performed += Jump_performed;
        _playerInputActions.Player.Sprint.started += Sprint_started;
        _playerInputActions.Player.Sprint.canceled += Sprint_canceled;

        // Make local cinemachine vcam follow this GameObject
        _cinemachineVirtualCamera = GameObject.FindGameObjectWithTag("VirtualCamera");
        _cinemachineVirtualCamera.GetComponent<CinemachineVirtualCamera>().Follow =
            _cinemachineCameraTarget.transform;
    }
    
    #region Movement

    private void LateUpdate() {
        PlayerRotation();
    }

    private void FixedUpdate() {

        //PlayerRotation();

        Vector2 inputVector = _playerInputActions.Player.Movement.ReadValue<Vector2>();

        // We only want to do movement calculations if there is input
        if (inputVector.magnitude != 0) {
            //_forwardVector = transform.forward * inputVector.y;
            //_sidewaysVector = transform.right * inputVector.x;
            Vector3 move = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized *
                           movementSpeed;

            float thresholdSpeed = maxSpeed;

            if (_isSprinting) {
                move *= sprintMultiplier;
                thresholdSpeed *= sprintMultiplier;
            }

            playerRb.AddForce(new Vector3(move.x, 0f, move.z), ForceMode.Acceleration);

            // If horizontal velocity is higher than defined maximum, set to maximum
            if (VelocityHorizontal() > thresholdSpeed) {
                float y = playerRb.velocity.y;
                Vector3 vectorMaxSpeed = new Vector3(thresholdSpeed, 1, thresholdSpeed);
                playerRb.velocity = Vector3.Scale(playerRb.velocity.normalized, vectorMaxSpeed);
                playerRb.velocity = new Vector3(playerRb.velocity.x, y, playerRb.velocity.z);
            }

            // If there is no input, we check whether the horizontal velocity can be clamped to 0
        } else if (0 < VelocityHorizontal() && VelocityHorizontal() < _thresholdMovement) {
            playerRb.velocity = Vector3.Scale(playerRb.velocity, new Vector3(0, 1, 0));

            // Add some friction to reduce sliding
            //TODO: this shall only happen, while not in the air
        } else if (VelocityHorizontal() > _thresholdMovement) {
            playerRb.velocity = Vector3.Scale(playerRb.velocity, new Vector3(breakForce, 1, breakForce));
        }
        
        GroundedCheck();
        
    }
    
    private IEnumerator JumpCooldown() {
        _isJumping = true;
        yield return new WaitForSeconds(_cooldownJump);
        _isJumping = false;
    }

    private void GroundedCheck() {
        var pos = transform.position;
        Vector3 spherePosition = new Vector3(pos.x, pos.y - GroundedOffset,
            pos.z);
        _isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }
    
    // Rotating the player transform to match Camera POV
    private void PlayerRotation() {
        //transform.rotation = UnityEngine.Quaternion.Euler(transform.eulerAngles.x, _mainCamera.transform.eulerAngles.y, transform.eulerAngles.z);
        Vector2 look = _playerInputActions.Player.Look.ReadValue<Vector2>();
        if (look.sqrMagnitude >= _thresholdCamera) {
            _cinemachineTargetPitch += -look.y * rotationSpeed;
            _rotationVelocity = look.x * rotationSpeed;

            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

            _cinemachineCameraTarget.transform.localRotation =
                UnityEngine.Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private float VelocityHorizontal() {
        return Mathf.Abs(playerRb.velocity.x) + Mathf.Abs(playerRb.velocity.z);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    #endregion
    
    #region Input Methods

    private void Sprint_started(InputAction.CallbackContext context) {
        _isSprinting = true;
    }

    private void Sprint_canceled(InputAction.CallbackContext context) {
        _isSprinting = false;
    }

    private void Jump_performed(InputAction.CallbackContext context) {
        if (!_isGrounded || _isJumping) return;
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        StartCoroutine(JumpCooldown());
    }

    #endregion

    
    
}