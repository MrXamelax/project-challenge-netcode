using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUIL : MonoBehaviour {

    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private TMP_InputField inputIP;
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private GameObject uiNetwork;
    [SerializeField] private Button startMatchBtn;

    [SerializeField] private int maxNameLength = 20;

    private void Awake() {
        
        // Not really needed anymore
        serverBtn.onClick.AddListener(() => {
            ChangeIP();
            SwapUI();
            NetworkManager.Singleton.StartServer();
        });
        
        // Launch Host with given IP (Server + Client itself)
        hostBtn.onClick.AddListener(() => {
            ChangeIP();
            SwapUI(true);
            NetworkManager.Singleton.StartHost();
            if (inputName.text != "") MatchManager.Instance.SetLocalName(inputName.text);
            else MatchManager.Instance.SetLocalName("Player" + NetworkManager.Singleton.LocalClientId);
        });
        
        // Launch Client connecting to given IP
        clientBtn.onClick.AddListener(() => {
            ChangeIP();
            SwapUI();
            NetworkManager.Singleton.StartClient();
            if (inputName.text != "") MatchManager.Instance.SetLocalName(inputName.text);
            else MatchManager.Instance.SetLocalName("Player" + NetworkManager.Singleton.LocalClientId);
        });
        
        // Limiting name length to maxNameLength
        inputName.onEndEdit.AddListener((param) => {
            Debug.Log("EndEdit");
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
        MatchManager.Instance.SetLocalName(inputName.text);
    }
    
}