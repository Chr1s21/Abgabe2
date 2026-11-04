using UnityEngine;
using Unity.Netcode;      
using UnityEngine.UI;  

public class HealthBar : NetworkBehaviour
{
   [SerializeField] PlayerHealth playerHealth;
   [SerializeField] Slider slider;
   [SerializeField] Transform canvasTransform;

    public override void OnNetworkSpawn()
    {
        slider.maxValue = playerHealth.maxHealth;
        slider.value = playerHealth.currentHealth.Value;

        playerHealth.currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        playerHealth.currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        slider.value = newValue;
    }

    void LateUpdate()
    {
        if(Camera.main != null)
        {
            canvasTransform.forward = Camera.main.transform.forward;
        }
    }
}
