using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum PartType { Floor, Wall, Ramp }

[RequireComponent(typeof(Rigidbody))]
public class BuildBlock : NetworkBehaviour
{
    public static readonly List<BuildBlock> All = new List<BuildBlock>();

    [SerializeField] private PartType type;
    [SerializeField] private float despawnAfterLoose = 10f;

    public PartType Type => type;

    public NetworkVariable<ulong> BuilderId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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


    private const float TouchMargin = 0.05f;

    public static void CheckStability()
    {
        List<BuildBlock> standing = new List<BuildBlock>();
        foreach (BuildBlock block in All)
        {
            if (block.IsSpawned && !block.IsLoose.Value) standing.Add(block);
        }

        HashSet<BuildBlock> supported = new HashSet<BuildBlock>();
        Queue<BuildBlock> queue = new Queue<BuildBlock>();

        foreach (BuildBlock block in standing)
        {
            if (block.TouchesGround())
            {
                supported.Add(block);
                queue.Enqueue(block);
            }
        }

        while (queue.Count > 0)
        {
            BuildBlock current = queue.Dequeue();
            foreach (BuildBlock neighbour in current.GetNeighbours())
            {
                if (supported.Add(neighbour))   // Add ist false, wenn schon drin
                {
                    queue.Enqueue(neighbour);
                }
            }
        }

        foreach (BuildBlock block in standing)
        {
            if (!supported.Contains(block)) block.Loosen();
        }
    }

    private Bounds GetBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (!col.enabled) continue;
            if (first) { bounds = col.bounds; first = false; }
            else bounds.Encapsulate(col.bounds);
        }
        return bounds;
    }

    private Collider[] OverlapAround(int layerMask)
    {
        Bounds b = GetBounds();
        return Physics.OverlapBox(b.center, b.extents + Vector3.one * TouchMargin,
            Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore);
    }

    private bool TouchesGround()
    {
        foreach (Collider col in OverlapAround(~0))
        {
            if (col.GetComponentInParent<BuildBlock>() != null) continue;

            if (col.GetComponentInParent<PlayerHealth>() != null) continue;
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic) continue;

            return true; // Level, Boden oder ein fester Gegenstand
        }
        return false;
    }

    private List<BuildBlock> GetNeighbours()
    {
        List<BuildBlock> result = new List<BuildBlock>();

        foreach (Collider col in OverlapAround(LayerMask.GetMask("Block")))
        {
            BuildBlock other = col.GetComponentInParent<BuildBlock>();
            if (other == null || other == this) continue;
            if (!other.IsSpawned || other.IsLoose.Value) continue;
            if (!result.Contains(other)) result.Add(other);
        }
        return result;
    }
}