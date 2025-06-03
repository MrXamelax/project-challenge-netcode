using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour {

    [SerializeField] private GameObject UINetwork;

    public void HideNetworkUI() {
        UINetwork.SetActive(false);
    }

}
