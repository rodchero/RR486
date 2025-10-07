using System.Collections.Generic;
using UnityEngine;

public class BasicFSM : MonoBehaviour
{
    public enum BasicEnemyState
    {
        Idle,
        Destroyed,
        Patrol,
        Fighting
    }

    [Header("FSM Parameters")]
    [SerializeField] private float enableDistance = 20f;
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float waypointTolerance = 1.0f;
    [SerializeField] private float enemySpeed = 10.0f; // New variable
    [SerializeField] private float patrolSpeed = 5.0f; // Will be set in Start()
    [SerializeField] private Material destroyedMaterial;
    [SerializeField] private GameObject smokeVFX;
    [SerializeField] private Transform playerTank;
    [SerializeField] private LayerMask playerTankLayer;

    private BasicEnemyState currentState = BasicEnemyState.Idle;
    private int currentWaypointIndex = 0;
    private bool isDestroyed = false;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        patrolSpeed = enemySpeed * 0.5f; // Ensure patrolSpeed is always half of enemySpeed
        if (playerTank == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTank = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDestroyed)
            return;

        float distanceToPlayer = playerTank != null ? Vector3.Distance(transform.position, playerTank.position) : Mathf.Infinity;

        // idle if far from player
        if (distanceToPlayer >= enableDistance)
        {
            currentState = BasicEnemyState.Idle;
        }

        switch (currentState)
        {
            // if idle and player approached, start patrolling
            case BasicEnemyState.Idle:
                if (distanceToPlayer <= enableDistance)
                {
                    currentState = BasicEnemyState.Patrol;
                }
                break;

            // if patrolling and player out of range, go idle
            case BasicEnemyState.Patrol:
                if (distanceToPlayer > enableDistance)
                {
                    currentState = BasicEnemyState.Idle;
                }
                else
                {
                    // Raycast to check line of sight to player
                    Vector3 directionToPlayer = (playerTank.position - transform.position).normalized;
                    Ray ray = new Ray(transform.position, directionToPlayer);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, enableDistance, playerTankLayer))
                    {
                        // If the raycast hits the player tank, switch to Fighting
                        if (hit.transform == playerTank)
                        {
                            currentState = BasicEnemyState.Fighting;
                            break;
                        }
                    }
                    Patrol();
                }
                break;

            case BasicEnemyState.Fighting:
                AvoidPlayerShells();    
                break;

            case BasicEnemyState.Destroyed:
                // Handled in TakeHit()
                break;
        }
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 toWaypoint = (targetWaypoint.position - transform.position);
        toWaypoint.y = 0f; // Ignore vertical difference for rotation

        if (toWaypoint.sqrMagnitude < 0.01f)
            return;

        // Calculate the desired rotation
        Quaternion targetRotation = Quaternion.LookRotation(toWaypoint.normalized, Vector3.up);

        // Smoothly rotate towards the target
        float rotationSpeed = 120f; // degrees per second, adjust as needed
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // Only move forward if facing (within 5 degrees) the waypoint
        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleToTarget < 5f)
        {
            transform.position += transform.forward * patrolSpeed * Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) < waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
    }

    // Call this method when the tank takes a hit
    public void TakeHit()
    {
        if (isDestroyed)
            return;

        currentState = BasicEnemyState.Destroyed;
        isDestroyed = true;

        // Change material
        if (rend != null && destroyedMaterial != null)
        {
            rend.material = destroyedMaterial;
        }

        // Spawn smoke VFX
        if (smokeVFX != null)
        {
            Instantiate(smokeVFX, transform.position, Quaternion.identity);
        }
    }

    private void AvoidPlayerShells()
    {
        GameObject[] shells = GameObject.FindGameObjectsWithTag("PlayerShell");
        Vector3 avoidanceDirection = Vector3.zero;
        float minTimeToImpact = float.MaxValue;
        float dangerDistance = 2.0f;
        float lookaheadTime = 2.0f;

        foreach (GameObject shell in shells)
        {
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            if (shellRb == null) continue;

            Vector3 shellPos = shell.transform.position;
            Vector3 shellVel = shellRb.linearVelocity;
            Vector3 toEnemy = transform.position - shellPos;

            float relativeSpeed = Vector3.Dot(shellVel.normalized, toEnemy.normalized) * shellVel.magnitude;
            if (relativeSpeed <= 0) continue;

            float timeToClosest = Vector3.Dot(toEnemy, shellVel.normalized) / shellVel.magnitude;
            if (timeToClosest < 0 || timeToClosest > lookaheadTime) continue;

            Vector3 closestPoint = shellPos + shellVel * timeToClosest;
            float distanceAtClosest = Vector3.Distance(closestPoint, transform.position);

            if (distanceAtClosest < dangerDistance && timeToClosest < minTimeToImpact)
            {
                Vector3 perp = Vector3.Cross(shellVel.normalized, Vector3.up).normalized;
                avoidanceDirection = perp;
                minTimeToImpact = timeToClosest;
            }
        }

        if (avoidanceDirection != Vector3.zero)
        {
            // Move at enemySpeed, not faster
            transform.position += avoidanceDirection * enemySpeed * Time.deltaTime;
        }
        else
        {
            // Optionally, pursue or attack the player here
        }
    }
}
