using System;
using UnityEngine;

// Display health above player every
public class HealthBar : MonoBehaviour {

    // Transform of health bar GameObject
    [SerializeField] private Transform healthBarTransform;
    
    private HealthSystem _healthSystem;

    // Initializing health system and subscribing to health change event
    public void Setup(HealthSystem healthSystem) {
        _healthSystem = healthSystem;
        healthSystem.OnHealthChanged += HealthSystem_OnHealthChanged;
    }

    // Display health change by adjusting health bar
    private void HealthSystem_OnHealthChanged(object sender, EventArgs e) {
        healthBarTransform.localScale = new Vector3(_healthSystem.GetHealthPercent() * 10, 1);
    }
    
}