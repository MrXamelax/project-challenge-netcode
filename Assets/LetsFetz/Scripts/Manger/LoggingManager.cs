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

    private float _timeStart;
    private string _loggingFolderPath;

    public enum LoggingType {
        DamageDone, DamageTaken, StopRegeneration,
        Respawn, Death, 
        CaptureDataStation, CaptureYellowZeal, CaptureRedZeal, CaptureGasLeak,
        DropYellowZeal, DropRedZeal, StealGasLeak
    }

    private void Awake() {

        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }

    }

    public void ToggleLogging(bool start) {
        if (start) {
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

    private void LogRegular() {
        var timePassed = Time.time - _timeStart;
        var timeSpan = TimeSpan.FromSeconds(timePassed);
        var timeString = timeSpan.ToString(@"mm\:ss\:ff");
        string data = timeString + ",";
        WriteLog(data);
    }

    public void LogEvent() {
        string data = "";
        WriteLog(data);
    }

    private void WriteLog(string message) {
        _loggingData.Add(message);
    }

    private string PrepareLog() {
        string data = "Timestamp,Playername,PlayerID,TeamID,x,y,z,HP,Event\n";
        foreach (var line in _loggingData) {
            data += line + ";\n";
        }
        return data;
    }

    private void ExportData() {
        //TODO: unique file name
        string filename = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_log.txt";
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