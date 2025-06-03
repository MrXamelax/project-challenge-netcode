using System;
using System.Collections.Generic;
using System.Linq;
using Contracts;
using Unity.Netcode;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class TeamManager : NetworkBehaviour {

    [SerializeField] private GameObject minimapIconPlayer;
    [SerializeField] private SpriteRenderer barSpriteRenderer;

    // First row teamID, second row playerID
    public List<ulong>[] teams = new List<ulong>[5];
    private List<Transform> teamSpawns;

    private int localTeamID;

    private Vector3 _localSpawnPos;
    
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
        
        // Referencing spawn points for teams in scene
        if (IsHost && IsOwner) {
            teamSpawns = GameObject.Find("TeamSpawns").GetComponentsInChildren<Transform>().ToList();
            teamSpawns.RemoveAt(0);
        }
    }

    private void Awake() {
        localTeamID = 0;
    }

    public void OnMatchStarted() {
        // Seed generation voodoo magic
        var currentTimeTicks = DateTime.UtcNow.Ticks;
        var seed = (uint)(currentTimeTicks ^ (currentTimeTicks >> 32));
        var rng = new Random(seed);
        
        // Pseudo matrix for local spawn point distribution
        Vector2[] localSpawns = {new (1, 1), new (-1, 1), new (1, -1), new (-1, -1)};

        // Team spawn distribution
        var iterator = teamSpawns.Count; // Caching loop variable since original value is getting changed
        for (var i = 0; i < iterator; i++) {
            
            // Randomizing team spawn
            var teamSpawn = teamSpawns[rng.NextInt(0, teamSpawns.Count)];
            
            // Local spawn distribution, not randomized, based on team join order
            for (var j = 0; j < teams[i].Count; j++) {
                
                // Local spawn is a square of 7x7, players spawn in corners
                var localSpawnPoint = Vector2.Scale(new Vector2(3.5f, 3.5f), localSpawns[j]);
                var spawnPoint = localSpawnPoint + new Vector2(teamSpawn.position.x, teamSpawn.position.z);
                
                // ClientID is cached in teams list array
                var clientID = teams[i][j];
                
                // Sending spawn point to player
                TeleportClientRpc(spawnPoint.x, teamSpawn.position.y, spawnPoint.y, new ClientRpcParams 
                    { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>{clientID}}});
            }
            
            // Removing team spawn so it is not used again
            teamSpawns.Remove(teamSpawn);
        }
    }

    public Vector3 GetLocalSpawnPos() {
        return _localSpawnPos;
    }
    
    private void SetLocalSpawnPos(Vector3 pos) {
        _localSpawnPos = pos;
    }
    
    // Teleport local player to given coordinates
    [ClientRpc]
    private void TeleportClientRpc(float x, float y, float z, ClientRpcParams rpcParams = default) {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.position = new Vector3(x, y, z);
        player.GetComponent<TeamManager>().SetLocalSpawnPos(new Vector3(x, y, z));
    }

    #region SetTeamID
    
    public void SetTeamID(int teamID) {
        bool swap = false;
        // If same team, button does nothing
        if (teamID == localTeamID) {
            //Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} is already in team {teamID}");
            return;
        }

        // If team is full, don't let player join
        if (teams[teamID - 1].Count >= 4) {
            //Debug.Log($"Team {teamID} is full!");
            return;
        }

        // If already in that team, don't let player join
        if (teams[teamID - 1].Contains(NetworkManager.Singleton.LocalClientId)) {
            //Debug.Log($"Player {NetworkManager.Singleton.LocalClientId} is already in team {teamID}");
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

        if (teamID == teamManager.GetLocalTeamID()) {
            minimapIconPlayer.SetActive(true);
            barSpriteRenderer.color = Color.green;
        } else {
            minimapIconPlayer.SetActive(false);
            barSpriteRenderer.color = Color.red;
        }
        
        SetTeamIDClientRpc(teamID, rpcParams.Receive.SenderClientId, swap, rpcParams.Receive.SenderClientId, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = ClientListExcept(rpcParams.Receive.SenderClientId)}});
        teamManager.DistributeTeamUpdateServerRpc(teamID, rpcParams.Receive.SenderClientId);
        
        GameUI.Instance.UpdateDisplayTeams();
        ScoreboardManager.Instance.UpdateDisplayScoreboard();
    }
    
    [ClientRpc]
    private void SetTeamIDClientRpc(int teamID, ulong player, bool swap, ulong originalSender, ClientRpcParams rpcParams = default) {
        if (IsHost) return;
        
        var teamManager = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>();
        
        if (swap) teamManager.teams[localTeamID-1].Remove(player);
        
        localTeamID = teamID;
        
        teamManager.UpdateTeams(teamID, player);

        if (teamID == NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>().GetLocalTeamID()) {
            minimapIconPlayer.SetActive(true);
            barSpriteRenderer.color = Color.green;
        } else {
            minimapIconPlayer.SetActive(false);
            barSpriteRenderer.color = Color.red;
        }
        
        teamManager.DistributeTeamUpdateServerRpc(teamID, originalSender);
        
        GameUI.Instance.UpdateDisplayTeams();
        ScoreboardManager.Instance.UpdateDisplayScoreboard();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DistributeTeamUpdateServerRpc(int teamID, ulong originalSender) {
        DistributeTeamUpdateClientRpc(teamID, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>{originalSender}}});
    }

    [ClientRpc]
    private void DistributeTeamUpdateClientRpc(int teamID, ClientRpcParams rpcParams = default) {
        if (teamID == localTeamID) {
            minimapIconPlayer.SetActive(true);
            barSpriteRenderer.color = Color.green;
        } else {
            minimapIconPlayer.SetActive(false);
            barSpriteRenderer.color = Color.red;
        }
        
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
        if (!MatchManager.Instance.IsMatchRunning()) return;
        //Debug.Log($"Awarding {points} points to team {teamID+1}!");
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
        if (!MatchManager.Instance.IsMatchRunning()) return;
        var contractType = Constants.CONTRACT_MAP[contract.GetContractID()].GetType();
        var contractProgress = ContractManager.Instance.GetContractOfType2(teamID, contractType);
        var points = contractProgress.AddProgress(progress);
        GameUI.Instance.UpdateDisplayProgress();
        GameUI.Instance.UpdateDisplayPoints();
        if (points > 0) {
            AddPointsServerRpc(points, teamID-1);
            LoggingManager.LoggingType loggingType;
            // Hard coded but no better solution right now
            switch (contractType.ToString()) {
                case "Contracts.DataStation":
                    loggingType = LoggingManager.LoggingType.PointsDataStation;
                    break;
                case "Contracts.Zeal":
                    loggingType = LoggingManager.LoggingType.PointsZeal;
                    break;
                case "Contracts.GasLeak":
                    loggingType = LoggingManager.LoggingType.PointsGasLeak;
                    break;
                default:
                    loggingType = LoggingManager.LoggingType.Error;
                    break;
            }
            LoggingManager.Instance.LogEvent(
                NetworkManager.Singleton.ConnectedClients[teams[teamID-1][0]],
                loggingType
            );
        }
        UpdateAllContractsProgress(teamID-1);
    }
    
    private void UpdateAllContractsProgress(int teamID) {
        foreach (var contract in ContractManager.Instance.GetTeamContracts()[teamID]) {
            UpdateProgressClientRpc(teamID, contract.GetContractID(), contract.GetProgressToNextLevel(), contract.GetLevel());
        }
    }

    [ClientRpc]
    private void UpdateProgressClientRpc(int teamID, int contractID, int progressToNextLevel, int level) {
        if (IsHost) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<TeamManager>().GetLocalTeamID()-1 != teamID) return;
        var contract = ContractManager.Instance.GetTeamContracts()[teamID][contractID];
        contract.SetLevel(level);
        contract.SetProgressToNextLevel(progressToNextLevel);
        GameUI.Instance.UpdateDisplayProgress();
        GameUI.Instance.UpdateDisplayPoints();
    }
    
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