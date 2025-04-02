using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class NewNoNetworkFirstPersonController : MonoBehaviour {

    [SerializeField] private Transform weapon;
    [SerializeField] private Transform weaponSpawn;
    [SerializeField] private Transform bulletPrefab;

    // Player Movement
    private Rigidbody playerRb;
    private NewPlayerInputActions _playerInputActions;
    private GameObject _mainCamera;

    private float _thresholdMovement = 0.05f;
    private bool _isMoving;
    private bool _isSprinting;

    private Vector3 _forwardVector;
    private Vector3 _sidewaysVector;
    
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
    [Tooltip("How fast the player rotates")]
    [SerializeField] private float rotationSpeed = 1.0f;
    
    [Tooltip("How far in degrees can you move the camera up")]
    [SerializeField] private float topClamp = 85.0f;
    
    [Tooltip("How far in degrees can you move the camera down")]
    [SerializeField] private float bottomClamp = -85.0f;

    [SerializeField] private Camera _camera;
    
    // Camera attach
    private GameObject _cinemachineVirtualCamera;
    [SerializeField] private GameObject _cinemachineCameraTarget;
    
    private void Awake() {
        playerRb = GetComponent<Rigidbody>();
        
        //playerRb.constraints = RigidbodyConstraints.FreezeRotationY;
    }

    private void Start() {
        Initialize();
    }

    //public override void OnNetworkSpawn() {
    //    if(IsOwner) Initialize();
    //}

    private void Initialize() {
        //Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} joined");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        
        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Jump.performed += Jump_performed;
        _playerInputActions.Player.Movement.started += Movement_started;
        _playerInputActions.Player.Movement.performed += Movement_performed;
        _playerInputActions.Player.Movement.canceled += Movement_canceled;
        _playerInputActions.Player.Sprint.started += Sprint_started;
        _playerInputActions.Player.Sprint.canceled += Sprint_canceled;
        
        _playerInputActions.Player.Look.performed += Look_performed;
        _playerInputActions.Player.ADS.started += ADS_started;
        _playerInputActions.Player.ADS.canceled += ADS_canceled;
        _playerInputActions.Player.Shoot.started += Shoot_started;
        _playerInputActions.Player.Shoot.canceled += Shoot_canceled;

        _playerInputActions.Player.Reload.performed += Reload_performed;
        
        // Make local cinemachine vcam follow this GameObject
        //_cinemachineVirtualCamera = GameObject.FindGameObjectWithTag("VirtualCamera");
        //_cinemachineVirtualCamera.GetComponent<CinemachineVirtualCamera>().Follow =
            //_cinemachineCameraTarget.transform;

        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void LateUpdate() {
        //if (IsOwner)
        //PlayerRotation();
        //CameraRotation();
    }

    private void FixedUpdate() {
        //if (!IsOwner) return;
        
        //PlayerRotation();

        Vector2 inputVector = _playerInputActions.Player.Movement.ReadValue<Vector2>();
        
        // We only want to do movement calculations if there is input
        if (inputVector.magnitude != 0) {
            //_forwardVector = transform.forward * inputVector.y;
            //_sidewaysVector = transform.right * inputVector.x;
            Vector3 move = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized * movementSpeed;

            float thresholdSpeed = maxSpeed;
            
            if (_isSprinting) {
                move *= sprintMultiplier;
                thresholdSpeed *= sprintMultiplier;
            }
            
            playerRb.AddForce(new Vector3(move.x, 0f, move.z), ForceMode.Acceleration);
            
            // If horizontal velocity is higher than defined maximum, set to maximum
            //TODO: MAKE IT UNABHÄNGIG VOM Y WERT!!!
            if (VelocityHorizontal()> thresholdSpeed) {
                float y = playerRb.velocity.y;
                Vector3 vectorMaxSpeed = new Vector3(thresholdSpeed, 1, thresholdSpeed);
                playerRb.velocity = Vector3.Scale(playerRb.velocity.normalized,vectorMaxSpeed);
                playerRb.velocity = new Vector3(playerRb.velocity.x, y, playerRb.velocity.z);
            }
            
            // If there is no input, we check whether the horizontal velocity can be clamped to 0
        } else if(0 < VelocityHorizontal() && VelocityHorizontal() < _thresholdMovement) {
            playerRb.velocity = Vector3.Scale(playerRb.velocity, new Vector3(0, 1, 0));
            
            // Add some friction to reduce sliding
            //TODO: this shall only happen, while not in the air
        }  else if (VelocityHorizontal() > _thresholdMovement) {
            playerRb.velocity = Vector3.Scale(playerRb.velocity, new Vector3(breakForce, 1, breakForce));
        }
    }
    
    private void Sprint_started(InputAction.CallbackContext context) {
        _isSprinting = true;
    }
    
    private void Sprint_canceled(InputAction.CallbackContext context) {
        _isSprinting = false;
    }
    
    private void ADS_started(InputAction.CallbackContext context) {
        Debug.Log("ADS!");
        //_cinemachineVirtualCamera;
    }
    
    private void ADS_canceled(InputAction.CallbackContext context) {
        Debug.Log("No ADS!");
    }

    private void Shoot_started(InputAction.CallbackContext context) {
        Debug.Log("Shoot!");
        ShootBulletServerRpc();
    }

    private void Shoot_canceled(InputAction.CallbackContext context) {
        Debug.Log("Stop Shooting!");
    }
    
    private void Reload_performed(InputAction.CallbackContext context) {
        Debug.Log("Reload!");
        ReloadWeaponServerRpc();
    }

    private void Movement_started(InputAction.CallbackContext context) {
        
    }
    
    private void Movement_performed(InputAction.CallbackContext context) {
        
    }
    
    private void Movement_canceled(InputAction.CallbackContext context) {
        _forwardVector = Vector3.zero;
        _sidewaysVector = Vector3.zero;
    }

    private void Jump_performed(InputAction.CallbackContext context) {
        //Debug.Log(context);
        //Debug.Log($"$Player {NetworkManager.Singleton.LocalClientId} jumped!");
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }
    
    private void Look_performed(InputAction.CallbackContext context) {
        /*
        Vector2 input = context.ReadValue<Vector2>() * rotationSpeed / 50.0f;
        transform.Rotate(0, input.x, 0);
        Transform tf = _camera.transform;
        tf.Rotate(-input.y, 0, 0);
        */
        //PlayerRotation();
    }

    private void CameraRotation() {
        _rotationVelocity = _playerInputActions.Player.Look.ReadValue<Vector2>().x;
        transform.Rotate(Vector3.up * _rotationVelocity);
    }

    // Rotating the player transform to match Camera POV
    // TODO: fine tuning with triggering in look.performed, remove tiny stuttering. doesn't matter for now
    private void PlayerRotation() {
        //transform.rotation = UnityEngine.Quaternion.Euler(transform.eulerAngles.x, _mainCamera.transform.eulerAngles.y, transform.eulerAngles.z);
    }

    private float VelocityHorizontal() {
        return Mathf.Abs(playerRb.velocity.x) + Mathf.Abs(playerRb.velocity.z);
    }
    
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
    
    [ServerRpc]
    private void ShootBulletServerRpc() {
        Transform bullet = Instantiate(bulletPrefab, weaponSpawn);
        //bullet.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    private void ReloadWeaponServerRpc() {
        Transform spawnedWeapon = Instantiate(weapon, weaponSpawn);
        //spawnedWeapon.GetComponent<NetworkObject>().Spawn();
        //spawnedWeapon.GetComponent<NetworkObject>().TrySetParent(transform);
    }

    [ServerRpc]
    private void DieServerRpc() {
        GetComponent<NetworkObject>().Despawn();
    }

    /*private void OnTriggerEnter(Collider other) {
        if (!IsOwner) return;
        if (other.CompareTag("Bullet") && !other.GetComponent<NetworkObject>().IsOwner) {
            Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} was shot!");
            DieServerRpc();
        }
    }*/
}