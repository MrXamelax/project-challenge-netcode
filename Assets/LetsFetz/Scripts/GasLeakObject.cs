using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GasLeakObject : NetworkBehaviour, IInteractable {
    private int capturedByTeamID = -1;
    private int refinerLevel = 0;

    [SerializeField] private GameObject goRefiner;
    [SerializeField] private GameObject goLeak;

    [SerializeField] private GameObject goMinimapIconFree;
    [SerializeField] private GameObject goMinimapIconCaptured;

    //private Coroutine cProgressTicking;
    
    private bool isRefining = false;

    private TeamManager teamManager;

    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
        //cProgressTicking = StartCoroutine(ProgressTicking());
        //StopCoroutine(cProgressTicking);
    }

    public void Interact() {
        
        // Refiner is already owned by my team
        if (isRefining && capturedByTeamID == teamManager.GetLocalTeamID()) {
            Debug.Log("Refiner is already under your control!");
            return;
        }

        var steal = capturedByTeamID != teamManager.GetLocalTeamID() /*&& capturedByTeamID != -1*/;
        Debug.Log($"Refiner is being {(steal ? "stolen" : "captured")}");
        
        if (!IsHost) {
            //isRefining = true;
            //goLeak.SetActive(false);
            //goRefiner.SetActive(true);
            //goMinimapIconFree.SetActive(false);
            //goMinimapIconCaptured.SetActive(true);
            //capturedByTeamID = teamManager.GetLocalTeamID();
            Capture(teamManager.GetLocalTeamID(), steal);
            if (!steal) StartCoroutine(ProgressLeveling());
        }
        
        InteractServerRpc(teamManager.GetLocalTeamID());
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(int teamID, ServerRpcParams rpcParams = default) {
        
        // Sent from my team?
        var steal = teamID != teamManager.GetLocalTeamID();
        
        if (capturedByTeamID == -1) steal = false;
        
        if (!isRefining) {
            StartCoroutine(ProgressLeveling());
            StartCoroutine(ProgressTicking());
        }
        
        Capture(teamID, steal);
        
        InteractClientRpc(teamID, new ClientRpcParams { Send = new ClientRpcSendParams {TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
    }

    [ClientRpc]
    private void InteractClientRpc(int teamID, ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        
        var steal = teamID != teamManager.GetLocalTeamID();
        
        // Initially capturing the the gas leak is not stealing it
        if (capturedByTeamID == -1) steal = false;
        
        if (!isRefining) StartCoroutine(ProgressLeveling());
        Capture(teamID, steal);
        //isRefining = true;
        //goLeak.SetActive(false);
        //goRefiner.SetActive(true);
        //capturedByTeamID = teamID;
        //if (!steal) StartCoroutine(ProgressLeveling());
    }

    private void Capture(int teamID, bool steal) {
        
        Debug.Log(steal ? $"Team {teamID} stole a refiner from team {capturedByTeamID}!" : $"Team {teamID} captured a refiner!");

        // Initial capture
        if (capturedByTeamID == -1 && steal) {
            Debug.Log("Initial capture by other team");
            isRefining = true;
            goLeak.SetActive(false);
            goRefiner.SetActive(true);
            
            capturedByTeamID = teamID;
            return;
        }
        
        if (!steal) {
            isRefining = true;
            goLeak.SetActive(false);
            goRefiner.SetActive(true);
            goMinimapIconFree.SetActive(false);
            goMinimapIconCaptured.SetActive(true);
        } else {
            goMinimapIconFree.SetActive(true);
            goMinimapIconCaptured.SetActive(false);
        }
        
        capturedByTeamID = teamID;
        
    }

    IEnumerator ProgressTicking() {
        while (true) {
            yield return new WaitForSeconds(Constants.GASLEAK_TIME_PER_TICK);
            if (!isRefining) break;
            Debug.Log($"Adding {Constants.GASLEAK_PROGRESS_PER_TICK_BY_LEVEL[refinerLevel]} Gas Leak progress to team {capturedByTeamID}");
            teamManager.AddProgressOnServer(capturedByTeamID, new Contracts.GasLeak(),
                Constants.GASLEAK_PROGRESS_PER_TICK_BY_LEVEL[refinerLevel]);
        }
    }

    IEnumerator ProgressLeveling() {
        yield return new WaitForSeconds(Constants.GASLEAK_TIME_TO_LEVELUP[refinerLevel]);
        Debug.Log($"Refiner leveled up to level {refinerLevel + 1}!");
        if (refinerLevel < Constants.GASLEAK_TIME_TO_LEVELUP.Length - 1) {
            refinerLevel += 1;
            StartCoroutine(ProgressLeveling());
        }
    }

    private List<ulong> ClientListExcept(ulong senderClientID) {
        var clientIDList = new List<ulong>();

        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds) {
            if (clientID == senderClientID) continue;
            clientIDList.Add(clientID);
        }

        return clientIDList;
    }
}