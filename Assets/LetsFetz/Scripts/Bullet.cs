using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour {

    [SerializeField] private float bulletSpeed = 20.0f;

    // value for debugging to see, if it got changed at all
    public ulong bulletClientID = 1337;

    private void Start() {
        StartCoroutine(BulletTimer());
    }

    private void FixedUpdate() {
        //if (!IsOwner) return;
        transform.position += transform.forward * (bulletSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Bullet") || other.CompareTag("Passable")) return;
        if (other.CompareTag("Player")) {
            if (other.GetComponent<NetworkObject>().OwnerClientId == bulletClientID) return;
        }
        /*
        if (other.gameObject.TryGetComponent(out NetworkObject no)) {
            //if (no.IsOwner) return;
            print("Check");
            if (no.OwnerClientId == bulletClientID) return;
        }
        */
        //print("Destroying Bullet because");
        Debug.Log($"I did interact with collider of {other.gameObject.name}");
        Destroy(gameObject);
    }

    IEnumerator BulletTimer() {
        yield return new WaitForSeconds(5.0f);
        Destroy(gameObject);
        //DestroyBulletServerRpc();
    }
    
    [ServerRpc]
    private void DestroyBulletServerRpc() {
        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
    
}