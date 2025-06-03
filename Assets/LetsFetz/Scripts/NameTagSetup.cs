using TMPro;
using Unity.Netcode;
using UnityEngine;

// On spawn, change name tag above overlying player GameObject
// Gets called by every player object on entering the scene
public class NameTagSetup : MonoBehaviour {

    // Scene reference for TextMeshPro Component
    [SerializeField] private TMP_Text textNameTag;

    // Setter
    public void SetTextNameTag(string playerName) {
        textNameTag.text = playerName;
    }
    
    // Setter gets called through chain of RPCs for the local player
    // No need to do this for our own name tag, since we are playing in 1st person
    private void Start() {
        if (GetComponentInParent<NetworkObject>().IsOwner) {
            MatchManager.Instance.AddPlayerWithName(NetworkManager.Singleton.LocalClientId, MatchManager.Instance.GetLocalName());
            return;
        }
        var teamManager = GetComponentInParent<TeamManager>();
        teamManager.RetrievePlayerName();
    }
}
