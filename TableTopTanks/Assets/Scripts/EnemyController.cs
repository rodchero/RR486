using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // this class is designed to be inherited by specific enemy types

    [SerializeField] private LayerMask projectileLayer;


    // internal variables
    enum State
    {
        Idle,
        Destroyed,
        Patrol,
        Fighting
    }
    private State currentState = State.Idle;
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & projectileLayer.value) != 0)
        {
            // handle being hit by a shell
            currentState = State.Destroyed;
            // change color of tank and make smoke vfx
        }
    }

    private void Shoot()
    {

    }

    private void PlaceMine()
    {

    }
}
