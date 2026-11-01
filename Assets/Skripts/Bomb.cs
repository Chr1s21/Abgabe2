using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float radius;
    public float force;
    public LayerMask interactionMask;
    public GameObject explosionPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("Explode", 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Explode()
    {
        Debug.Log("Bomb exploded");
        Collider[] objs = Physics.OverlapSphere(transform.position, radius, interactionMask);
        foreach(Collider obj in objs)
        {
            obj.GetComponent<Rigidbody>().AddExplosionForce(force, transform.position, radius, 1f, ForceMode.Impulse);
        }

        GetComponent<AudioSource>().Play();
        Instantiate(explosionPrefab,transform.position,Quaternion.identity);
        
        Invoke("End",0.5f);
    }

    void End()
    {
        Destroy(gameObject);
    }
}
