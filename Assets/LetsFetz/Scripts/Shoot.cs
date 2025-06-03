using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shoot : MonoBehaviour {

    [SerializeField] private Transform bulletPrefab;
    
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void ShootBullet() {
        Transform bullet = Instantiate(bulletPrefab);
        bullet.GetComponent<NetworkObject>().Spawn();
    }
    
    
}