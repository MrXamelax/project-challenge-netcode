using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayOnPlayer : MonoBehaviour {

    [SerializeField] private Transform tfPlayer;
    
    private void Update() {
        transform.position = tfPlayer.position;
    }
}
