using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour {

    [SerializeField] [Tooltip("Distance to follow target on y axis")]
    private float offsetY;
    
    public static FollowPlayer Instance { get; private set; }
    
    private Transform playerTransform;
    
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
        
    }

    private void Start() {
        gameObject.SetActive(false);
    }

    public void OnInitialize(Transform pos) {
        playerTransform = pos;
        gameObject.SetActive(true);
    }
    
    void Update() {
        transform.position = playerTransform.position + Vector3.up * offsetY;
    }
}
