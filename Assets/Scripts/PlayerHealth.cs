using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerHealth : NetworkBehaviour
{
    public const ulong NoAttacker = ulong.MaxValue;

    public int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Aus der Map fallen")]
    [SerializeField] private float killHeight = -20f;        // unter dieser Höhe scheidet man aus
    [SerializeField] private float lastHitCreditTime = 5f;   // so lange zählt ein Treffer noch als Kill

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> kills = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float RespawnDelay => respawnDelay;
    public float DeathTime { get; private set; }

    private ulong lastAttackerId = NoAttacker;
    private float lastAttackTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentHealth.Value = maxHealth;

        isDead.OnValueChanged += OnDeadChanged;
        ApplyDeadState(isDead.Value);   // für Spieler, die später beitreten
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeadChanged;
    }

    // ------------------------------------------------ Server

    private void Update()
    {
        if (!IsServer || !IsSpawned || isDead.Value) return;

        if (transform.position.y < killHeight)
        {
            // Wurde man kurz vorher getroffen, bekommt der Angreifer den Kill.
            bool recentlyHit = Time.time - lastAttackTime < lastHitCreditTime;
            ulong attacker = recentlyHit ? lastAttackerId : NoAttacker;

            TakeDamage(currentHealth.Value, attacker);
        }
    }

    public void TakeDamage(int damage, ulong attackerId = NoAttacker)
    {
        if (!IsServer || isDead.Value || damage <= 0) return;
        if (GameManager.IsGameOver) return;   // nach dem Sieg kein Schaden mehr

        // Letzten Angreifer merken, für den Fall, dass man danach von der Map fällt.
        if (attackerId != NoAttacker && attackerId != OwnerClientId)
        {
            lastAttackerId = attackerId;
            lastAttackTime = Time.time;
        }

        currentHealth.Value = Mathf.Max(currentHealth.Value - damage, 0);
        Debug.Log($"Player {OwnerClientId} nimmt {damage} Schaden von {attackerId}. Leben: {currentHealth.Value}");

        if (currentHealth.Value == 0) Die(attackerId);
    }

    private void Die(ulong attackerId)
    {
        isDead.Value = true;

        // Kill für den Angreifer, aber nicht, wenn man sich selbst erwischt hat.
        if (attackerId != NoAttacker && attackerId != OwnerClientId)
        {
            PlayerHealth killer = GetPlayer(attackerId);
            if (killer != null)
            {
                killer.kills.Value++;
                Debug.Log($"Player {attackerId} hat Player {OwnerClientId} ausgeschaltet. Kills: {killer.kills.Value}");

                if (GameManager.Instance != null) GameManager.Instance.OnKill(killer);
            }
        }

        // Nach dem Tod zählt der alte Treffer nicht mehr.
        lastAttackerId = NoAttacker;

        Invoke(nameof(Respawn), respawnDelay);
    }

    public void Respawn()
    {
        if (!IsServer) return;

        CancelInvoke(nameof(Respawn));   // falls er von außen früher aufgerufen wird
        currentHealth.Value = maxHealth;
        RespawnRpc(SpawnPoint.GetRandomPosition((int)(OwnerClientId % 2)));

        // Kurz warten, damit der Teleport ankommt, bevor man wieder sichtbar wird.
        Invoke(nameof(Revive), 0.2f);
    }

    private void Revive()
    {
        isDead.Value = false;
    }

    // Spieler-Objekt zu einer Client-ID finden (nur auf dem Server).
    public static PlayerHealth GetPlayer(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
            && client.PlayerObject != null)
        {
            return client.PlayerObject.GetComponent<PlayerHealth>();
        }
        return null;
    }

    [Rpc(SendTo.Owner)]
    private void RespawnRpc(Vector3 spawnPosition)
    {
        CharacterController cc = GetComponent<CharacterController>();
        cc.enabled = false;
        GetComponent<NetworkTransform>().Teleport(spawnPosition, transform.rotation, transform.localScale);

        // Ist man noch "tot", bleibt er aus; eingeschaltet wird er beim Wiederbeleben.
        cc.enabled = !isDead.Value;
    }

    // ------------------------------------------------ Alle Rechner

    private void OnDeadChanged(bool oldValue, bool newValue)
    {
        ApplyDeadState(newValue);
    }

    private void ApplyDeadState(bool dead)
    {
        // Für alle: unsichtbar und nicht treffbar.
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = !dead;
        foreach (Canvas c in GetComponentsInChildren<Canvas>(true)) c.enabled = !dead;
        foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = !dead;

        if (dead) DeathTime = Time.time;

        UpdateControls();
    }

    // Steuerung nur, wenn man lebt UND die Runde läuft.
    public void UpdateControls()
    {
        if (!IsOwner) return;

        bool canPlay = !isDead.Value && !GameManager.IsGameOver;
        GetComponent<PlayerInput>().enabled = canPlay;
        GetComponent<FP_CharacterController>().enabled = canPlay;
    }
}