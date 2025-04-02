using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MatchManager : MonoBehaviour {

    private bool isMatchRunning = false;
    
    public static MatchManager Instance { get; private set; }

    private string localName;
    private int localPoints;

    // Index 0: Team 1, Index 1: Team 2, ...
    private int[] pointsTeams;
    
    public Dictionary<ulong, string> playerNames;
    
    // Cached Reference to local TeamManager
    private TeamManager teamManager;
    
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
        
        playerNames = new Dictionary<ulong, string>();
        pointsTeams = new int[5];
    }
    
    // Setter for local TeamManager
    // Gets called by first TeamManager, that enters the scene, which is the local player
    // Cannot get called earlier, would disrupt things
    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
    }
    
    public void SetLocalName(string playerName) {
        localName = playerName;
    }
    
    public string GetLocalName() {
        return localName;
    }
    
    public void AddPlayerWithName(ulong clientId, string playerName) {
        playerNames.Add(clientId, playerName);
    }
    
    public int AddPoints(int points, int teamID) {
        return pointsTeams[teamID] += points;
    }

    public void AddLocalPoints(int points) {
        localPoints += points;
    }
    
    public bool IsMatchRunning() {
        return isMatchRunning;
    }
    
    public void StartMatch() {
        isMatchRunning = true;
        GameUI.Instance.OnMatchStarted();
        localPoints = 0;
    }

    public void EndMatch() {
        isMatchRunning = false;
    }
}
