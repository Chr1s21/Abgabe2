using UnityEngine;
using Unity.Netcode;

public class Destructible : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    // Alle dürfen lesen (für Anzeige/Farbe), nur der Server schreibt.
    public NetworkVariable<int> Health = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int MaxHealth => maxHealth;

    // 1 = voll, 0 = kaputt. Praktisch für Farben oder Balken.
    public float HealthPercent => maxHealth > 0 ? (float)Health.Value / maxHealth : 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Health.Value = maxHealth;
        }
    }

    // Nur auf dem Server aufrufen (von Waffe oder Bombe).
    public void TakeDamage(int amount)
    {
        if (!IsServer || !IsSpawned || amount <= 0) return;
        if (Health.Value <= 0) return; // schon kaputt

        Health.Value = Mathf.Max(Health.Value - amount, 0);
        Debug.Log($"{name} nimmt {amount} Schaden, Leben: {Health.Value}/{maxHealth}");

        if (Health.Value == 0)
        {
            Break();
        }
    }

    private void Break()
    {
        // Collider sofort aus, sonst "sieht" die Prüfung das Teil noch.
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        NetworkObject.Despawn();

        // Danach prüfen, ob dadurch etwas in der Luft hängt.
        BuildBlock.CheckStability();
    }
}