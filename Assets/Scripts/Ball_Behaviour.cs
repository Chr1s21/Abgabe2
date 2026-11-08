using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class Ball_Behaviour : NetworkBehaviour
{
    public int damage = 10;                       // gegen Spieler
    [SerializeField] private int structureDamage = 25;   // gegen Blöcke und Gegenstände
    [SerializeField] private float minImpactSpeed = 3f;  // langsam rollende Bälle machen keinen Schaden
    [SerializeField] private float lifetime = 2f;   // Sekunden bis zum Verschwinden
    [HideInInspector] public ulong shooterId;

    private bool hasHit;   // nur einmal Schaden machen

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(DestroyBallAfterSeconds(lifetime));
        }
    }

    private IEnumerator DestroyBallAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    // Spieler: über den Trigger am Body
    void OnTriggerEnter(Collider collider)
    {
        if (!IsServer || !IsSpawned || hasHit) return;

        PlayerHealth playerHealth = collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.OwnerClientId == shooterId) return;

        hasHit = true;
        playerHealth.TakeDamage(damage);
        NetworkObject.Despawn();
    }

    // Blöcke und Gegenstände: über eine echte Kollision
    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !IsSpawned || hasHit) return;

        Destructible destructible = collision.collider.GetComponentInParent<Destructible>();
        if (destructible == null) return;   // Boden, Level usw.: einfach abprallen

        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        hasHit = true;
        destructible.TakeDamage(structureDamage);
        NetworkObject.Despawn();
    }
}