using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameUI : MonoBehaviour {
    
    public static GameUI Instance { get; private set; }
    
    [SerializeField] private GameObject _teamsPanel;
    [SerializeField] private GameObject _scoreboard;
    [SerializeField] private GameObject _minimap;
    [SerializeField] private GameObject _progressPanel;
    [SerializeField] private TMP_Text[] _progressTexts;
    [SerializeField] private TMP_Text[] _pointsTexts;
    
    [SerializeField] private TMP_Text _debugText;
    [SerializeField] private TMP_Text[] _teamTexts;
    
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

        _debugText.text = "Team: 0";
    }

    private void Start() {
        //_minimap.SetActive(false);
    }

    private void InitializeDisplayProgress() {
        var contracts = ContractManager.Instance.GetTeamContracts()[0];
        //Debug.Log("Size: " + contracts.Count);
        for (int i = 0; i < _pointsTexts.Length; i++) {
            //Debug.Log("InitializeDisplayProgress() " + i);
            //_progressTexts[i].text = contracts.ToArray()[i].GetPointsPerLevel()[0].ToString();
            _pointsTexts[i].text = $"{contracts[i].GetPointsPerLevel()[0].ToString()}";
            _progressTexts[i].text = $"0 / {contracts[i].GetProgressToNextLevel()}";
            //Debug.Log(i + ": " + contracts[i].GetPointsPerLevel().Length);
            //Debug.Log(i + ": " + contracts[i]);
        }
    }
    
    public void UpdateDisplayProgress() {
        
        // gets called when progress is added to any contract
        // does this need be rpc'd when team progress thingy?
        var contracts = ContractManager.Instance.GetTeamContracts()[teamManager.GetLocalTeamID()-1];
        for (int i = 0; i < _progressTexts.Length; i++) {
            _progressTexts[i].text = $"{contracts[i].GetProgressNeeded() - contracts[i].GetProgressToNextLevel()} / {contracts[i].GetProgressNeeded()}";
        }
    }

    public void UpdateDisplayPoints() {
        var contracts = ContractManager.Instance.GetTeamContracts()[teamManager.GetLocalTeamID()-1];
        for (int i = 0; i < _pointsTexts.Length; i++) {
            _pointsTexts[i].text = $"{contracts[i].GetPointsPerLevelCurrent()}";
        }
    }

    public void OnInitialize() {
        _playerInputActions.Player.ToggleScoreboard.started += ToggleScoreboard_started;
        _playerInputActions.Player.ToggleScoreboard.canceled += ToggleScoreboard_canceled;
        _playerInputActions.Player.ToggleTeams.started += ToggleTeamsStarted;
        EnableChildren(_scoreboard);
        _scoreboard.SetActive(false);
        _minimap.SetActive(true);
        InitializeDisplayProgress();
    }

    private void ToggleTeamsStarted(InputAction.CallbackContext obj) {
        _teamsPanel.SetActive(!_teamsPanel.activeSelf);
    }
    
    private void ToggleScoreboard_started(InputAction.CallbackContext obj) {
        _minimap.SetActive(false);
        _progressPanel.SetActive(false);
        _scoreboard.SetActive(true);
    }
    
    private void ToggleScoreboard_canceled(InputAction.CallbackContext obj) {
        _scoreboard.SetActive(false);
        _minimap.SetActive(true);
        _progressPanel.SetActive(true);
    }

    // We don't want to allow picking a team while the match is already running
    public void OnMatchStarted() {
        _playerInputActions.Player.ToggleTeams.Disable();
        _teamsPanel.SetActive(false);
    }

    public void ChangeTeam(int teamID) {
        teamManager.SetTeamID(teamID);
        if(!NetworkManager.Singleton.IsHost)UpdateDisplayTeams();
        _debugText.text = $"Team: {teamManager.GetLocalTeamID()}";
    }

    public void SetTeamManager(TeamManager teamManagerHere) {
        teamManager = teamManagerHere;
    }

    public void UpdateDisplayTeams() {
        
        // Reset team texts
        for (int i = 0; i < _teamTexts.Length; i++) {
            _teamTexts[i].text = "";
        }
        
        for(var j = 0; j < _teamTexts.Length; j++) {
            for (var i = 0; i < teamManager.teams[j].Count; i++) {
                _teamTexts[j].text += MatchManager.Instance.playerNames[teamManager.teams[j].ToArray()[i]];
                if (i < teamManager.teams[j].Count - 1) _teamTexts[j].text += "\n";
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
