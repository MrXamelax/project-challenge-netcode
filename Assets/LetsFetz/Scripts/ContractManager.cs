using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContractManager : MonoBehaviour {
    
    private List<Contract> _contractList;
    private List<Contract>[] _teamContracts;
    
    public static ContractManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        Initialize();
    }
    
    public List<Contract> GetContractList() {
        return _contractList;
    }

    public Contract GetContractOfType2(int teamID, Type contractType) {
        foreach (var contract in _teamContracts[teamID-1]) {
            if (contract.GetType() == contractType) return contract;
        }
        Debug.Log("Upsii, hier ist wohl etwas schief gegangen :o");
        return null;
    }
    
    public Contract GetContractOfType(Type contractType) {
        foreach (var contract in _contractList) {
            if (contract.GetType() == contractType) return contract;
        }
        Debug.Log("Upsii, hier ist wohl etwas schief gegangen :o");
        return null;
    }

    private void Initialize() {
        //_contractList = new List<Contract>();
        //_contractList.Add(new Contracts.DataStation());
        _teamContracts = new List<Contract>[5]; // Number of teams
        for (int i = 0; i < _teamContracts.Length; i++) {
            _teamContracts[i] = new List<Contract>();
            _teamContracts[i].Add(new Contracts.DataStation());
            _teamContracts[i].Add(new Contracts.GasLeak());
            _teamContracts[i].Add(new Contracts.Zeal());
        }
    }
    
}
