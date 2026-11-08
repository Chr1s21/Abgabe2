using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Weapon : NetworkBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform muzzle;   // z. B. die Hand

    [Header("Werte")]
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 5f;          // Schüsse pro Sekunde
    [SerializeField] private int playerDamage = 20;
    [SerializeField] private int structureDamage = 25;
    [SerializeField] private float hitForce = 5f;          // Stoß für Physikobjekte

    [Header("Effekt")]
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private float tracerDuration = 0.08f;

    private BuildMode buildMode;
    private bool triggerHeld;
    private float nextFireTime;        // Client: wann darf ich wieder schießen
    private float serverNextFireTime;  // Server: Kontrolle gegen Schnellfeuer-Cheats

    public override void OnNetworkSpawn()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        buildMode = GetComponent<BuildMode>();
    }

    // ------------------------------------------------ Client

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed) triggerHeld = true;
        else if (context.canceled) triggerHeld = false;
    }

    private void Update()
    {
        if (!IsOwner || !triggerHeld) return;
        if (buildMode != null && buildMode.IsBuilding) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / fireRate;
        FireRpc(cam.transform.position, cam.transform.forward, muzzle.position);
    }

    // ------------------------------------------------ Server

    [Rpc(SendTo.Server)]
    private void FireRpc(Vector3 origin, Vector3 direction, Vector3 muzzlePosition)
    {
        // Feuerrate auch hier prüfen, mit etwas Spielraum für Netzwerkschwankungen.
        if (Time.time < serverNextFireTime) return;
        serverNextFireTime = Time.time + 0.8f / fireRate;

        // Der Schuss muss ungefähr beim Spieler starten.
        if (Vector3.Distance(origin, transform.position) > 5f) return;
        if (Vector3.Distance(muzzlePosition, transform.position) > 5f) return;
        direction.Normalize();

        Vector3 endPoint = origin + direction * range;

        if (TryGetHit(origin, direction, out RaycastHit hit))
        {
            endPoint = hit.point;
            ApplyHit(hit, direction);
        }

        ShotEffectRpc(muzzlePosition, endPoint);
    }

    private bool TryGetHit(Vector3 origin, Vector3 direction, out RaycastHit closest)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, ~0, QueryTriggerInteraction.Ignore);

        closest = default;
        float best = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // Sich selbst nicht treffen.
            if (hit.collider.transform.IsChildOf(transform)) continue;

            if (hit.distance < best)
            {
                best = hit.distance;
                closest = hit;
            }
        }
        if (best < float.MaxValue)
            Debug.Log($"Treffer: {closest.collider.name} ({closest.collider.GetType().Name})");
        else
            Debug.Log("Kein Treffer");
        return best < float.MaxValue;
    }

    private void ApplyHit(RaycastHit hit, Vector3 direction)
    {
        PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(playerDamage);
            return;
        }

        Destructible destructible = hit.collider.GetComponentInParent<Destructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(structureDamage);
        }

        // Bälle, Bomben, losgesprengte Teile anschubsen.
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(direction * hitForce, hit.point, ForceMode.Impulse);
        }
    }

    // ------------------------------------------------ Effekt (bei allen)

    [Rpc(SendTo.ClientsAndHost)]
    private void ShotEffectRpc(Vector3 from, Vector3 to)
    {
        GameObject tracer = new GameObject("Tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 0.03f;
        line.endWidth = 0.03f;
        line.material = tracerMaterial;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Destroy(tracer, tracerDuration);
    }

    // ------------------------------------------------ Fadenkreuz

    private void OnGUI()
    {
        if (!IsSpawned || !IsOwner) return;

        float size = 4f;
        GUI.DrawTexture(new Rect(Screen.width / 2f - size / 2f, Screen.height / 2f - size / 2f, size, size),
            Texture2D.whiteTexture);
    }
}