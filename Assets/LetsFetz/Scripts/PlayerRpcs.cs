using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRpcs : NetworkBehaviour {
    
    [SerializeField] private Transform bulletPrefab;

    [SerializeField] private GameObject zealIconYellow;
    [SerializeField] private GameObject zealIconRed;
    
    [SerializeField] private GameObject containerZealIconYellow;
    [SerializeField] private GameObject containerZealIconRed;

    private bool hasYellowZeal;
    private bool hasRedZeal;

    public override void OnNetworkSpawn() {
        containerZealIconYellow.transform.SetParent(null);
        containerZealIconRed.transform.SetParent(null);

        containerZealIconYellow.transform.rotation = Quaternion.identity;
        containerZealIconRed.transform.rotation = Quaternion.identity;
    }

    [ServerRpc]
    public void StartShootingServerRpc(float xP, float yP, float zP, float xR, float yR, float zR, float wR, ServerRpcParams rpcParams = default) {
        StartShootingClientRpc(xP, yP, zP, xR, yR, zR, wR, rpcParams.Receive.SenderClientId);
    }
    
    [ClientRpc]
    private void StartShootingClientRpc(float xP, float yP, float zP, float xR, float yR, float zR, float wR, ulong clientID) {
        if (NetworkManager.Singleton.LocalClientId == clientID) return;
        
        var rot = new Quaternion(xR, yR, zR, wR);
        var pos = new Vector3(xP, yP, zP);
        
        Transform bullet = Instantiate(bulletPrefab, pos, rot);
        bullet.GetComponent<Bullet>().bulletClientID = clientID;
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpZealServerRpc(bool isRed, ServerRpcParams rpcParams = default) {
        PickUpZealClientRpc(isRed, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void PickUpZealClientRpc(bool isRed, ulong clientID) {
        if (NetworkManager.Singleton.LocalClientId == clientID) return;
        
        if (isRed) zealIconRed.SetActive(true);
        else zealIconYellow.SetActive(true);
        
        var zealType = isRed ? Constants.ZEAL_RED_GAMEOBJECT_NAME : Constants.ZEAL_YELLOW_GAMEOBJECT_NAME;
        var zeal = GameObject.Find(zealType);
        zeal = GameObject.FindWithTag("");
        zeal.GetComponent<ZealObject>().ZealState(false);
    }
    
    
}
