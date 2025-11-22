using System.Collections.Generic;
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

    [Header("Flank Settings")]
    [SerializeField] private float flankDistance = 30f;           // lateral distance from player to aim for
    [SerializeField] private float flankBehindFactor = 0.5f;      // how far behind the player the flank point should be (fraction of flankDistance)
    [SerializeField] private float flankTimeout = 5f;             // abort flank after this many seconds and resume chase
    [SerializeField] private float navSampleRadius = 4f;          // radius to sample NavMesh around the computed flank point

    // internal variables
    private Rigidbody rb;
    private NavMeshAgent agent;
    // no need for destroyed state; turret behaviour script handles enemy destruction
    private enum State { Idle, Flank, Chase }
    private State currentState;
    private float playerCheckTimeout = 0f;

    // flank runtime state
    private Vector3 flankTarget;
    private float flankStartTime;
    private bool flankInitialized;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTank = FindNearestPlayer();
        rb = GetComponent<Rigidbody>();
        currentState = State.Idle;

        // configure NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = rotateSpeed;
        agent.stoppingDistance = 0.0f;
        agent.acceleration = moveSpeed * 4f;

        // we drive the rigidbody; agent provides nextPosition/velocity
        agent.updatePosition = false;
        agent.updateRotation = true;

        // ensure agent internal position matches rigidbody so it can compute a path immediately
        agent.Warp(transform.position);
        agent.nextPosition = rb.position;

        // keep agent stopped until chase begins
        agent.isStopped = true;

        // flank setup
        flankInitialized = false;
    }

    // Update is called once per frame
    void Update()
    {
        playerCheckTimeout += Time.deltaTime;
        if (playerCheckTimeout >= 2.0f)
        {
            playerTank = FindNearestPlayer();
            playerCheckTimeout = 0;
        }
        switch (currentState)
        {
            case State.Idle:
                //Debug.Log("Movement: Idle");
                // stop agent path in idle to avoid sudden changes
                if (agent.hasPath)
                    agent.ResetPath();

                agent.isStopped = true;

                if (playerTank != null && Vector3.Distance(transform.position, playerTank.transform.position) < activationDistance)
                {
                    // choose randomly between chase and flank to add some variety
                    if (UnityEngine.Random.value >= 0.5f)
                    {
                        currentState = State.Chase;
                    }
                    else
                    {
                        currentState = State.Flank;
                    }
                }

                break;
            case State.Chase:
                // reset any pending flank state when entering chase
                flankInitialized = false;
                //Debug.Log("Movement: Chasing");
                Chase();
                break;
            case State.Flank:
                //Debug.Log("Movement: Flanking");
                Flank();
                break;
        }
    }

    // FixedUpdate to move the Rigidbody to follow the NavMeshAgent smoothly
    void FixedUpdate()
    {
        //sync rigidbody position with agent
        agent.nextPosition = rb.position;

        // move if facing desired rotation, rotation handled by navmesh agent
        if ((Quaternion.Angle(rb.rotation, agent.transform.rotation) < 1.0f) && currentState != State.Idle)
        {

            Vector3 moveDir = transform.forward; // - Vector3.Cross(transform.forward, agent.velocity);
            rb.linearVelocity = moveDir.normalized * moveSpeed;
        }
        else
        {
            // slow down while turning
            rb.linearVelocity *= 0.5f;
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
        if (playerTank == null || agent == null) return;

        // abort flank if player moved out of activation range
        if (Vector3.Distance(transform.position, playerTank.transform.position) >= activationDistance)
        {
            flankInitialized = false;
            currentState = State.Idle;
            return;
        }

        // initialize flank target only once per flank entry
        if (!flankInitialized)
        {
            // choose flank side randomly
            int side = (Random.value < 0.5f) ? -1 : 1;

            Vector3 playerPos = playerTank.transform.position;
            Vector3 playerForward = playerTank.transform.forward;
            Vector3 playerRight = playerTank.transform.right;

            // compute a point to the side and slightly behind the player
            Vector3 desired = playerPos
                              + playerRight * (side * flankDistance)
                              + playerForward * (-flankDistance * flankBehindFactor);

            // try to find a nearby valid point on the NavMesh
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
            {
                flankTarget = hit.position;
            }
            else
            {
                // fallback: clamp to a point offset from current position towards desired
                Vector3 dir = (desired - transform.position).normalized;
                flankTarget = transform.position + dir * Mathf.Min(flankDistance, 20f);
            }

            agent.isStopped = false;
            agent.SetDestination(flankTarget);
            flankStartTime = Time.time;
            flankInitialized = true;
        }

        // if path is pending, just wait
        if (agent.pathPending)
            return;

        // if reached flank target or timeout, resume chase
        bool reached = (agent.hasPath == false && agent.remainingDistance <= agent.stoppingDistance + 1.0f)
                       || (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 1.0f);

        if (reached || (Time.time - flankStartTime) > flankTimeout || agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            flankInitialized = false;
            currentState = State.Chase;
            return;
        }
    }

    private GameObject FindNearestPlayer()
    {
        GameObject[] playerList = GameObject.FindGameObjectsWithTag("Player");
        Vector3 thisPos = transform.position;
        float closest = float.PositiveInfinity;
        GameObject closestPlayer = null;

        for (int i = 0; i < playerList.Length; i++)
        {
            float distance = Vector3.Distance(thisPos, playerList[i].transform.position);
            if (distance < closest)
            {
                closest = distance;
                closestPlayer = playerList[i];
            }
        }
        if (closestPlayer != null) Debug.Log("Movement: Found player to target");
        return closestPlayer;
    }
}
