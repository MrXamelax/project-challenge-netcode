using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DataStationObject : MonoBehaviour {

    private int capturedByTeamID = -1;
    private int[] activePlayersFromTeams = {0,0,0,0,0};

    private bool captured = false;

    private TeamManager teamManager;
    
    private Coroutine cCaptureDataStation;

    private PlayerRpcs _rpcs;

    private void Awake() {
        cCaptureDataStation = StartCoroutine(CaptureDataStation(0));
        StopCoroutine(cCaptureDataStation);
        gameObject.name = Constants.DATASTATION_GAMEOBJECT_NAME;
    }

    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
        _rpcs = teamManagerHere.GetComponent<PlayerRpcs>();
    }

    private void OnTriggerEnter(Collider other) {
        if (!NetworkManager.Singleton.IsHost || !other.CompareTag("Player")) return;
        activePlayersFromTeams[other.GetComponent<TeamManager>().GetLocalTeamID()-1] += 1;
        CheckCapture(true);
    }
    
    private void OnTriggerExit(Collider other) {
        if (!NetworkManager.Singleton.IsHost || !other.CompareTag("Player")) return;
        activePlayersFromTeams[other.GetComponent<TeamManager>().GetLocalTeamID()-1] -= 1;
        CheckCapture(false);
    }

    private void CheckCapture(bool enter) {
        var majority = 0;
        var winningTeamID = 0;
        for (var i = 0; i < activePlayersFromTeams.Length; i++) {
            
            // A team has taken majority
            if (activePlayersFromTeams[i] > majority) {
                majority = activePlayersFromTeams[i];
                winningTeamID = i+1;
                //cCaptureDataStation = StartCoroutine(CaptureDataStation(i+1));
                //capturedByTeamID = i+1;
                //captured = true;
                //StartCoroutine(ProgressTicking());
            }
            // Tied
            else if (activePlayersFromTeams[i] == majority) {
                winningTeamID = -1;
                //StopCoroutine(cCaptureDataStation);
                //Debug.Log("Capturing stopped!");
                //capturedByTeamID = -1;
                //captured = false;
                //StopCoroutine(ProgressTicking());
            }
        }

        if (winningTeamID > 0) {
            cCaptureDataStation = StartCoroutine(CaptureDataStation(winningTeamID));
        } else {
            StopCoroutine(cCaptureDataStation);
            Debug.Log("Capturing stopped!");
        }

        // DEBUG
        //if (capturedByTeamID > 0) Debug.Log($"Captured by team {capturedByTeamID}!");
        //else Debug.Log($"Captured by no team or tied!");
        if (winningTeamID == -1) Debug.Log($"No team or tied!");
        
    }
    
    IEnumerator CaptureDataStation(int teamID) {
        Debug.Log("Capturing started!");
        yield return new WaitForSeconds(Constants.DATASTATION_CAPTURE_TIME);
        Debug.Log($"Team {teamID} captured!");
        capturedByTeamID = teamID;
        _rpcs.CaptureDataStationServerRpc(capturedByTeamID.ToString());
        captured = true;
        StartCoroutine(ProgressTicking());
    }
    
    //TODO: Beep Boop we send progress on contract to according clients and they locally do their thing
    IEnumerator ProgressTicking() {
        while (MatchManager.Instance.IsMatchRunning()) {
            yield return new WaitForSeconds(Constants.DATASTATION_TIME_PER_TICK);
            if (!captured) break;
            Debug.Log($"Adding {Constants.DATASTATION_PROGRESS_PER_TICK} Data Station progress to team {capturedByTeamID}");
            //teamManager.AddProgressToContract(capturedByTeamID, new Contracts.DataStation());
            teamManager.AddProgressOnServer(capturedByTeamID, new Contracts.DataStation(), Constants.DATASTATION_PROGRESS_PER_TICK);
            
            //var teamManager = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>();
            
            //ContractManager.Instance.GetContractList().Find(x =>
            //x is Contracts.DataStation).AddProgress(Constants.DATASTATION_PROGRESS_PER_TICK);
        }
    }
    
}
