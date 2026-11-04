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

    public LayerMask interactionMask;
    public float pullForce;
    [SerializeField]
    private GameObject bombPrefab;
    private GameObject objInHand;
    private float timeStep;
    private bool gravityGunActive;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeStep = Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (gravityGunActive && objInHand == null)
        {
            Debug.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * 10);
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, maxDistance, interactionMask))
            {
                if (Vector3.Distance(hand.transform.position, hit.transform.position) < 1)
                {
                    objInHand = hit.transform.gameObject;
                    objInHand.transform.position = hand.transform.position;
                    objInHand.transform.parent = hand.transform;
                    objInHand.GetComponent<Rigidbody>().isKinematic = true;
                }
                else
                {
                    Vector3 pullDirection = (cam.transform.position - hit.point).normalized;
                    hit.transform.GetComponent<Rigidbody>().AddForce(pullDirection * pullForce, ForceMode.Force);
                }
            }
        }
    }
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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

    public void OnGravityGun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            gravityGunActive = true;
        }
        else if (context.canceled)
        {
            gravityGunActive = false;
        }
    }

    public void OnBomb(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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
