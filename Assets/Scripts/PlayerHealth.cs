using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Max(currentHealth.Value - damage, 0);
        if (currentHealth.Value <= 0)
        {
            Respawn();
        }
        Debug.Log($"Player: {OwnerClientId} took {damage} damage. Current health: {currentHealth.Value}");
    }

    public void Respawn()
    {
        currentHealth.Value = maxHealth;
        Vector3 pos = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        RespawnRpc(pos);
    }
    [Rpc(SendTo.Owner)]
    private void RespawnRpc(Vector3 spawnPosition)
    {
        CharacterController cc = GetComponent<CharacterController>();
        cc.enabled = false;
        GetComponent<NetworkTransform>().Teleport(spawnPosition,transform.rotation,transform.localScale);
        cc.enabled = true;
    }
}
