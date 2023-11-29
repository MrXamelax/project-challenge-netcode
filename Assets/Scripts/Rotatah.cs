using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Rotatah : MonoBehaviour {

    //[SerializeField] private float rotationSpeed = 1;

    [SerializeField] private float rotationX;
    [SerializeField] private float rotationY;
    [SerializeField] private float rotationZ;

    [SerializeField] private float rotationSpeed;
    
    //[SerializeField][Range(1,3)] private byte axis;

    //private Vector3 _axisVector;

    /*private void Awake() {
        switch (axis) {
            case 1:
                _axisVector = Vector3.right;
                break;
            case 2:
                _axisVector = Vector3.up;
                break;
            case 3:
                _axisVector = Vector3.forward;
                break;
            default:
                _axisVector = Vector3.up;
                break;
        }
    }*/

    private void Update() {
        //REMOVELATER();
        //transform.Rotate(_axisVector, rotationSpeed * Time.deltaTime);
        transform.Rotate(new Vector3(rotationX, rotationY, rotationZ) * Time.deltaTime * rotationSpeed, Space.World);
    }

    /*private void REMOVELATER() {
        switch (axis) {
            case 1:
                _axisVector = Vector3.right;
                break;
            case 2:
                _axisVector = Vector3.up;
                break;
            case 3:
                _axisVector = Vector3.forward;
                break;
            default:
                _axisVector = Vector3.up;
                break;
        }
    }*/
    
}