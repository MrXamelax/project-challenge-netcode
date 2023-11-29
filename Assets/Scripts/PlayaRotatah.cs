using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayaRotatah : MonoBehaviour {

    [SerializeField] private Transform tfCam;
    
    private void Update() {
        Vector3 lookAt = transform.position + tfCam.forward;
        transform.LookAt(new Vector3(lookAt.x, transform.position.y, lookAt.z));
    }
}
