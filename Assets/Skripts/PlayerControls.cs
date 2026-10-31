/*using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public GameObject ballPrefab;
    public GameObject hand;
    public float throwForce;
    public Camera cam;
    public float maxDistance;
    public float pullDistance;
    public LayerMask interactionMask;
    private GameObject objInHand;
    private float force = 1f;
    public GameObject bombPrefab;
    private float timeStep;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeStep = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    void Update()
    {

        if(Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("T key pressed");
            if(Time.timeScale == 1f)
            {
                Time.timeScale = 0.1f;
                Time.fixedDeltaTime *= Time.timeScale;

            }
            else
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = timeStep;
            }
        }

        if(Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("F key pressed");
            GameObject bomb = Instantiate(bombPrefab,hand.transform.position,Quaternion.identity);
            bomb.GetComponent<Rigidbody>().AddForce(hand.transform.forward * throwForce, ForceMode.Impulse);
        }


        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(objInHand == null)
            {
                Debug.Log("Mouse left");
            GameObject ball = Instantiate(ballPrefab,hand.transform.position,Quaternion.identity);
            ball.GetComponent<Rigidbody>().AddForce(hand.transform.forward * throwForce, ForceMode.Impulse);
            }
            else
            {
                objInHand.GetComponent<Rigidbody>().isKinematic = false;
                objInHand.transform.parent = null;
                objInHand.GetComponent<Rigidbody>().AddForce(hand.transform.forward * throwForce, ForceMode.Impulse);
                objInHand = null;

            }             
            
        }

        
        if(Mouse.current.rightButton.isPressed && objInHand == null)
        {
            Debug.Log("Mouse right");
            Debug.DrawLine(cam.transform.position,cam.transform.position + cam.transform.forward * 10);
            RaycastHit hit;
            
            if(Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, maxDistance, interactionMask))
            {
                
                objInHand = hit.transform.gameObject;
                objInHand.transform.position = hand.transform.position;
                objInHand.transform.parent = hand.transform;
                objInHand.GetComponent<Rigidbody>().isKinematic = true;
            }

             if(Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, pullDistance, interactionMask))
            {
                
                hit.collider.gameObject.GetComponent<Rigidbody>().AddForce(-cam.transform.forward * force, ForceMode.Impulse);
           
            }



        }
    }
}*/