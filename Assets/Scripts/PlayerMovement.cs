using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour {
    
    private Rigidbody _playerRb;
    private PlayerInputActions _playerInputActions;
    private bool _isMoving;
    private bool _isSprinting;
    private bool _clampMove;
    
    [SerializeField] private float breakForce;
    [SerializeField] [Range(1,50)] private float acceleration;
    [SerializeField] [Range(1,20)] private float sprintMultiplier;
    [SerializeField] [Range(1,10)] private int maxSpeed;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private AudioListener audioListener;

    [SerializeField] private Transform tfBody;
    
    private void Awake() {
        Cursor.visible = false;
        _playerRb = GetComponent<Rigidbody>();
        //_playerInput = GetComponent<PlayerInput>();
        //_playerInput.onActionTriggered += PlayerInput_onActionTriggered;

        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Jump.performed += Jump;
        _playerInputActions.Player.Movement.performed += Movement_performed;
        _playerInputActions.Player.Movement.canceled += Movement_canceled;
        _playerInputActions.Player.Sprint.started += Sprint_started;
        _playerInputActions.Player.Sprint.canceled += Sprint_canceled;
        //_playerInputActions.Player.Movement.performed += Movement_performed;
    }

    private void Initialize() {
        if (IsOwner) {
            audioListener.enabled = true;
            virtualCamera.Priority = 1;
        } else {
            virtualCamera.Priority = 0;
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Update() {
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Debug.Log("LMB!");
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            _playerRb.velocity = Vector3.zero;
            transform.position = Vector3.up;
        }
    }
    
    private void FixedUpdate() {
        
        //float velocityHorizontal = new Vector3(_playerRb.velocity.x, 0, _playerRb.velocity.z).magnitude;
        
        //if (velocityHorizontal < maxSpeed) {
            Debug.Log("MOVE!");
            Vector2 inputVector = _playerInputActions.Player.Movement.ReadValue<Vector2>();
            /*
             * w 0 1
             * a -1 0
             * s 0 -1
             * d 1 0
             */

            Transform tfPlayer = transform;
            Rigidbody rbPlayer = _playerRb;

            Vector3 moveZ = tfBody.forward * inputVector.y;
            Vector3 moveX = tfBody.right * inputVector.x;

            Vector3 move = moveX + moveZ;

            //Vector3 move = new Vector3(inputVector.y,0,inputVector.x);
            //Vector3 move = new Vector3(inputVector.x, 0, inputVector.y);
            //Vector3 move = new Vector3(inputVector.x * forward.x, 0, inputVector.y * forward.z);

            //Debug.Log("Forward x: " + forward.x + " y: " + forward.y + " z: " + forward.z);
            //Debug.Log("Input x: " + inputVector.x + " y: " + inputVector.y);

            //Debug.Log("Velocity: " + rbPlayer.velocity.magnitude);

            float sprintSpeed = 1;
            if (_isSprinting) sprintSpeed = sprintMultiplier;

            // APPLYING FORCE HERE
            // ------------------------------------------------------------------------------
            if (_isMoving) {
                _playerRb.AddForce(move * (sprintSpeed * acceleration), ForceMode.VelocityChange);
                // Don't go too fast!
                if (new Vector3(rbPlayer.velocity.x, 0, rbPlayer.velocity.z).magnitude > maxSpeed) {
                    _playerRb.velocity = new Vector3(move.normalized.x * maxSpeed, rbPlayer.velocity.y,move.normalized.z * maxSpeed);
                }
            }
            // ------------------------------------------------------------------------------

            else {
                float velocityHorizontal = new Vector3(rbPlayer.velocity.x, 0, rbPlayer.velocity.z).magnitude;
                if (_clampMove && velocityHorizontal < 0.1) {
                    _playerRb.velocity = new Vector3(0, _playerRb.velocity.y, 0);
                    _clampMove = !_clampMove;
                    //Debug.Log("Stopp!");
                }

                if (velocityHorizontal >= 0.1) {
                    //Debug.Log("BREMSEN!!\nVelocity: " + velocity);
                    _playerRb.velocity = new Vector3(rbPlayer.velocity.x * breakForce, rbPlayer.velocity.y, rbPlayer.velocity.z * breakForce);
                }

            }

        //}

    }
    
    private void Sprint_started(InputAction.CallbackContext obj) {
        _isSprinting = true;
    }

    private void Sprint_canceled(InputAction.CallbackContext obj) {
        _isSprinting = false;
    }

    private void Movement_performed(InputAction.CallbackContext context) {
        _isMoving = true;
        _clampMove = true;
    }

    private void Movement_canceled(InputAction.CallbackContext context) {
        _isMoving = false;
    }
    
    public void Jump(InputAction.CallbackContext context) {
        //Debug.Log(context);
        if (context.performed) {
            //Debug.Log("Jump " + context.phase);
            _playerRb.AddForce(Vector3.up * 5, ForceMode.Impulse);
        }
        
    }

    private void OnEnable() {
        _playerInputActions.Enable();
    }

    private void OnDisable() {
        _playerInputActions.Disable();
    }
    
}
