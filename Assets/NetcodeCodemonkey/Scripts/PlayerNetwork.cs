using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerNetwork : NetworkBehaviour {

    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform spawnedObjectTransform;

    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<MyCustomData> mcd = new NetworkVariable<MyCustomData>(
        new MyCustomData {
            _int = 56,
            _bool = true,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct MyCustomData : INetworkSerializable {
        public int _int;
        public bool _bool;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
        }
    }
        
    public override void OnNetworkSpawn() {
        Initialize();
    }

    private void Initialize() {
        mcd.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) => {
            Debug.Log(OwnerClientId + "; " + newValue._int + "; " + newValue._bool);
        };
        randomNumber.OnValueChanged += (int previousValue, int newValue) => {
            Debug.Log(OwnerClientId + "; randomNumber: " + randomNumber.Value);
        };
    }
    
    private void Update() {
        
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T)) {
            spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);

            //TestServerRpc();
            //TestClientRPC();

            //randomNumber.Value = Random.Range(0, 100);
            /*
             mcd.Value = new MyCustomData {
                _int = 10,
                _bool = false,
            };
            */
        }

        if (Input.GetKeyDown(KeyCode.Y)) {
            Destroy(spawnedObjectTransform.gameObject);
            //spawnedObjectTransform.GetComponent<NetworkObject>().Despawn();
        }

        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float moveSpeed = 3f;
        transform.position += moveDir * (moveSpeed * Time.deltaTime);

    }

    [ServerRpc]
    private void TestServerRpc() {
        Debug.Log("TestServerRpc " + OwnerClientId);
    }

    [ClientRpc]
    private void TestClientRPC() {
        Debug.Log("TestClientRpc " + OwnerClientId);
    }
    
}