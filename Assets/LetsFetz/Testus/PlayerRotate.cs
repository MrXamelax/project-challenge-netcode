using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class PlayerRotate : MonoBehaviour {
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private float _speed;
    [SerializeField] private float _rotationLimit;
    
    private NewPlayerInputActions _playerInputActions;

    protected float vertRot;

    private void Awake() {
        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();
    }

    public virtual void Rotate() {
        vertRot -= GetVerticalValue();
        vertRot = vertRot <= -_rotationLimit ? -_rotationLimit :
                  vertRot >= _rotationLimit ? _rotationLimit :
                  vertRot;

        RotateVertical();
        RotateHorizontal();
    }

    protected float GetVerticalValue() => _playerInputActions.Player.Look.ReadValue<Vector2>().y * _speed * Time.deltaTime;
    protected float GetHorizontalValue() => _playerInputActions.Player.Look.ReadValue<Vector2>().x * _speed * Time.deltaTime;
    protected virtual void RotateVertical() => _cameraHolder.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
    protected virtual void RotateHorizontal() => transform.Rotate(Vector3.up * GetHorizontalValue());

}
