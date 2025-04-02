using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootManager : MonoBehaviour {
    
    [SerializeField] private Transform bulletPrefab;
    
    private GameObject _mainCamera;

    private PlayerRpcs _rpcs;
    
    private Coroutine _shootingCoroutine;
    
    public static ShootManager Instance { get; private set; }
    
    private NewPlayerInputActions _playerInputActions;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        _mainCamera = GameObject.FindWithTag("MainCamera");
        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Shoot.started += Shoot_started;
        _playerInputActions.Player.Shoot.canceled += Shoot_canceled;
    }
    
    public void OnInitialize(Transform tf) {
        _rpcs = tf.gameObject.GetComponent<PlayerRpcs>();
    }

    private void Shoot_started(InputAction.CallbackContext obj) {
        if (!MatchManager.Instance.IsMatchRunning()) return;
        _shootingCoroutine = StartCoroutine(ShootingCycle());
        
    }
    
    private void Shoot_canceled(InputAction.CallbackContext obj) {
        if (!MatchManager.Instance.IsMatchRunning()) return;
        StopShooting();
    }
    
    private void StopShooting() {
        StopCoroutine(_shootingCoroutine);
    }
    
    
    IEnumerator ShootingCycle() {
        var rot = _mainCamera.transform.rotation;
        var pos = _mainCamera.transform.position;
        
        Transform bullet = Instantiate(bulletPrefab, pos, rot);
        bullet.GetComponent<Bullet>().bulletClientID = NetworkManager.Singleton.LocalClientId;
        
        _rpcs.StartShootingServerRpc(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        
        yield return new WaitForSeconds(0.1f);
        _shootingCoroutine = StartCoroutine(ShootingCycle());
    }
    
}
