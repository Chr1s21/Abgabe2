using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerControls : NetworkBehaviour
{
    public GameObject ballPrefab;
    public GameObject hand;
    public Camera cam;
    public float throwForce;
    public float maxDistance;
    private BuildMode buildMode;

    public LayerMask interactionMask;
    [SerializeField]
    private GameObject bombPrefab;
    private GameObject objInHand;
  



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buildMode = GetComponent<BuildMode>();
    }

    void FixedUpdate()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
       
    }
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        if (objInHand == null)
        {
           ShootRpc(hand.transform.position, hand.transform.forward);
        }
        else
        {
            objInHand.GetComponent<Rigidbody>().isKinematic = false;
            objInHand.transform.parent = null;
            objInHand.GetComponent<Rigidbody>().AddForce(hand.transform.forward * throwForce, ForceMode.Impulse);
            objInHand = null;

        }
    }


    public void OnBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || buildMode.IsBuilding) return;

        BombRpc(hand.transform.position, hand.transform.forward);
    }

    [Rpc(SendTo.Server)]
    private void ShootRpc(Vector3 position, Vector3 direction)
    {
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);
        ball.GetComponent<Ball_Behaviour>().shooterId = OwnerClientId;
        ball.GetComponent<NetworkObject>().Spawn();
        
        ball.GetComponent<Rigidbody>().AddForce(direction * throwForce, ForceMode.Impulse);
    }

    [Rpc(SendTo.Server)]
    private void BombRpc(Vector3 position, Vector3 direction)
    {
        GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);
        bomb.GetComponent<NetworkObject>().Spawn();
        bomb.GetComponent<Rigidbody>().AddForce(direction * throwForce, ForceMode.Impulse);     
    }
}
