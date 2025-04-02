using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NetcodeCodemonkey.Scripts {
    public class EyeWaggle : MonoBehaviour {
        [SerializeField] private Transform tf;
        [SerializeField] private int speed = 3;
        [SerializeField] private int minSpeed = 10;
        [SerializeField] private int maxSpeed = 20;
        [SerializeField] private int maxAngle = 10;
        
        private int signum = 1;

        private void Update() {



            if (tf.rotation.eulerAngles.z >= 10) changeDirectionAndSpeed();
            
            //if (forward) Debug.Log("vorwärts!" + tf.rotation.eulerAngles.z);
            //else Debug.Log("rückwärts!" + tf.rotation.eulerAngles.z);

            else if (tf.rotation.eulerAngles.z <= 350 && tf.rotation.eulerAngles.z >= 10) changeDirectionAndSpeed();
            
            tf.Rotate(Vector3.forward, signum * speed * Time.deltaTime);
        }

        private void changeDirectionAndSpeed() {
            signum *= -1;
            int change = Random.Range(1, maxSpeed - minSpeed);
            Debug.Log("Change: " + change);
            if (speed + change > maxSpeed) speed -= change;
            else speed += change;
        }
        
    }
}
