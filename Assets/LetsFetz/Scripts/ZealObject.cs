using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ZealObject : MonoBehaviour, IInteractable {

    [SerializeField] private bool isRed;

    private PlayerRpcs _rpcs;

    public void SetRpcs(PlayerRpcs rpcs) {
        _rpcs = rpcs;
    }

    public void Interact() {
        CollectZeal();
        Debug.Log((isRed ? "Red " : "Yellow ") + "Zeal has been collected by Team");
    }

    private void CollectZeal() {
        //TODO: Scheiße digga das is ja immer noch nich fertig ahhh
        ZealState(false);
        _rpcs.PickUpZealServerRpc(isRed);
        /*
         * - deactivate gameobject for everyone
         * + activate highlighted minimap and map icon on player for everyone
         * - deactivate unhighlighted minimap and map icon on zeal object for everyone
         * - start ticking zeal points for team
         * - local client has to know, it has the zeal
         */
    }
    
    private void DropZeal() {
        ZealState(true);
        /*
         * called when you have the zeal and dps threshold is met
         * deactivate highlighted minimap and map on player for everyone
         * unhighlighted minimap and map icon on zeal object for everyone
         * stop ticking zeal points for team
         * local client no longer has zeal
         */
    }
    
    public void ZealState(bool active) {
        var childObjects = GetComponentsInChildren<Transform>().ToList();
        childObjects.RemoveAt(0);
        foreach (var tf in childObjects) {
            tf.gameObject.SetActive(active);
        }
    }
    
}
