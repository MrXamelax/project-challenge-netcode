using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorY : MonoBehaviour {

    private Vector3 offsetRot = new Vector3(0, 180, 0);
    
    void Update() {
        transform.Rotate(offsetRot);
    }
}
