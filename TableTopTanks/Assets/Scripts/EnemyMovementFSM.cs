using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementFSM : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private GameObject playerTank;
    [SerializeField] private float activationDistance = 50f;
    [SerializeField] private float rotateSpeed = 60f;
    [SerializeField] private float moveSpeed = 10f;

    // internal variables
    private Rigidbody rb;
    private NavMeshAgent agent;
    // no need for destroyed state; turret behaviour script handles enemy destruction
    private enum State { Idle, Flank, Chase }
    private State currentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentState = State.Idle;

        // configure NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = rotateSpeed;
        agent.acceleration = moveSpeed * 4f;

        // we drive the rigidbody; agent provides nextPosition/velocity
        agent.updatePosition = false;
        agent.updateRotation = true;

        // ensure agent internal position matches rigidbody so it can compute a path immediately
        agent.Warp(transform.position);
        agent.nextPosition = rb.position;

        // keep agent stopped until chase begins
        agent.isStopped = true;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                // stop agent path in idle to avoid sudden changes
                if (agent.hasPath)
                    agent.ResetPath();

                agent.isStopped = true;

                if (playerTank != null && Vector3.Distance(transform.position, playerTank.transform.position) < activationDistance)
                {
                    currentState = State.Chase;
                }
                break;
            case State.Chase:
                Chase();
                break;
            case State.Flank:
                Flank();
                break;
        }
    }

    // FixedUpdate to move the Rigidbody to follow the NavMeshAgent smoothly
    void FixedUpdate()
    {
        //sync rigidbody position with agent
        agent.nextPosition = rb.position;
        if (Quaternion.Angle(rb.rotation, agent.transform.rotation) < 1.0f)
        {

            Vector3 moveDir = transform.forward - Vector3.Cross(transform.forward, agent.velocity);
            rb.linearVelocity = moveDir.normalized * moveSpeed;
        }
        
    }

    private void Chase()
    {
        if (playerTank == null || agent == null) return;

        // ensure agent is on navmesh and in sync with rigidbody
        if (!agent.isOnNavMesh)
        {
            agent.Warp(rb.position);
            agent.nextPosition = rb.position;
        }
        else
        {
            // keep internal agent position in sync before asking for a path
            agent.nextPosition = rb.position;
        }

        agent.isStopped = false;
        agent.SetDestination(playerTank.transform.position);

        // transition back to idle if player out of range
        if (Vector3.Distance(transform.position, playerTank.transform.position) >= activationDistance)
        {
            currentState = State.Idle;
        }
    }

    private void Flank()
    {
        // Implement flank behaviour here later
    }
}
