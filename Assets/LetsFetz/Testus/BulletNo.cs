using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class BulletNo : NetworkBehaviour {

    [SerializeField] private float bulletSpeed = 20.0f;

    private void Awake() {
        //throw new NotImplementedException();
    }

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public override void OnNetworkSpawn() {
        //if (IsOwner) 
        StartCoroutine(BulletTimer());
    }

    private void FixedUpdate() {
        //if (!IsOwner) return;
        transform.position += transform.forward * (bulletSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other) {
        /*if (!IsOwner) return;
        if (other.gameObject.TryGetComponent(out NetworkObject no)) {
            if (no.IsOwner) return;
        }
        Debug.Log("Destroy now");*/
        if (other.CompareTag("Player")) return;
        DestroyBulletServerRpc();
    }

    IEnumerator BulletTimer() {
        yield return new WaitForSeconds(5.0f);
        DestroyBulletServerRpc();
    }

    [ServerRpc]
    private void DestroyBulletServerRpc() {
        //GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
    
    
}