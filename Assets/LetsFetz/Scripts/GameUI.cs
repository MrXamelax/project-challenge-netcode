using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class GameUI : MonoBehaviour {
    
    public static GameUI Instance { get; private set; }
    
    [SerializeField] private GameObject teamsPanel;
    [SerializeField] private GameObject scoreboard;
    [SerializeField] private GameObject minimap;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private TMP_Text[] progressTexts;
    [SerializeField] private TMP_Text[] pointsTexts;
    
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text[] teamTexts;

    [SerializeField] private GameObject healthBar;
    [SerializeField] private Transform healthBarFill;

    [SerializeField] private GameObject ammoCount;
    [SerializeField] private TMP_Text ammoCountText;
    
    [SerializeField] private Transform reloadBarFill;
    
    private NewPlayerInputActions _playerInputActions;

    private TeamManager teamManager;
    
    private void Awake() {
        
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
        
        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();

        debugText.text = "Team: 0";
    }

    private void Start() {
        //_minimap.SetActive(false);
    }

    private void InitializeDisplayProgress() {
        var contracts = ContractManager.Instance.GetTeamContracts()[0];
        //Debug.Log("Size: " + contracts.Count);
        for (int i = 0; i < pointsTexts.Length; i++) {
            //Debug.Log("InitializeDisplayProgress() " + i);
            //_progressTexts[i].text = contracts.ToArray()[i].GetPointsPerLevel()[0].ToString();
            pointsTexts[i].text = $"{contracts[i].GetPointsPerLevel()[0].ToString()}";
            progressTexts[i].text = $"0 / {contracts[i].GetProgressToNextLevel()}";
            //Debug.Log(i + ": " + contracts[i].GetPointsPerLevel().Length);
            //Debug.Log(i + ": " + contracts[i]);
        }
    }
    
    public void UpdateDisplayProgress() {
        
        // gets called when progress is added to any contract
        // does this need be rpc'd when team progress thingy?
        var contracts = ContractManager.Instance.GetTeamContracts()[teamManager.GetLocalTeamID()-1];
        for (int i = 0; i < progressTexts.Length; i++) {
            progressTexts[i].text = $"{contracts[i].GetProgressNeeded() - contracts[i].GetProgressToNextLevel()} / {contracts[i].GetProgressNeeded()}";
        }
    }

    public void UpdateDisplayPoints() {
        var contracts = ContractManager.Instance.GetTeamContracts()[teamManager.GetLocalTeamID()-1];
        for (int i = 0; i < pointsTexts.Length; i++) {
            pointsTexts[i].text = $"{contracts[i].GetPointsPerLevelCurrent()}";
        }
    }

    public void OnInitialize(HealthSystem healthSystem) {
        _playerInputActions.Player.ToggleScoreboard.started += ToggleScoreboard_started;
        _playerInputActions.Player.ToggleScoreboard.canceled += ToggleScoreboard_canceled;
        _playerInputActions.Player.ToggleTeams.started += ToggleTeamsStarted;
        EnableChildren(scoreboard);
        scoreboard.SetActive(false);
        minimap.SetActive(true);
        healthBar.SetActive(true);
        healthBar.GetComponent<HealthBar>().Setup(healthSystem);
        
        healthSystem.OnHealthChanged += (sender, e) => {
            healthBarFill.localScale = new Vector3(healthSystem.GetHealthPercent() * 1, 1);
        };
        
        ammoCount.SetActive(true);
        ammoCountText.text = $"{Constants.PLAYER_MAX_AMMO} / {Constants.PLAYER_MAX_AMMO}";
        InitializeDisplayProgress();
    }
    
    public void UpdateDisplayAmmo(int ammoCurrent) {
        ammoCountText.text = $"{ammoCurrent} / {Constants.PLAYER_MAX_AMMO}";
    }

    public void UpdateDisplayReloadBar(float percent) {
        reloadBarFill.localScale = new Vector3(percent, 1);
    }

    private void ToggleTeamsStarted(InputAction.CallbackContext obj) {
        teamsPanel.SetActive(!teamsPanel.activeSelf);
    }
    
    private void ToggleScoreboard_started(InputAction.CallbackContext obj) {
        minimap.SetActive(false);
        progressPanel.SetActive(false);
        scoreboard.SetActive(true);
    }
    
    private void ToggleScoreboard_canceled(InputAction.CallbackContext obj) {
        scoreboard.SetActive(false);
        minimap.SetActive(true);
        progressPanel.SetActive(true);
    }

    // We don't want to allow picking a team while the match is already running
    public void OnMatchStarted() {
        _playerInputActions.Player.ToggleTeams.Disable();
        teamsPanel.SetActive(false);
    }

    public void ChangeTeam(int teamID) {
        teamManager.SetTeamID(teamID);
        if(!NetworkManager.Singleton.IsHost)UpdateDisplayTeams();
        debugText.text = $"Team: {teamManager.GetLocalTeamID()}";
    }

    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
    }

    public void UpdateDisplayTeams() {
        
        // Reset team texts
        for (int i = 0; i < teamTexts.Length; i++) {
            teamTexts[i].text = "";
        }
        
        for(var j = 0; j < teamTexts.Length; j++) {
            for (var i = 0; i < teamManager.teams[j].Count; i++) {
                teamTexts[j].text += MatchManager.Instance.playerNames[teamManager.teams[j].ToArray()[i]];
                if (i < teamManager.teams[j].Count - 1) teamTexts[j].text += "\n";
            }
        }
    }
    
    void EnableChildren(GameObject parent) {
        foreach (Transform child in parent.transform) {
            child.gameObject.SetActive(true);
            if (child.childCount > 0) {
                EnableChildren(child.gameObject);
            }
        }
    }

}
