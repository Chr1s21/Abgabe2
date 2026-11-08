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

        // ------------------------------------------------ Stabilität

    private const float TouchMargin = 0.05f;

    // Wird auf dem Server aufgerufen, nachdem irgendetwas entfernt wurde.
    public static void CheckStability()
    {
        // 1. Alle noch festen Teile sammeln.
        List<BuildBlock> standing = new List<BuildBlock>();
        foreach (BuildBlock block in All)
        {
            if (block.IsSpawned && !block.IsLoose.Value) standing.Add(block);
        }

        // 2. Startpunkte: alle Teile, die den Boden/das Level berühren.
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

        // 3. Von dort aus alle verbundenen Nachbarn besuchen.
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

        // 4. Alles, was nicht erreicht wurde, hängt in der Luft.
        foreach (BuildBlock block in standing)
        {
            if (!supported.Contains(block)) block.Loosen();
        }
    }

    // Box um alle Collider dieses Teils.
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
            // Andere Bauteile zählen nicht als Boden.
            if (col.GetComponentInParent<BuildBlock>() != null) continue;

            // Spieler, Bälle und Bomben tragen kein Bauwerk.
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