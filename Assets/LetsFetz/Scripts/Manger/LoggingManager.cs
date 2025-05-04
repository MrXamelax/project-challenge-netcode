using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class LoggingManager : MonoBehaviour {
    
    public static LoggingManager Instance { get; private set; }
    private Coroutine _cLoggingCycle;
    
    private List<string> _loggingData = new List<string>();

    private Dictionary<ulong, TeamManager> _teamManagers;
    private Dictionary<ulong, NewNetworkFirstPersonController> _nnfpControllers;

    private float _timeStart;
    private string _loggingFolderPath;

    public enum LoggingType {
        Regular,
        DamageDone, DamageTaken, StopRegeneration,
        Respawn, Death, 
        CaptureDataStation, CaptureYellowZeal, CaptureRedZeal, CaptureGasLeak,
        DropYellowZeal, DropRedZeal, StealGasLeak,
        PointsDataStation, PointsZeal, PointsGasLeak
    }

    private void Awake() {

        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }

    }

    private void Initialize() {
        _teamManagers = new Dictionary<ulong, TeamManager>();
        _nnfpControllers = new Dictionary<ulong, NewNetworkFirstPersonController>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            _teamManagers.Add(client.ClientId, client.PlayerObject.GetComponent<TeamManager>());
            _nnfpControllers.Add(client.ClientId, client.PlayerObject.GetComponent<NewNetworkFirstPersonController>());
        }
    }

    public void ToggleLogging(bool start) {
        if (start) {
            Initialize();
            Debug.Log("Start Logging");
            _timeStart = Time.time;
            _cLoggingCycle = StartCoroutine(LoggingCycle());
        } else {
            Debug.Log("Stop Logging");
            StopCoroutine(_cLoggingCycle);
            ExportData();
        }
    }
    
    private IEnumerator LoggingCycle() {
        while (true) {
            yield return new WaitForSeconds(Constants.LOGGER_INTERVAL);
            LogRegular();
        }
    }

    private string TimeStamp() {
        var timePassed = Time.time - _timeStart;
        var timeSpan = TimeSpan.FromSeconds(timePassed);
        var timeString = timeSpan.ToString(@"mm\:ss\:ff");
        return timeString;
    }

    private void LogRegular() {
        string data = "";
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            /*
            var player = client.PlayerObject;
            var pos = player.transform.position;
            data += $"{TimeStamp()};";
            data += $"{MatchManager.Instance.GetPlayerNameByClientID(client.ClientId)};";
            data += $"{client.ClientId};";
            data += $"{_teamManagers[client.ClientId].GetLocalTeamID()};";
            data += $"{pos.x};";
            data += $"{pos.y};";
            data += $"{pos.z};";
            data += $"{_nnfpControllers[client.ClientId].GetHealth()};";
            data += $"{MatchManager.Instance.GetPointsFromTeam(_teamManagers[client.ClientId].GetLocalTeamID())};";
            data += "Regular\n";
            */
            data += DataEntry(client, LoggingType.Regular);
        }
        WriteLog(data);
    }

    public void LogEvent(NetworkClient client, LoggingType type) {
        WriteLog(DataEntry(client, type));
    }
    
    private string DataEntry(NetworkClient client, LoggingType type) {
        string data = "";
        var player = client.PlayerObject;
        var pos = player.transform.position;
        data += $"{TimeStamp()};";
        data += $"{MatchManager.Instance.GetPlayerNameByClientID(client.ClientId)};";
        data += $"{client.ClientId};";
        data += $"{_teamManagers[client.ClientId].GetLocalTeamID()};";
        data += $"{pos.x};";
        data += $"{pos.y};";
        data += $"{pos.z};";
        data += $"{_nnfpControllers[client.ClientId].GetHealth()};";
        data += $"{MatchManager.Instance.GetPointsFromTeam(_teamManagers[client.ClientId].GetLocalTeamID())};";
        data += $"{type.ToString()}\n";
        return data;
    }

    private void WriteLog(string message) {
        _loggingData.Add(message);
    }

    private string PrepareLog() {
        //TODO: Add points (of every contract?) to log
        string data = "Timestamp;Playername;PlayerID;TeamID;x;y;z;HP;Points;Event\n";
        foreach (var line in _loggingData) {
            data += line;
        }
        return data;
    }

    private void ExportData() {
        var playername = MatchManager.Instance.GetPlayerNameByClientID(NetworkManager.Singleton.LocalClientId);
        string filename = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Log_{playername}.txt";
        _loggingFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TheoEval/";
        string fullPath = _loggingFolderPath + filename;
        //string externalPath = Application.persistentDataPath + "/" + filename;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, PrepareLog());
    }

    public void OpenLogFolder() {
        if (NetworkManager.Singleton.IsHost) Application.OpenURL(_loggingFolderPath);
    }

}