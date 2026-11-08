using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Bomb : NetworkBehaviour
{
    [HideInInspector] public ulong throwerId = PlayerHealth.NoAttacker;
    [Header("Zünder")]
    [SerializeField] private float fuseTime = 3f;

    [Header("Wegschleudern")]
    public float radius;
    public float force;
    public LayerMask interactionMask;

    [Header("Schaden")]
    public float damageRadius = 10f;
    [SerializeField] private int maxPlayerDamage = 70;
    [SerializeField] private int maxStructureDamage = 120;

    [Header("Effekt")]
    public GameObject explosionPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(Explode), fuseTime);
        }
    }

    void Explode()
    {
        PushRigidbodies();
        ApplyDamage();

        ExplodeRpc();
        Invoke(nameof(End), 3f);
    }

    private void PushRigidbodies()
    {
        foreach (Collider obj in Physics.OverlapSphere(transform.position, radius, interactionMask))
        {
            Rigidbody rb = obj.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;

            rb.AddExplosionForce(force, transform.position, radius, 1, ForceMode.Impulse);
        }
    }

    private void ApplyDamage()
    {
        // Ein Objekt kann mehrere Collider haben (z. B. die Tür).
        // Pro Objekt merken wir uns nur die KLEINSTE Entfernung, damit es nur einmal Schaden bekommt.
        Dictionary<PlayerHealth, float> players = new Dictionary<PlayerHealth, float>();
        Dictionary<Destructible, float> structures = new Dictionary<Destructible, float>();

        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius,
            ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hits)
        {
            // Entfernung zum nächsten Punkt des Colliders, nicht zur Mitte.
            Vector3 closest = col.ClosestPointOnBounds(transform.position);
            float distance = Vector3.Distance(transform.position, closest);

            PlayerHealth player = col.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                KeepClosest(players, player, distance);
                continue;
            }

            Destructible structure = col.GetComponentInParent<Destructible>();
            if (structure != null)
            {
                KeepClosest(structures, structure, distance);
            }
        }

        // Erst NACH der Schleife Schaden verteilen. Würde ein Teil mitten in der
        // Schleife zerstört, hätten wir sonst Verweise auf gelöschte Objekte.
        foreach (var entry in players)
        {
            entry.Key.TakeDamage(CalculateDamage(maxPlayerDamage, entry.Value), throwerId);
        }
        foreach (var entry in structures)
        {
            entry.Key.TakeDamage(CalculateDamage(maxStructureDamage, entry.Value));
        }
    }

    private static void KeepClosest<T>(Dictionary<T, float> dict, T key, float distance)
    {
        if (!dict.TryGetValue(key, out float current) || distance < current)
        {
            dict[key] = distance;
        }
    }

    private int CalculateDamage(int maxDamage, float distance)
    {
        float factor = 1f - Mathf.Clamp01(distance / damageRadius);
        return Mathf.RoundToInt(maxDamage * factor);
    }

    void End()
    {
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExplodeRpc()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GetComponent<AudioSource>().Play();
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }
}