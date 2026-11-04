using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum PartType { Floor, Wall, Ramp }

[RequireComponent(typeof(Rigidbody))]
public class BuildBlock : NetworkBehaviour
{
    // Alle gebauten Teile in der Welt (auf jedem Rechner).
    public static readonly List<BuildBlock> All = new List<BuildBlock>();

    [SerializeField] private PartType type;
    [SerializeField] private float despawnAfterLoose = 10f;

    public PartType Type => type;

    public NetworkVariable<ulong> BuilderId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Losgesprengt? Als NetworkVariable, damit auch Clients wissen,
    // dass der Platz wieder frei ist.
    public NetworkVariable<bool> IsLoose = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Rigidbody rb;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetList() => All.Clear();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
    }

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer && !IsLoose.Value) rb.isKinematic = true;
        base.OnNetworkPostSpawn();
    }

    public void Loosen()
    {
        if (!IsServer || IsLoose.Value) return;

        IsLoose.Value = true;
        rb.isKinematic = false;
        Invoke(nameof(DespawnSelf), despawnAfterLoose);
    }

    private void DespawnSelf()
    {
        if (IsSpawned) NetworkObject.Despawn();
    }

    // ------------------------------------------------ Slots

    // Welchen Platz belegt ein Teil dieses Typs in dieser Zelle mit dieser Drehung?
    public static (Vector3Int, PartType) GetSlot(PartType type, Vector3 cellCenter, Quaternion rotation, float gridSize)
    {
        float half = gridSize * 0.5f;
        Vector3 slotPosition;

        switch (type)
        {
            case PartType.Floor:
                slotPosition = cellCenter + Vector3.down * half;        // Unterseite der Zelle
                break;
            case PartType.Wall:
                slotPosition = cellCenter + rotation * Vector3.back * half; // Kante zum Spieler
                break;
            default:
                slotPosition = cellCenter;                               // Mitte
                break;
        }

        // In ganze Zahlen umrechnen, damit kleine Rundungsfehler nicht stören.
        Vector3Int key = Vector3Int.RoundToInt(slotPosition * 2f / gridSize);
        return (key, type);
    }

    public static bool IsSlotTaken(PartType type, Vector3 cellCenter, Quaternion rotation, float gridSize)
    {
        var wanted = GetSlot(type, cellCenter, rotation, gridSize);

        foreach (BuildBlock block in All)
        {
            if (block.IsLoose.Value) continue;

            var existing = GetSlot(block.Type, block.transform.position, block.transform.rotation, gridSize);
            if (existing == wanted) return true;
        }
        return false;
    }
}