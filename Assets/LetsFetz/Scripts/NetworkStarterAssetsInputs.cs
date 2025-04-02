using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

public class NetworkStarterAssetsInputs : NetworkBehaviour {
    [Header("Character Input Values")] public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;

    [Header("Movement Settings")] public bool analogMovement;

    [Header("Mouse Cursor Settings")] public bool cursorLocked = true;
    public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    public void OnMove(InputValue value) {
        if (!IsOwner) return;
        Debug.Log($"OnMove called by {NetworkManager.Singleton.LocalClientId}");
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value) {
        if (!IsOwner) return;
        Debug.Log($"OnLook called by {NetworkManager.Singleton.LocalClientId}");
        if (cursorInputForLook) {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value) {
        if (!IsOwner) return;
        Debug.Log($"OnJump called by {NetworkManager.Singleton.LocalClientId}");
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value) {
        if (!IsOwner) return;
        Debug.Log($"OnSprint called by {NetworkManager.Singleton.LocalClientId}");
        SprintInput(value.isPressed);
    }
#endif


    public void MoveInput(Vector2 newMoveDirection) {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection) {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState) {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState) {
        sprint = newSprintState;
    }

    private void OnApplicationFocus(bool hasFocus) {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState) {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}