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

public class NewNetworkFirstPersonController : NetworkBehaviour {
    [SerializeField] private Transform weapon;
    [SerializeField] private Transform weaponSpawn;
    [SerializeField] private Transform bulletPrefab;

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

    private HealthSystem _healthSystem;
    [SerializeField] private HealthBar healthBar;
    
    // Random Property Section
    [SerializeField] private GameObject[] goPovElements;
    
    [SerializeField] private GameObject zealYellowPrefab;
    [SerializeField] private GameObject zealRedPrefab;

    [SerializeField] private List<Transform> zealSpawnPoints;
    
    private Random rng;
    
    private GameObject startMatchButton;
    private GameObject timer;
    private TMP_Text timerTxt;

    private int _minutesRemaining = 20;
    private int _secondsRemaining;
    private int _secondsPassed = 0;

    private bool _inCombat;
    private bool _firstLife = true;

    private Coroutine _cOutOfCombatTimer;

    private GameObject _mainCamera;
    // End of Random Property Section
    

    private void Awake() {
        playerRb = GetComponent<Rigidbody>();
    }

    private void Start() {
        _mainCamera = GameObject.FindWithTag("MainCamera");
    }

    public override void OnNetworkSpawn() {

        if (OwnerClientId == 0) {
            // Seed generation voodoo magic
            long currentTimeTicks = DateTime.UtcNow.Ticks;
            uint seed = (uint)(currentTimeTicks ^ (currentTimeTicks >> 32));
        
            rng = new Random(seed);
            zealSpawnPoints = GameObject.Find("ZealSpawnPoints").GetComponentsInChildren<Transform>().ToList();
            zealSpawnPoints.RemoveAt(0);
        }
        
        _healthSystem = new HealthSystem(Constants.PLAYER_MAX_HEALTH);
        healthBar.Setup(_healthSystem);
        
        if (IsOwner) Initialize();
    }

    private void Initialize() {
        Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} joined");
        
        //TODO: Adjust Time.fixeddeltatime according to application framerate

        foreach (var go in goPovElements) {
            go.layer = LayerMask.NameToLayer("POV");
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _inCombat = false;
        
        timer = GameObject.FindWithTag("Timer");
        timerTxt = timer.GetComponent<TMP_Text>();
        if (IsHost) {
            timer.SetActive(false);
            startMatchButton = GameObject.FindWithTag("StartMatch");
            startMatchButton.GetComponent<Button>().onClick.AddListener(() => {
                if (StartMatch()) {
                    startMatchButton.SetActive(false);
                    _playerInputActions.Player.ToggleScoreboard.Enable();
                }
            });
        }

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
        //_playerInputActions.Player.Shoot.started += Shoot_started;
        //_playerInputActions.Player.Shoot.canceled += Shoot_canceled;

        //_playerInputActions.Player.Reload.performed += Reload_performed;

        _playerInputActions.Player.OpenMenu.performed += OpenMenu_performed;
        
        _healthSystem.OnDeath += OnDeath;
        
        //GameUI.Instance.OnInitialize(_healthSystem);
        GameUI.Instance.OnInitialize(_healthSystem);
        FollowPlayer.Instance.OnInitialize(transform);
        ShootManager.Instance.OnInitialize(transform);
        //FollowPlayer.Instance.SetPlayerPos();

        // Make local cinemachine vcam follow this GameObject
        _cinemachineVirtualCamera = GameObject.FindGameObjectWithTag("VirtualCamera");
        _cinemachineVirtualCamera.GetComponent<CinemachineVirtualCamera>().Follow =
            _cinemachineCameraTarget.transform;
    }

    private void OnDeath(object sender, EventArgs e) {
        Debug.Log("OnDeath()");
        if (_firstLife) {
            _firstLife = false;
            transform.position = GetComponent<TeamManager>().GetLocalSpawnPos();
            _healthSystem.Heal(Constants.PLAYER_MAX_HEALTH);
            return;
        }

        Debug.Log("Tot!");
        DeathProcess();
        //NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>().DeathServerRpc();
    }

    private void DeathProcess() {
        ShootManager.Instance.OnDeath();
        GameUI.Instance.OnDeath();
        transform.position = MatchManager.Instance.GetDeathZonePosition();
    }
    
    

    private void LateUpdate() {
        if (IsOwner) PlayerRotation();
    }

