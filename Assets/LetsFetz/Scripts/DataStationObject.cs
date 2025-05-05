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

    //private ulong[] _lastClientFromTeam;
    private List<ulong>[] _clientsFromTeams;

    private void Awake() {
        cCaptureDataStation = StartCoroutine(CaptureDataStation(0));
        StopCoroutine(cCaptureDataStation);
        gameObject.name = Constants.DATASTATION_GAMEOBJECT_NAME;
        
        //_lastClientFromTeam = new ulong[5];
        _clientsFromTeams = new List<ulong>[5];
        
        for (int i = 0; i < _clientsFromTeams.Length; i++) {
            _clientsFromTeams[i] = new List<ulong>();
        }
    }

    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
        _rpcs = teamManagerHere.GetComponent<PlayerRpcs>();
    }

    private void OnTriggerEnter(Collider other) {
        if (!NetworkManager.Singleton.IsHost || !other.CompareTag("Player")) return;
        var teamID = other.GetComponent<TeamManager>().GetLocalTeamID()-1;
        activePlayersFromTeams[teamID] += 1;
        
        _clientsFromTeams[teamID].Add(other.GetComponent<NetworkObject>().OwnerClientId);
        
        CheckCapture(true);
    }
    
    private void OnTriggerExit(Collider other) {
        if (!NetworkManager.Singleton.IsHost || !other.CompareTag("Player")) return;
        var teamID = other.GetComponent<TeamManager>().GetLocalTeamID()-1;
        activePlayersFromTeams[teamID] -= 1;
        
        _clientsFromTeams[teamID].Remove(other.GetComponent<NetworkObject>().OwnerClientId);
        
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
            }
            // Tied
            else if (activePlayersFromTeams[i] == majority) {
                winningTeamID = -1;
            }
        }

        if (winningTeamID > 0) {
            if (winningTeamID == capturedByTeamID) {
                //Debug.Log("Your team already owns the data station!");
                return;
            }
            cCaptureDataStation = StartCoroutine(CaptureDataStation(winningTeamID));
        } else {
            StopCoroutine(cCaptureDataStation);
            //Debug.Log("Capturing stopped!");
        }

        
        //if (winningTeamID == -1) Debug.Log($"No team or tied!");
        
    }
    
    IEnumerator CaptureDataStation(int teamID) {
        //Debug.Log("Capturing started!");
        yield return new WaitForSeconds(Constants.DATASTATION_CAPTURE_TIME);
        //Debug.Log($"Team {teamID} captured!");
        capturedByTeamID = teamID;
        _rpcs.CaptureDataStationServerRpc(capturedByTeamID.ToString());
        LoggingManager.Instance.LogEvent(
            NetworkManager.Singleton.ConnectedClients[_clientsFromTeams[teamID-1][0]],
            LoggingManager.LoggingType.CaptureDataStation);
        captured = true;
        StartCoroutine(ProgressTicking());
    }
    
    IEnumerator ProgressTicking() {
        while (MatchManager.Instance.IsMatchRunning()) {
            yield return new WaitForSeconds(Constants.DATASTATION_TIME_PER_TICK);
            if (!captured) break;
            //Debug.Log($"Adding {Constants.DATASTATION_PROGRESS_PER_TICK} Data Station progress to team {capturedByTeamID}");
            teamManager.AddProgressOnServer(capturedByTeamID, new Contracts.DataStation(), Constants.DATASTATION_PROGRESS_PER_TICK);
        }
    }
    
}
