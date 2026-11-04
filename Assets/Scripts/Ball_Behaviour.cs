using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class Ball_Behaviour : NetworkBehaviour
{
    public int damage = 10;

    [HideInInspector] public ulong shooterId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(DestroyBallAfterSeconds(5f));
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

    void OnTriggerEnter(Collider collider)
    {
        if(!IsServer || !IsSpawned) return;

        PlayerHealth playerHealth = collider.gameObject.GetComponentInParent<PlayerHealth>();
        if(playerHealth==null || playerHealth.OwnerClientId == shooterId) return;

        playerHealth.TakeDamage(damage);
        if(IsSpawned) NetworkObject.Despawn();
        
    }

}
