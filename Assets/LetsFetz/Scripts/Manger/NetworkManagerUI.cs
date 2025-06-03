using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour {

    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private TMP_InputField inputIP;
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private GameObject uiNetwork;
    [SerializeField] private Button startMatchBtn;

    [SerializeField] private int maxNameLength = 16;

    private void Awake() {
        
        // Not really needed anymore
        serverBtn.onClick.AddListener(() => {
            ChangeIP();
            NetworkManager.Singleton.StartServer();
        });
        
        // Launch Host with given IP (Server + Client itself)
        hostBtn.onClick.AddListener(() => {
            ChangeIP();
            SwapUI(true);
            NetworkManager.Singleton.StartHost();
        });
        
        // Launch Client connecting to given IP
        clientBtn.onClick.AddListener(() => {
            ChangeIP();
            SwapUI();
            NetworkManager.Singleton.StartClient();
        });
        
        // Limiting name length to maxNameLength
        inputName.onValueChanged.AddListener((param) => {
            if (inputName.text.Length > maxNameLength) inputName.text = inputName.text.Substring(0, maxNameLength);
        });
    }
    


    private void ChangeIP() {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (inputIP.text != "") transport.ConnectionData.Address = inputIP.text;
    }
    
    // Disable network UI and show 'Start Match' Button for hosting player
    private void SwapUI(bool isHosting = false) {
        uiNetwork.SetActive(false);
        if (isHosting) startMatchBtn.gameObject.SetActive(true);
        
        if (inputName.text != "") MatchManager.Instance.SetLocalName(inputName.text);
        else MatchManager.Instance.SetLocalName("Player " + NetworkManager.Singleton.LocalClientId);
    }
    
}