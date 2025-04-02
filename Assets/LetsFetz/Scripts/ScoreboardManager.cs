using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// In charge of updating the scoreboard ui
public class ScoreboardManager : MonoBehaviour {
    
    // Singleton Pattern for this class
    public static ScoreboardManager Instance { get; private set; }
    
    // Definitely not a smooth solution, but lets see if it works --> it does! for now..
    // CAREFUL! index 0 of team all TMP_Text arrays is reserved for $"{rank} Team {team}"
    [SerializeField] private GameObject[] teams;
    [SerializeField] private TMP_Text[] team1;
    [SerializeField] private TMP_Text[] team2;
    [SerializeField] private TMP_Text[] team3;
    [SerializeField] private TMP_Text[] team4;
    [SerializeField] private TMP_Text[] team5;
    
    // Visually updating points for teams
    [SerializeField] private TMP_Text[] pointsTexts;

    // Simple Concatenation as list of TMP_Text arrays
    private List<TMP_Text[]> teamsList;
    
    // Cached Reference to local TeamManager
    private TeamManager teamManager;

    // Singleton Pattern for this class
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    
    // Setter for local TeamManager
    // Gets called by first TeamManager, that enters the scene, which is the local player
    // Cannot get called earlier, would disrupt things
    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
        teamsList = new List<TMP_Text[]>{team1, team2, team3, team4, team5};
    }
    
    // Before game start
    // Update player text entries in scoreboard ui 
    public void UpdateDisplayScoreboard() {
        for (var j = 0; j < teamsList.Count; j++) {
            teamsList[j][0].text = $"1 Team {j+1}";
            
            for (var i = 1; i < teamsList[j].Length; i++) {
                if (teamManager.teams[j].Count < i) teamsList[j][i].text = "";
                else teamsList[j][i].text = MatchManager.Instance.playerNames[teamManager.teams[j][i-1]];
            }
        }
    }
    
    // While game running
    // Update points text entries in scoreboard ui 
    public void UpdateDisplayPoints(int points, int teamID) {
        pointsTexts[teamID].text = MatchManager.Instance.AddPoints(points, teamID).ToString();
        // TODO: sort scoreboard entries in descending order
        
    }

    private void SortTeams() {
        var max = 0;
        foreach (var points in pointsTexts) {
            if (int.Parse(points.text) > max) max = int.Parse(points.text);
            
        }
    }
    
}
