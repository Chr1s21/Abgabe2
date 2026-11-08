using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerControls : NetworkBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject hand;
    [SerializeField] private float throwForce;

    private BuildMode buildMode;

    void Start()
    {
        buildMode = GetComponent<BuildMode>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        ShootRpc(hand.transform.position, hand.transform.forward);
    }

    public void OnBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        BombRpc(hand.transform.position, hand.transform.forward);
    }

    [Rpc(SendTo.Server)]
    private void ShootRpc(Vector3 position, Vector3 direction)
    {
        if (!CanAct()) return;

        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);
        ball.GetComponent<Ball_Behaviour>().shooterId = OwnerClientId;
        ball.GetComponent<NetworkObject>().Spawn();
        ball.GetComponent<Rigidbody>().AddForce(direction * throwForce, ForceMode.Impulse);
    }

    [Rpc(SendTo.Server)]
    private void BombRpc(Vector3 position, Vector3 direction)
    {
        if (!CanAct()) return;

        GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);
        bomb.GetComponent<Bomb>().throwerId = OwnerClientId;
        bomb.GetComponent<NetworkObject>().Spawn();
        bomb.GetComponent<Rigidbody>().AddForce(direction * throwForce, ForceMode.Impulse);
    }

    // Server-Prüfung: Ausgeschiedene Spieler und Spieler nach Rundenende dürfen nichts werfen.
    private bool CanAct()
    {
        if (GameManager.IsGameOver) return false;
        if (TryGetComponent(out PlayerHealth health) && health.isDead.Value) return false;
        return true;
    }
}