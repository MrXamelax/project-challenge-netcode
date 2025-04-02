using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRotation : MonoBehaviour {
    
    private enum RotationType {X, Y, Z};

    [SerializeField] private RotationType _rotationType;
    [SerializeField] private float _rotationSpeed = 1f;
    private void Update() {
        switch (_rotationType) {
            case RotationType.X:
                transform.Rotate(Vector3.right * (_rotationSpeed * Time.deltaTime));
                break;
            case RotationType.Y:
                transform.Rotate(Vector3.up * (_rotationSpeed * Time.deltaTime));
                break;
            case RotationType.Z:
                transform.Rotate(Vector3.forward * (_rotationSpeed * Time.deltaTime));
                break;
        }
    }
}
