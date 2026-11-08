using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int killsToWin = 1;
    [SerializeField] private float restartDelay = 5f;

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> winnerId = new NetworkVariable<ulong>(
        PlayerHealth.NoAttacker, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Serverzeit, zu der die neue Runde startet (für den Countdown im HUD).
    public NetworkVariable<double> restartAt = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int KillsToWin => killsToWin;
    public static bool IsGameOver => Instance != null && Instance.isGameOver.Value;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        isGameOver.OnValueChanged += OnGameOverChanged;
    }

    public override void OnNetworkDespawn()
    {
        isGameOver.OnValueChanged -= OnGameOverChanged;
    }

    // ------------------------------------------------ Server

    // Wird von PlayerHealth aufgerufen, wenn jemand einen Kill bekommt.
    public void OnKill(PlayerHealth killer)
    {
        if (!IsServer || isGameOver.Value) return;

        if (killer.kills.Value >= killsToWin)
        {
            EndRound(killer.OwnerClientId);
        }
    }

    private void EndRound(ulong winner)
    {
        winnerId.Value = winner;
        restartAt.Value = NetworkManager.ServerTime.Time + restartDelay;
        isGameOver.Value = true;

        Debug.Log($"Player {winner} hat gewonnen! Neue Runde in {restartDelay} s");
        Invoke(nameof(StartNewRound), restartDelay);
    }

    private void StartNewRound()
    {
        // Alle gebauten Teile entfernen.
        // Kopie der Liste, weil Despawn Teile aus der Original-Liste löscht.
        foreach (BuildBlock block in new List<BuildBlock>(BuildBlock.All))
        {
            if (block.IsSpawned) block.NetworkObject.Despawn();
        }

        // Alle Spieler zurücksetzen.
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            PlayerHealth player = client.PlayerObject.GetComponent<PlayerHealth>();
            player.kills.Value = 0;
            player.Respawn();
        }

        winnerId.Value = PlayerHealth.NoAttacker;
        isGameOver.Value = false;
        Debug.Log("Neue Runde gestartet");
    }

    // ------------------------------------------------ Alle Rechner

    private void OnGameOverChanged(bool oldValue, bool newValue)
    {
        // Eigene Steuerung sperren bzw. wieder freigeben.
        NetworkObject localPlayer = NetworkManager.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            localPlayer.GetComponent<PlayerHealth>().UpdateControls();
        }
    }
}