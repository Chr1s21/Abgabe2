using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Bomb : NetworkBehaviour
{
    public float radius;
    public float force;
    public LayerMask interactionMask;

    public GameObject explosionPrefab;

    public float damageRadius = 10f;
    public int damage = 30;

   public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(Explode), 3f);
        }
    }

    void Explode()
    {
        Collider[] objs = Physics.OverlapSphere(transform.position,radius,interactionMask);
        foreach(Collider obj in objs)
        {
            obj.GetComponent<Rigidbody>().AddExplosionForce(force,transform.position,radius,1,ForceMode.Impulse);
        }

        HashSet<PlayerHealth> hitPlayers = new HashSet<PlayerHealth>();
        foreach(Collider col in Physics.OverlapSphere(transform.position, damageRadius))
        {
            PlayerHealth playerHealth = col.gameObject.GetComponentInParent<PlayerHealth>();
            if(playerHealth != null && hitPlayers.Add(playerHealth))
            {
                playerHealth.TakeDamage(damage);
            }
        }

        ExplodeRpc();

        Invoke(nameof(End), 3f);
    }

    void End()
    {
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExplodeRpc()
    {
        Instantiate(explosionPrefab,transform.position,Quaternion.identity);
        GetComponent<AudioSource>().Play();
        GetComponent<Renderer>().enabled=false;
        GetComponent<Collider>().enabled=false;
    }
}
