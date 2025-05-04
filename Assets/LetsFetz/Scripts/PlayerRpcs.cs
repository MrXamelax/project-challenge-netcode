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

    public bool hasYellowZeal;
    public bool hasRedZeal;

    public override void OnNetworkSpawn() {
        containerZealIconYellow.transform.SetParent(null);
        containerZealIconRed.transform.SetParent(null);

        containerZealIconYellow.transform.rotation = Quaternion.identity;
        containerZealIconRed.transform.rotation = Quaternion.identity;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LogEventServerRpc(LoggingManager.LoggingType type, ServerRpcParams rpcParams = default) {
        LoggingManager.Instance.LogEvent(NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId], type);
    }

    #region Shooting
    
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
    
    #endregion

    #region Zeal
    
    [ServerRpc(RequireOwnership = false)]
    public void PickUpZealServerRpc(bool isRed, int teamID, ServerRpcParams rpcParams = default) {
        LoggingManager.Instance.LogEvent(
            NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId],
            isRed ? LoggingManager.LoggingType.CaptureRedZeal : LoggingManager.LoggingType.CaptureYellowZeal
        );
        PickUpZealClientRpc(isRed, rpcParams.Receive.SenderClientId, teamID);
    }

    [ClientRpc]
    private void PickUpZealClientRpc(bool isRed, ulong clientID, int teamID) {
        if (NetworkManager.Singleton.LocalClientId == clientID) {
            if (isRed) hasRedZeal = true;
            else hasYellowZeal = true;
            return;
        }
        
        if (isRed) zealIconRed.SetActive(true);
        else zealIconYellow.SetActive(true);
        
        var zealType = isRed ? Constants.ZEAL_RED_GAMEOBJECT_TAG : Constants.ZEAL_YELLOW_GAMEOBJECT_TAG;
        var zeal = GameObject.FindGameObjectWithTag(zealType);
        var zealObject = zeal.GetComponent<ZealObject>();
        zealObject.ZealState(false, teamID);

        if (!IsHost) return;
        // Start ticking zeal progress
        StartCoroutine(zealObject.ProgressTicking());
    }

    [ServerRpc]
    public void DropZealServerRpc(bool isRed, float xP, float yP, float zP, ServerRpcParams rpcParams = default) {
        LoggingManager.Instance.LogEvent(
            NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId],
            isRed ? LoggingManager.LoggingType.DropRedZeal : LoggingManager.LoggingType.DropYellowZeal
        );
        DropZealClientRpc(isRed, rpcParams.Receive.SenderClientId, xP, yP, zP);
    }
    
    [ClientRpc]
    private void DropZealClientRpc(bool isRed, ulong clientID, float xP, float yP, float zP) {
        if (NetworkManager.Singleton.LocalClientId == clientID) {
            if (isRed) hasRedZeal = false;
            else hasYellowZeal = false;
            return;
        }
        
        var pos = new Vector3(xP, yP, zP);
        
        if (isRed) zealIconRed.SetActive(false);
        else zealIconYellow.SetActive(false);
        
        var zealType = isRed ? Constants.ZEAL_RED_GAMEOBJECT_TAG : Constants.ZEAL_YELLOW_GAMEOBJECT_TAG;
        var zeal = GameObject.FindGameObjectWithTag(zealType);
        zeal.transform.position = pos;
        zeal.GetComponent<ZealObject>().ZealState(true, -1);
    }
    
    #endregion

    #region Datastation

    [ServerRpc]
    public void CaptureDataStationServerRpc(string teamID) {
        CaptureDataStationClientRpc(teamID);
    }
    
    [ClientRpc] 
    private void CaptureDataStationClientRpc(string teamID) {
        GameUI.Instance.UpdateDisplayDatastation(teamID);
    }

    #endregion
    
}
