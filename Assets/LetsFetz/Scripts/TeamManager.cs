using System.Collections.Generic;
using Contracts;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour {

    // First row teamID, second row playerID
    public List<ulong>[] teams = new List<ulong>[5];

    private int localTeamID;
    
    public int GetLocalTeamID() {
        return localTeamID;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) Initialize();
    }

    private void Initialize() {
        for (int i = 0; i < teams.Length; i++) {
            teams[i] = new List<ulong>();
        }
        GameUI.Instance.SetTeamManager(this);
        GameUI.Instance.UpdateDisplayTeams();
        ScoreboardManager.Instance.SetTeamManager(this);
        ScoreboardManager.Instance.UpdateDisplayScoreboard();
        if (IsHost) {
            GameObject.Find(Constants.DATASTATION_GAMEOBJECT_NAME).GetComponent<DataStationObject>().SetTeamManager(this);
        }
        foreach (var gasleak in GameObject.FindGameObjectsWithTag(Constants.GASLEAK_GAMEOBJECT_TAG)) {
            gasleak.GetComponent<GasLeakObject>().SetTeamManager(this);
        }
    }

    private void Awake() {
        localTeamID = 0;
    }

    #region SetTeamID
    
    public void SetTeamID(int teamID) {
        bool swap = false;
        // If same team, button does nothing
        if (teamID == localTeamID) {
            Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} is already in team {teamID}");
            return;
        }

        // If team is full, don't let player join
        if (teams[teamID - 1].Count >= 4) {
            Debug.Log($"Team {teamID} is full!");
            return;
        }

        // If already in that team, don't let player join
        if (teams[teamID - 1].Contains(NetworkManager.Singleton.LocalClientId)) {
            Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} is already in team {teamID}");
            return;
        }
        
        // If player already is in a team, that means he switches teams
        if (localTeamID != 0) {
            teams[localTeamID-1].Remove(OwnerClientId);
            swap = true;
        }

        // Host gets logic in ServerRpc
        if (!IsHost) {
            localTeamID = teamID;
            UpdateTeams(teamID, OwnerClientId);
            ScoreboardManager.Instance.UpdateDisplayScoreboard();
        }
        
        SetTeamIDServerRpc(teamID, swap);
    }

    [ServerRpc]
    private void SetTeamIDServerRpc(int teamID, bool swap, ServerRpcParams rpcParams = default) {
        var teamManager = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>();
        if (swap) teamManager.teams[localTeamID-1].Remove(OwnerClientId);
        
        
        localTeamID = teamID;
        
        teamManager.UpdateTeams(teamID, OwnerClientId);
        
        SetTeamIDClientRpc(teamID, rpcParams.Receive.SenderClientId, swap, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
        
        GameUI.Instance.UpdateDisplayTeams();
        ScoreboardManager.Instance.UpdateDisplayScoreboard();
    }
    
    [ClientRpc]
    private void SetTeamIDClientRpc(int teamID, ulong player, bool swap, ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        
        var teamManager = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>();
        
        if (swap) teamManager.teams[localTeamID-1].Remove(player);
        
        localTeamID = teamID;
        
        teamManager.UpdateTeams(teamID, player);
        GameUI.Instance.UpdateDisplayTeams();
        ScoreboardManager.Instance.UpdateDisplayScoreboard();
    }

    private void UpdateTeams(int teamID, ulong playerID) {
        teams[teamID-1].Add(playerID);
    }
    
    #endregion

    #region Player Name
    
    
    public void RetrievePlayerName() {
        if (OwnerClientId == NetworkManager.Singleton.LocalClientId) {
             GetComponentInChildren<NameTagSetup>().SetTextNameTag(MatchManager.Instance.GetLocalName());
             MatchManager.Instance.AddPlayerWithName(OwnerClientId, MatchManager.Instance.GetLocalName());
             return;
        }
        RetrievePlayerNameServerRpc(OwnerClientId);
    }
    
    // Send request for name to server
    [ServerRpc(RequireOwnership = false)]
    private void RetrievePlayerNameServerRpc(ulong playerWithNameTag, ServerRpcParams rpcParams = default) {
        RetrievePlayerNameClientRpc(rpcParams.Receive.SenderClientId, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>{playerWithNameTag}}});
    }
    
    // Send request for name change to actual client
    // Gets local name and sends it to Server via RPC
    [ClientRpc]
    private void RetrievePlayerNameClientRpc(ulong originalSender, ClientRpcParams rpcParams = default) {
        SendPlayerNameServerRpc(MatchManager.Instance.GetLocalName(), originalSender);
    }
    
    // Gets name from player and sends it back to original client
    [ServerRpc]
    private void SendPlayerNameServerRpc(string playerName, ulong originalSender, ServerRpcParams rpcParams = default) {
        SendPlayerNameClientRpc(playerName, rpcParams.Receive.SenderClientId, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>{originalSender}}});
    }
    
    // Only executed on original client to set name tag from player for which it got requested
    [ClientRpc] 
    private void SendPlayerNameClientRpc(string playerName, ulong playerId, ClientRpcParams rpcParams = default) {
        GetComponentInChildren<NameTagSetup>().SetTextNameTag(playerName);
        MatchManager.Instance.AddPlayerWithName(playerId, playerName);
    }
    
    #endregion
    
    #region Add Points
    
    
    public void AddPoints(int points) {
        if (!IsHost) {
            ScoreboardManager.Instance.UpdateDisplayPoints(points, localTeamID-1);
            MatchManager.Instance.AddLocalPoints(points);
        }
        AddPointsServerRpc(points, localTeamID-1);
    }
    
    // teamID starts at 0 from here on!! --> call with -1
    [ServerRpc]
    private void AddPointsServerRpc(int points, int teamID, ServerRpcParams rpcParams = default) {
        Debug.Log($"Awarding {points} points to team {teamID+1}!");
        ScoreboardManager.Instance.UpdateDisplayPoints(points, teamID);
        AddPointsClientRpc(points, teamID, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
    }
        
    [ClientRpc]
    private void AddPointsClientRpc(int points, int teamID, ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        ScoreboardManager.Instance.UpdateDisplayPoints(points, teamID);
    }
    
    #endregion

    #region Add Progress

    public void AddProgressOnServer(int teamID, Contract contract, int progress) {
        var contractType = Constants.CONTRACT_MAP[contract.GetContractID()].GetType();
        var contractProgress = ContractManager.Instance.GetContractOfType2(teamID, contractType);
        var points = contractProgress.AddProgress(progress);
        GameUI.Instance.UpdateDisplayProgress();
        GameUI.Instance.UpdateDisplayPoints();
        if (points > 0) AddPointsServerRpc(points, teamID-1);
    }

    private void UpdateProgressOnClient() {
        
    }
    
    /*
    public void AddProgressToContract(int teamID, Contract contract) {
        AddProgressToContractServerRpc(teamID, contract.GetContractID());
    }
    
    [ServerRpc]
    private void AddProgressToContractServerRpc(int teamID, int contractID, ServerRpcParams rpcParams = default) {
        //var contractType = Constants.CONTRACT_MAP[contractID].GetType();
        //var contract = ContractManager.Instance.GetContractOfType(contractType);
        //contract.AddProgress(Constants.DATASTATION_PROGRESS_PER_TICK);
        AddProgressToContractClientRpc(teamID, contractID, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListFromTeam(teamID)}} );
    }

    [ClientRpc]
    private void AddProgressToContractClientRpc(int teamID, int contractID, ClientRpcParams rpcParams = default) {
        var contractType = Constants.CONTRACT_MAP[contractID].GetType();
        var contract = ContractManager.Instance.GetContractOfType(contractType);
        var points = contract.AddProgress(Constants.DATASTATION_PROGRESS_PER_TICK);
        //if (points > 0) AddPointsServerRpc(points, teamID);
    }
    */
    
    #endregion
    
    #region Helpers
    // Helper function
    // Create list of all connected clients except one
    private List<ulong> ClientListExcept(ulong senderClientID) {
        var clientIDList = new List<ulong>();
        
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds) {
            if (clientID == senderClientID) continue;
            clientIDList.Add(clientID);
        }

        return clientIDList;
    }
    
    // Helper function
    // Create list with all clients from a specific team
    private List<ulong> ClientListFromTeam(int teamID) {
        var clientIDList = new List<ulong>();

        foreach (var clientID in teams[teamID-1]) {
            clientIDList.Add(clientID);
        }

        return clientIDList;
    }

    // Helper function
    // Returns cumulated number of players from all teams
    public int GetNumberOfClientsInTeams() {
        var n = 0;
        foreach (var team in teams) n += team.Count;
        return n;
    }
    #endregion
    
}