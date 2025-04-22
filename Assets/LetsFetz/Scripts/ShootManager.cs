using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootManager : MonoBehaviour {
    
    [SerializeField] private Transform bulletPrefab;

    [SerializeField] private GameObject reloadBar;
    
    private GameObject _mainCamera;

    private PlayerRpcs _rpcs;
    
    private Coroutine _shootingCoroutine;

    private int _ammoCurrent;
    
    private bool isReloading;
    
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
        _playerInputActions.Player.Reload.started += Reload_started;
        _ammoCurrent = Constants.PLAYER_MAX_AMMO;
        isReloading = false;
    }
    
    public void OnInitialize(Transform tf) {
        _rpcs = tf.gameObject.GetComponent<PlayerRpcs>();
    }

    private void Shoot_started(InputAction.CallbackContext obj) {
        if (!MatchManager.Instance.IsMatchRunning() || isReloading) return;
        if (_ammoCurrent > 0) _shootingCoroutine = StartCoroutine(ShootingCycle());
        else Debug.Log("No Ammo!");
    }
    
    private void Shoot_canceled(InputAction.CallbackContext obj) {
        if (!MatchManager.Instance.IsMatchRunning() || isReloading) return;
        StopShooting();
    }
    
    private void StopShooting() {
        if (_shootingCoroutine != null) StopCoroutine(_shootingCoroutine);
    }

    private void Reload_started(InputAction.CallbackContext obj) {
        if (!MatchManager.Instance.IsMatchRunning()) return;
        isReloading = true;
        if (_shootingCoroutine != null) StopCoroutine(_shootingCoroutine);
        StartCoroutine(ReloadAnimationUI());
    }

    IEnumerator ReloadAnimationUI() {
        reloadBar.SetActive(true);
        for (float i = 0; i <= Constants.PLAYER_RELOAD_TIME; i += Time.deltaTime) {
            yield return new WaitForSeconds(Time.deltaTime);
            GameUI.Instance.UpdateDisplayReloadBar(i/Constants.PLAYER_RELOAD_TIME);
        }

        _ammoCurrent = Constants.PLAYER_MAX_AMMO;
        reloadBar.SetActive(false);
        GameUI.Instance.UpdateDisplayAmmo(_ammoCurrent);
        isReloading = false;
    }
    
    IEnumerator ShootingCycle() {
        _ammoCurrent -= 1;
        GameUI.Instance.UpdateDisplayAmmo(_ammoCurrent);
        var rot = _mainCamera.transform.rotation;
        var pos = _mainCamera.transform.position;
        
        Transform bullet = Instantiate(bulletPrefab, pos, rot);
        bullet.GetComponent<Bullet>().bulletClientID = NetworkManager.Singleton.LocalClientId;
        
        _rpcs.StartShootingServerRpc(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        
        yield return new WaitForSeconds(Constants.PLAYER_SHOOT_INTERVAL);
        if (_ammoCurrent > 0) _shootingCoroutine = StartCoroutine(ShootingCycle());
    }
    
}
