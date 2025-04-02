using Unity.Netcode;
using UnityEngine;

// Change Rotation to face the local player GameObject every frame
public class FaceLocalPlayer : MonoBehaviour {

    // UI in World Space is turned 180° by default, idk why
    [SerializeField][Tooltip("For UI in World Space")]
    private bool mirrorY = false;
    private Vector3 offsetRot = new Vector3(0, 180, 0);
    
    // Player Transform to face
    private Transform playerTf;
    
    // No need to do this for local player, since we are playing in 1st person
    private bool _isLocalPlayer = false;

    // Caching components
    private void Start() {
        _isLocalPlayer = GetComponentInParent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId;
        
        if (_isLocalPlayer) return; // No need to do this for local player
        
        playerTf = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.transform;
    }

    // Rotate axes to face local player
    // Changing x and z rotation back to 0, so only y gets rotated
    // TODO: resetting x and z could look weird in some situations, evaluate later
    private void Update() {
        
        if (_isLocalPlayer) return; // No need to do this for local player
        
        // Rotation
        transform.LookAt(playerTf, Vector3.up);
        var angles = transform.eulerAngles;
        angles.x = 0;
        angles.z = 0;
        transform.eulerAngles = angles;
        
        if (mirrorY) transform.Rotate(offsetRot); // Flip vertically, most likely for UI
    }
}