    private void Update() {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.Z)) {
            //Debug.Log("Cranking shit up");
            //Time.fixedDeltaTime = 0.005f;
        }

        if (Input.GetKeyDown(KeyCode.W)) {
            //transform.position += Vector3.forward * 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            //Debug.Log("Defaulting");
            //Time.fixedDeltaTime = 0.035f;
        }
    }

    private void FixedUpdate() {
        if (!IsOwner) return;

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
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        _isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private bool StartMatch() {
        
        if (GetComponentInParent<TeamManager>().GetNumberOfClientsInTeams() > NetworkManager.Singleton.ConnectedClientsIds.Count) {
            Debug.Log("Player left the game before start! This is not handled yet!");
            return false;
        }

        // TODO: This is not actually checking if everyone is in a team, can be bypassed by disconnecting and connecting again
        // TODO: Change this when going to big test, is only meant for closely observed testing
        if (GetComponentInParent<TeamManager>().GetNumberOfClientsInTeams() < NetworkManager.Singleton.ConnectedClientsIds.Count) {
            Debug.Log("Not every player selected a team!");
            return false;
        }
        GetComponent<TeamManager>().OnMatchStarted();
        
        MatchManager.Instance.StartMatch();
        StartMatchClientRpc();
        timer.SetActive(true);
        StartCoroutine(TimerCountdown());
        
        //TODO: Players need to spawn with their whole team at a spawn point
        //transform.position = new Vector3(58,35,45);
        return true;
    }

    [ClientRpc]
    private void StartMatchClientRpc() {
        if (IsHost) return;
        Debug.Log(GetComponent<TeamManager>().GetLocalTeamID());
        MatchManager.Instance.StartMatch();
        timer = GameObject.FindWithTag("Timer");
        timerTxt = timer.GetComponent<TMP_Text>();
        StartCoroutine(TimerCountdown());
    }

    private void EndMatch() {
        _playerInputActions.Player.Disable();
        MatchManager.Instance.EndMatch();
    }

    IEnumerator TimerCountdown() {

        _secondsPassed += 1;
        
        if (_secondsPassed == Constants.ZEAL_YELLOW_SPAWN_TIMESTAMP) {
            // Host spawns yellow zeal in world
            if (IsHost && IsOwner) SpawnZeal();
            Debug.Log("Yellow Zeal has spawned!");
        }
        
        if (_secondsPassed == Constants.ZEAL_RED_SPAWN_TIMESTAMP) {
            // Host spawns read zeal in world
            if (IsHost && IsOwner) SpawnZeal(true);
            Debug.Log("Red Zeal has spawned!");
        }
        
        yield return new WaitForSeconds(1);
        if (_minutesRemaining > 0) {
            if (_secondsRemaining == 0) {
                _minutesRemaining -= 1;
                _secondsRemaining = 59;
            } else {
                _secondsRemaining -= 1;
            }
        } else {
            _secondsRemaining -= 1;
        }

        string secondsPrefix = "";
        if (_secondsRemaining < 10) secondsPrefix = "0";
        timerTxt.text = $"{_minutesRemaining}:{secondsPrefix}{_secondsRemaining}";

        if (_minutesRemaining == 0 && _secondsRemaining == 0) {
            EndMatch();
        } else {
            StartCoroutine(TimerCountdown());
        }
    }

    
    #region Zeal
    private void SpawnZeal(bool isRed = false, int randomNumber = -1) {
        Debug.Log($"SpawnZeal({isRed}, {randomNumber})");
        var prefab = isRed ? zealRedPrefab : zealYellowPrefab;
        
        if (randomNumber == -1) randomNumber = rng.NextInt(zealSpawnPoints.Count);
        
        var spawnpoint = zealSpawnPoints[randomNumber];
        zealSpawnPoints.RemoveAt(randomNumber);

        if (IsHost) {
            SpawnZealClientRpc(isRed, randomNumber, new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = ClientListExcept(OwnerClientId)}});
            Debug.Log($"Sending SpawnZealClientRpc({isRed}, {randomNumber})");
        }
        
        var zeal = Instantiate(prefab, spawnpoint.position, Quaternion.identity);
        zeal.GetComponent<ZealObject>().SetRpcs(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<PlayerRpcs>());
    }
    
    [ClientRpc]
    private void SpawnZealClientRpc(bool isRed, int randomNumber, ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        Debug.Log($"Retrieving SpawnZealClientRpc({isRed}, {randomNumber})");
        SpawnZeal(isRed, randomNumber);
    }
    #endregion
    
    #region Input Methods
    private void OpenMenu_performed(InputAction.CallbackContext obj) {
            // Close menu
        if (Cursor.visible) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; 
            _playerInputActions.Player.Shoot.Enable();
            // Open menu
        } else {
            _playerInputActions.Player.Shoot.Disable();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    private void Sprint_started(InputAction.CallbackContext context) {
        _isSprinting = true;
    }

    private void Sprint_canceled(InputAction.CallbackContext context) {
        _isSprinting = false;
    }

    private void ADS_started(InputAction.CallbackContext context) {
        //Debug.Log("ADS!");
        //_cinemachineVirtualCamera;
    }

    private void ADS_canceled(InputAction.CallbackContext context) {
        //Debug.Log("No ADS!");
    }

    private void Shoot_started(InputAction.CallbackContext context) {
        //Debug.Log("Shoot!");
        //if (!MatchManager.Instance.IsMatchRunning()) return;
        //ShootBullet();
    }

    private void Shoot_canceled(InputAction.CallbackContext context) {
        //Debug.Log("Stop Shooting!");
    }

    private void DropZeal() {
        var rpcs = GetComponent<PlayerRpcs>();
        if (!(rpcs.hasYellowZeal || rpcs.hasRedZeal)) {
            Debug.Log("You currently dont have a zeal!");
            return;
        }
        
        var isRed = rpcs.hasRedZeal;
        var zealType = isRed ? Constants.ZEAL_RED_GAMEOBJECT_TAG : Constants.ZEAL_YELLOW_GAMEOBJECT_TAG;
        var zeal = GameObject.FindGameObjectWithTag(zealType);
        var zealObject = zeal.GetComponent<ZealObject>();
        zealObject.DropZeal();
    }

    private void Reload_performed(InputAction.CallbackContext context) {
        //ReloadWeapon();
        
    }

    private void Movement_started(InputAction.CallbackContext context) {
    }

    private void Movement_performed(InputAction.CallbackContext context) {
    }

    private void Movement_canceled(InputAction.CallbackContext context) {
    }

    private void Jump_performed(InputAction.CallbackContext context) {
        if (!_isGrounded || _isJumping) return;
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        StartCoroutine(JumpCooldown());
    }

    private void Look_performed(InputAction.CallbackContext context) {
        //PlayerRotation();
    }
    #endregion

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

    #region trash
    /*
    private void ShootBullet() {
        var rot = _mainCamera.transform.rotation;
        var pos = _mainCamera.transform.position;
        if (!IsHost) {
            Transform bullet = Instantiate(bulletPrefab, pos, rot);
            //Transform bullet = Instantiate()
            bullet.GetComponent<Bullet>().bulletClientID = NetworkManager.Singleton.LocalClientId;
        }
        ShootBulletServerRpc(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
    }
    
    [ServerRpc]
    private void ShootBulletServerRpc(float xP, float yP, float zP, float xR, float yR, float zR, float wR, ServerRpcParams rpcParams = default) {
        var rot = new Quaternion(xR, yR, zR, wR);
        var pos = new Vector3(xP, yP, zP);
        var bulletClientID = rpcParams.Receive.SenderClientId;
        Transform bullet = Instantiate(bulletPrefab, pos, rot);
        
        bullet.GetComponent<Bullet>().bulletClientID = bulletClientID;
        
        ShootBulletClientRpc(bulletClientID, xP, yP, zP, xR, yR, zR, wR, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(bulletClientID)}});
        
    }

    [ClientRpc]
    private void ShootBulletClientRpc(ulong bulletClientID, float xP, float yP, float zP, float xR, float yR, float zR, float wR, ClientRpcParams rpcParams) {
        if (IsHost) return;
        var rot = new Quaternion(xR, yR, zR, wR);
        var pos = new Vector3(xP, yP, zP);
        Transform bullet = Instantiate(bulletPrefab, pos, rot);
        bullet.GetComponent<Bullet>().bulletClientID = bulletClientID;
    }

    private void ReloadWeapon() {
        Transform spawnedWeapon = Instantiate(weapon, weaponSpawn);
        ReloadWeaponServerRpc();
    }*/
    /*
    [ServerRpc]
    private void ReloadWeaponServerRpc(ServerRpcParams rpcParams = default) {
        //TODO: just spawn Weapon locally on every client, is nothing more than cosmetic
        //TODO: weapon holder just needs to know, what weapon he is holding for pattern, etc.
        /*TODO: Structure: Input --> Player holds weapon POV and for everyone else
                  - Client detects input for weapon swap process
                  - Client spawns his weapon locally via Instantiate();
                  - Client sends serverRPC
                    - Server tells clients to locally equip weapon to enemy that send the RPC
                  - Client knows what shooting pattern, ammo, animation, whatever... to use
         */
        /*
        ReloadWeaponClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
        Transform spawnedWeapon = Instantiate(weapon, weaponSpawn);
        
        //spawnedWeapon.GetComponent<NetworkObject>().Spawn();
        //spawnedWeapon.GetComponent<NetworkObject>().TrySetParent(transform);

    }

    [ClientRpc]
    private void ReloadWeaponClientRpc(ClientRpcParams rpcParams) {
        Transform spawnedWeapon = Instantiate(weapon, weaponSpawn);
    }
    */
    #endregion
        
    [ServerRpc]
    private void DieServerRpc() {
        GetComponent<NetworkObject>().Despawn();
    }

    [ServerRpc]
    private void CheckBulletServerRpc(ServerRpcParams rpcParams = default) {
        /*if (bulletClientID.Equals(rpcParams.Receive.SenderClientId.ToString())) {
            Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} was shot!");
            DieServerRpc();
        }*/
    }

    #region Damage
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Bullet")) return;
        if (IsOwner) return;
        
        //TODO: try to remove this line later
        if (other.GetComponent<Bullet>().bulletClientID == NetworkObject.OwnerClientId) return;
        if (other.GetComponent<Bullet>().bulletClientID != NetworkManager.Singleton.LocalClientId) return;
        
        // Damage Logic
        if (!IsHost) {
            _inCombat = true;
            if (_cOutOfCombatTimer != null) StopCoroutine(_cOutOfCombatTimer);
            _cOutOfCombatTimer = StartCoroutine(OutOfCombatTimer());
            _healthSystem.Damage(Constants.PLAYER_DAMAGE_PER_SHOT);
        }
        DamageServerRpc();
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageServerRpc(ServerRpcParams rpcParams = default) {
        _inCombat = true;
        if (_cOutOfCombatTimer != null) StopCoroutine(_cOutOfCombatTimer);
        _cOutOfCombatTimer = StartCoroutine(OutOfCombatTimer());
        _healthSystem.Damage(Constants.PLAYER_DAMAGE_PER_SHOT);
        DamageClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
    }
    
    [ClientRpc]
    private void DamageClientRpc(ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        _inCombat = true;
        if (_cOutOfCombatTimer != null) StopCoroutine(_cOutOfCombatTimer);
        _cOutOfCombatTimer = StartCoroutine(OutOfCombatTimer());
        _healthSystem.Damage(Constants.PLAYER_DAMAGE_PER_SHOT);
    }

    private void HealPlayer() {
        if (!IsHost) {
            _healthSystem.Heal(Constants.PLAYER_HEALTH_PER_TICK);
        }
        HealPlayerServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void HealPlayerServerRpc(ServerRpcParams rpcParams = default) {
        _healthSystem.Heal(Constants.PLAYER_HEALTH_PER_TICK);
        HealPlayerClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
    }
    
    [ClientRpc]
    private void HealPlayerClientRpc(ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        _healthSystem.Heal(Constants.PLAYER_HEALTH_PER_TICK);
    }

    IEnumerator OutOfCombatTimer() {
        yield return new WaitForSeconds(Constants.PLAYER_TIME_OUT_OF_COMBAT);
        _inCombat = false;
        StartCoroutine(RegenerateHealth());
    }

    IEnumerator RegenerateHealth() {
        Debug.Log("Starting Regen");
        while (!_inCombat && _healthSystem.GetHealth() < Constants.PLAYER_MAX_HEALTH) {
            HealPlayer();
            yield return new WaitForSeconds(Constants.PLAYER_TIME_PER_HEALTH_TICK);
        }
        Debug.Log("Stopping Regen");
    }
    
    #endregion
    
    #region Helpers
    
    // Helper function to create list of connected clients except the sender
    private List<ulong> ClientListExcept(ulong senderClientId) {
        var clientIdList = new List<ulong>();
        
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (clientId == senderClientId) continue;
            clientIdList.Add(clientId);
        }

        return clientIdList;
    }
    
    #endregion
    
}