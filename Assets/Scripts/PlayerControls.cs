using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerControls : NetworkBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject hand;
    [SerializeField] private Camera cam;
    [SerializeField] private float throwForce;

    private BuildMode buildMode;

    void Start()
    {
        buildMode = GetComponent<BuildMode>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        Vector3 start = hand.transform.position;
        ShootRpc(start, GetAimDirection(start));
    }

    public void OnBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        Vector3 start = hand.transform.position;
        BombRpc(start, GetAimDirection(start));
    }

    // Richtung von der Mündung zu dem Punkt, auf den das Fadenkreuz zeigt.
    private Vector3 GetAimDirection(Vector3 from)
    {
        int mask = ~LayerMask.GetMask("Player");   // sich selbst nicht treffen
        Vector3 target;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit,
                200f, mask, QueryTriggerInteraction.Ignore))
        {
            target = hit.point;
        }
        else
        {
            target = cam.transform.position + cam.transform.forward * 200f;   // in den Himmel
        }

        return (target - from).normalized;
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