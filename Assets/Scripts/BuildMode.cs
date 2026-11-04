using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[System.Serializable]
public class BuildPart
{
    public string name;
    public GameObject prefab;
    public GameObject ghostPrefab;
}

public class BuildMode : NetworkBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private Camera cam;

    [Header("Platzierung")]
    [SerializeField] private float buildDistance = 20f;
    [SerializeField] private LayerMask buildSurfaceMask;
    [SerializeField] private float gridSize = 4f;

    [Header("Vorschau")]
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);

    [Header("Bauteile")]
    [SerializeField] private BuildPart[] parts;

    private bool isBuilding;
    public bool IsBuilding => isBuilding;

    private int selectedIndex;
    private int rotationOffset;   // zusätzliche 90°-Schritte mit R

    private bool hasTarget;
    private RaycastHit currentHit;

    private GameObject ghost;
    private Renderer[] ghostRenderers;
    private Vector3 targetCell;
    private int targetRotationSteps;
    private bool canPlace;

    private PartType SelectedType => parts[selectedIndex].prefab.GetComponent<BuildBlock>().Type;

    // ------------------------------------------------ Netzwerk

    public override void OnNetworkSpawn()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
    }

    public override void OnNetworkDespawn()
    {
        DestroyGhost();
    }

    // ------------------------------------------------ Input

    public void OnToggleBuild(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        isBuilding = !isBuilding;

        if (isBuilding) CreateGhost();
        else DestroyGhost();
    }

    public void OnPlace(InputAction.CallbackContext context)
    {
        if (!context.performed || !isBuilding || !canPlace) return;

        PlaceRpc(targetCell, targetRotationSteps, selectedIndex);
    }

    public void OnRemove(InputAction.CallbackContext context)
    {
        if (!context.performed || !isBuilding || !hasTarget) return;

        BuildBlock block = currentHit.collider.GetComponentInParent<BuildBlock>();
        if (block == null) return;
        if (block.BuilderId.Value != OwnerClientId) return;

        RemoveRpc(block.NetworkObject);
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (!context.performed || !isBuilding) return;

        rotationOffset = (rotationOffset + 1) % 4;
    }

    public void OnSelectSlot(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (int.TryParse(context.control.name, out int number))
        {
            SelectPart(number - 1);
        }
    }

    public void OnCycleSlot(InputAction.CallbackContext context)
    {
        if (!context.performed || !isBuilding) return;

        float scroll = context.ReadValue<float>();
        if (scroll > 0) SelectPart(selectedIndex + 1);
        else if (scroll < 0) SelectPart(selectedIndex - 1);
    }

    private void SelectPart(int index)
    {
        int count = parts.Length;
        int newIndex = ((index % count) + count) % count;

        if (newIndex == selectedIndex) return;
        selectedIndex = newIndex;

        if (isBuilding)
        {
            DestroyGhost();
            CreateGhost();
        }
    }

    // ------------------------------------------------ Vorschau

    private void Update()
    {
        if (!IsOwner || !isBuilding) return;

        hasTarget = Physics.Raycast(cam.transform.position, cam.transform.forward, out currentHit,
            buildDistance, buildSurfaceMask, QueryTriggerInteraction.Ignore);

        UpdateGhost();
    }

    private void UpdateGhost()
    {
        if (!hasTarget)
        {
            ghost.SetActive(false);
            canPlace = false;
            return;
        }

        targetCell = CalculateTargetCell(currentHit);
        targetRotationSteps = CalculateRotationSteps();
        Quaternion rotation = StepsToRotation(targetRotationSteps);

        canPlace = !BuildBlock.IsSlotTaken(SelectedType, targetCell, rotation, gridSize);

        ghost.SetActive(true);
        ghost.transform.SetPositionAndRotation(targetCell, rotation);

        Color color = canPlace ? validColor : invalidColor;
        foreach (Renderer r in ghostRenderers) r.material.color = color;
    }

    private Vector3 CalculateTargetCell(RaycastHit hit)
    {
        // Ein kleines Stück von der getroffenen Fläche weg in Richtung Spieler,
        // damit wir in der Zelle VOR der Fläche landen und nicht darin.
        Vector3 point = hit.point + hit.normal * 0.1f;
        return SnapToCell(point);
    }

    private Vector3 SnapToCell(Vector3 p)
    {
        return new Vector3(Snap(p.x), Snap(p.y), Snap(p.z));
    }

    private float Snap(float value)
    {
        return Mathf.Floor(value / gridSize) * gridSize + gridSize * 0.5f;
    }

    private int CalculateRotationSteps()
    {
        // Blickrichtung des Spielers auf 0/90/180/270 Grad runden, plus Drehung mit R.
        int playerSteps = Mathf.RoundToInt(transform.eulerAngles.y / 90f);
        return (playerSteps + rotationOffset) % 4;
    }

    private static Quaternion StepsToRotation(int steps)
    {
        return Quaternion.Euler(0f, steps * 90f, 0f);
    }

    private void CreateGhost()
    {
        ghost = Instantiate(parts[selectedIndex].ghostPrefab);
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();

        foreach (Collider col in ghost.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
            Destroy(col);
        }
    }

    private void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        canPlace = false;
    }

    // ------------------------------------------------ Server

    [Rpc(SendTo.Server)]
    private void PlaceRpc(Vector3 cellCenter, int rotationSteps, int partIndex)
    {
        if (partIndex < 0 || partIndex >= parts.Length) return;
        if (rotationSteps < 0 || rotationSteps > 3) return;

        // Sicherstellen, dass die Position wirklich im Raster liegt.
        cellCenter = SnapToCell(cellCenter);
        if (Vector3.Distance(transform.position, cellCenter) > buildDistance + gridSize * 2f) return;

        Quaternion rotation = StepsToRotation(rotationSteps);
        PartType type = parts[partIndex].prefab.GetComponent<BuildBlock>().Type;
        if (BuildBlock.IsSlotTaken(type, cellCenter, rotation, gridSize)) return;

        GameObject block = Instantiate(parts[partIndex].prefab, cellCenter, rotation);
        block.GetComponent<BuildBlock>().BuilderId.Value = OwnerClientId;
        block.GetComponent<NetworkObject>().Spawn();
    }

    [Rpc(SendTo.Server)]
    private void RemoveRpc(NetworkObjectReference blockReference)
    {
        if (!blockReference.TryGet(out NetworkObject blockObject)) return;
        if (!blockObject.TryGetComponent(out BuildBlock block)) return;
        if (block.BuilderId.Value != OwnerClientId) return;
        if (Vector3.Distance(transform.position, blockObject.transform.position) > buildDistance + gridSize * 2f) return;

        blockObject.Despawn();
    }
}