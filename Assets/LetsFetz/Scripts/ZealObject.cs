using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ZealObject : MonoBehaviour, IInteractable {
    private int capturedByTeamID = -1;
    
    [SerializeField] private bool _isRed;

    private TeamManager teamManager;
    
    private PlayerRpcs _rpcs;

    public void SetRpcs(PlayerRpcs rpcs) {
        _rpcs = rpcs;
    }

    private void Start() {
        teamManager = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>();
    }

    public void Interact() {
        if (_rpcs.hasYellowZeal || _rpcs.hasRedZeal) {
            SwapZeal();
            return;
        }
        PickUpZeal();
    }

    private void PickUpZeal() {
        ZealState(false, teamManager.GetLocalTeamID());
        if (NetworkManager.Singleton.IsHost) StartCoroutine(ProgressTicking());
        _rpcs.PickUpZealServerRpc(_isRed, teamManager.GetLocalTeamID());
        /*
         * + deactivate gameobject for everyone
         * + activate highlighted minimap and map icon on player for everyone
         * + deactivate unhighlighted minimap and map icon on zeal object for everyone
         * - start ticking zeal points for team
         * - host client has to know, which team has the zeal
         */
    }
    
    public void DropZeal() {
        transform.position = NetworkManager.Singleton.LocalClient.PlayerObject.transform.position;
        ZealState(true, -1);
        var pos = transform.position;
        _rpcs.DropZealServerRpc(_isRed, pos.x, pos.y, pos.z);
        /*
         * - called when you have the zeal and dps threshold is met
         * + deactivate highlighted minimap and map on player for everyone
         * + unhighlighted minimap and map icon on zeal object for everyone
         * - stop ticking zeal points for team
         * - host client knows that team no longer has zeal
         */
    }

    private void SwapZeal() {
        PickUpZeal();
        var zealType = !_isRed ? Constants.ZEAL_RED_GAMEOBJECT_TAG : Constants.ZEAL_YELLOW_GAMEOBJECT_TAG;
        var zeal = GameObject.FindGameObjectWithTag(zealType);
        zeal.GetComponent<ZealObject>().DropZeal();
    }

    public void ZealState(bool active, int teamID) {
        if (!active) Debug.Log((_isRed ? "Red " : "Yellow ") + $"Zeal has been collected by Team {teamID}");
        capturedByTeamID = teamID;
        
        var childObjects = GetComponentsInChildren<Transform>(true).ToList();
        childObjects.RemoveAt(0);
        foreach (var tf in childObjects) {
            tf.gameObject.SetActive(active);
        }
    }

    public IEnumerator ProgressTicking() {
        while (true) {
            yield return new WaitForSeconds(Constants.ZEAL_TIME_PER_TICK);
            if (capturedByTeamID == -1) break;
            var progress = _isRed ? Constants.ZEAL_RED_PROGRESS_PER_TICK : Constants.ZEAL_YELLOW_PROGRESS_PER_TICK;
            Debug.Log($"Adding {progress} Zeal progress to team {capturedByTeamID}!");
            teamManager.AddProgressOnServer(capturedByTeamID, new Contracts.Zeal(), progress);
        }
        Debug.Log("Zeal dropped");
    }

}
