using System;
using System.Collections;
using System.Collections.Generic;
using Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionTrigger : MonoBehaviour {

    [SerializeField] private GameObject uiInteract;

    private NewPlayerInputActions _playerInputActions;
    
    private bool _isInteracting = false;
    private GameObject goInteractable;
    
    private void Start() {
        _playerInputActions = new NewPlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Interact.performed += Interact_performed;
    }
    
    public void SetIsInteracting(bool isInteracting) {
        _isInteracting = isInteracting;
    }

    private void Interact_performed(InputAction.CallbackContext obj) {
        if (_isInteracting && goInteractable != null) {
            goInteractable.GetComponentInParent<IInteractable>().Interact();
        }
        uiInteract.SetActive(false);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Interactable")) {
            //Debug.Log("Interactable detected!");
            _isInteracting = true;
            goInteractable = other.gameObject;
            uiInteract.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Interactable")) {
            Debug.Log("Interactable gone!");
            _isInteracting = false;
            goInteractable = null;
            uiInteract.SetActive(false);
        }
    }
    
    
}