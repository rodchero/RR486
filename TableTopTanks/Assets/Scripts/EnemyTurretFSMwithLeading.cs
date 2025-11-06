using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// This class controls the behaviour of a stationary enemy turret using a finite state machine (FSM)
// This is a copy of EnemyTurretFSM with improved leading algorithm for moving targets, resulting in a more difficult enemy with better aim.
public class EnemyTurretFSMwithLeading : MonoBehaviour
{

    [Header("Turret Settings")]
    [SerializeField] private GameObject turret;
    [SerializeField] private GameObject playerTank;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject projectileSpawner;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float activationDistance = 50f;
    [SerializeField] private float rotateSpeed = 60f;
    [SerializeField] private int maxShells = 5;
    [SerializeField] private float reloadTime = 2f;

    [Header("Lead Settings")]
    [SerializeField] private float maxLeadTime = 3f;
    [SerializeField] private float fallbackLeadTime = 0.5f;

    [Header("Aim / LOS Settings")]
    [SerializeField] private float aimAngleThreshold = 8f;
    [SerializeField] private float leadSphereRadius = 0.8f;

    // internal variables
    float timer;
    enum State { Idle, Active, Destroyed }
    State currentState;
    bool canShoot;
    float distanceToPlayer;
    List<GameObject> currentShells;
    bool hasLOS = false;
    private LayerMask projectileLayer;
    private Rigidbody playerRb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canShoot = false;
        currentState = State.Idle;
        timer = 0.0f;
        playerTank = GameObject.FindGameObjectWithTag("Player");
        currentShells = new List<GameObject>();
        projectileLayer = LayerMask.GetMask("Projectile");
        playerTank = GameObject.FindGameObjectWithTag("Player");
        playerRb = playerTank != null ? playerTank.GetComponent<Rigidbody>() : null;
    }

    // Update is called once per frame
    void Update()
    {
        // get distance to player tank
        // if multiplayer, this will only consider the closest player tank
        distanceToPlayer = playerTank != null ? Vector3.Distance(transform.position, playerTank.transform.position) : Mathf.Infinity;

        // reload timer
        if (timer >= reloadTime)
        {
            canShoot = true;
        }
        else
        {
            timer += Time.deltaTime;
        }
        // FSM switch
        switch (currentState)
            {
            case State.Idle:
                Idle();
                //Debug.Log("Turret: Idle");
                break;
            case State.Active:
                Active();
                //Debug.Log("Turret: Active");
                break;
            case State.Destroyed:
                Destroyed();
                //Debug.Log("Turret: Destroyed");
                break;
        }
    }

    private void Idle()
    {
        // if player is within activation distance, switch to Active state
        if (distanceToPlayer < activationDistance)
        {
            currentState = State.Active;
        }
    }

    private bool IsPlayerCollider(Collider c)
    {
        return c != null && (c.gameObject == playerTank || c.transform.IsChildOf(playerTank.transform) || c.CompareTag("Player"));
    }

    private void Active()
    {
        if (playerTank == null)
        {
            currentState = State.Idle;
            return;
        }

        // adjust ray origin and target up by 1 unit so ray doesn't pass under player (had to find out the hard way)
        Vector3 origin = (turret != null ? turret.transform.position : transform.position) + Vector3.up * 1f;
        Vector3 playerPos = playerTank.transform.position + Vector3.up * 1f;

        // determine player velocity (world-space). If Rigidbody exists, use it; otherwise assume zero.
        Vector3 playerVel = Vector3.zero;
        if (playerRb == null && playerTank != null)
            playerRb = playerTank.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerVel = playerRb.linearVelocity;

        // compute intercept time using quadratic solution; fallback to distance/projectileSpeed
        float interceptTime = ComputeInterceptTime(origin, playerPos, playerVel, projectileSpeed);
        interceptTime = Mathf.Clamp(interceptTime, 0f, maxLeadTime);

        // predicted future position of player
        Vector3 predictedPos = playerPos + playerVel * interceptTime;

        // direction to predicted position
        Vector3 toPredicted = predictedPos - origin;
        float distToPredicted = toPredicted.magnitude;

        if (distToPredicted < Mathf.Epsilon)
        {
            hasLOS = false;
        }
        else
        {
            Vector3 dirToPredicted = toPredicted / distToPredicted;

            // Determine line of sight (LOS) to player, to ensure enemy doesn't shoot at wall and destroy itself accidentally

            // 1) Check LOS to the player's current position (so enemy still fires if it can see the player)
            bool hasLOSCurrent = false;
            Vector3 toPlayerCurrent = playerPos - origin;
            float distToPlayerCurrent = toPlayerCurrent.magnitude;
            if (distToPlayerCurrent > Mathf.Epsilon)
            {
                Ray rayToPlayer = new Ray(origin, toPlayerCurrent.normalized);
                Debug.DrawRay(rayToPlayer.origin, rayToPlayer.direction * Mathf.Min(distToPlayerCurrent, activationDistance), Color.green);
                RaycastHit hitPlayer;
                if (Physics.Raycast(rayToPlayer, out hitPlayer, activationDistance))
                {
                    if (IsPlayerCollider(hitPlayer.collider))
                        hasLOSCurrent = true;
                }
            }

            // 2) Check path to predicted position using a SphereCast so enemy accounts for player's collider size and motion
            bool clearPathToPredicted = false;
            Ray rayToPredicted = new Ray(origin, dirToPredicted);
            Debug.DrawRay(rayToPredicted.origin, rayToPredicted.direction * Mathf.Min(distToPredicted, activationDistance), Color.yellow);
            RaycastHit hitPred;
            float maxCheckDist = Mathf.Min(distToPredicted, activationDistance);
            // If spherecast hits nothing, path is clear; if it hits player, also clear; else blocked.
            if (!Physics.SphereCast(rayToPredicted, leadSphereRadius, out hitPred, maxCheckDist))
            {
                clearPathToPredicted = true;
            }
            else
            {
                if (IsPlayerCollider(hitPred.collider))
                    clearPathToPredicted = true;
            }

            // Consider LOS true if enemy has LOS to current player position OR the predicted path is clear
            hasLOS = hasLOSCurrent || clearPathToPredicted;

            // rotate turret toward the predicted direction
            Quaternion lookRotation = Quaternion.LookRotation(dirToPredicted);
            turret.transform.rotation = Quaternion.RotateTowards(turret.transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

            float angleToAim = Quaternion.Angle(lookRotation, turret.transform.rotation);

            // fire when:
            //  - enemy has "LOS" (current or predicted path)
            //  - aim is within threshold
            if (hasLOS && angleToAim <= aimAngleThreshold)
            {
                Shoot();
            }
        }

        // transition to idle state if player moves out of range
        if (distanceToPlayer >= activationDistance)
        {
            currentState = State.Idle;
        }
    }
    private void Destroyed()
    {
        // spawn explosion/smoke VFX then remove the enemy gameobject
        if (explosionVFX != null)
        {
            GameObject vfxInstance = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfxInstance, 2.0f);
        }

        // finally destroy this enemy GameObject
        Destroy(gameObject);

    }

    private void OnCollisionEnter(Collision collision)
    {
        // if hit by a projectile, switch to destroyed state
        if (((1 << collision.gameObject.layer) & projectileLayer.value) != 0)
        {
            currentState = State.Destroyed;
        }
    }

    private void Shoot()
    {
        currentShells.RemoveAll(item => item == null); // clean up null entries in current shells list (from destroyed shells)
        if (canShoot == true && currentShells.Count < maxShells)
        {
            // instantiate projectile and reset timer
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawner.transform.position, projectileSpawner.transform.rotation);
            timer = 0.0f;
            canShoot = false;
            // shoot shell forward from shell spawner position
            Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();
            projectileRB.AddForce(projectileSpawner.transform.forward * projectileSpeed, ForceMode.Impulse);
            // keep track of current shells
            currentShells.Add(projectile);
        }
    }

    /// Compute time to intercept a moving target from shooterPos with projectileSpeed.
    /// Solves ||(targetPos + v*t - shooterPos)|| = projectileSpeed * t for the smallest positive t.
    /// Falls back to distance/projectileSpeed when no valid positive root exists.
 
    private float ComputeInterceptTime(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVel, float projSpeed)
    {
        if (projSpeed <= 0.0f)
            return fallbackLeadTime;

        Vector3 r = targetPos - shooterPos;
        float a = Vector3.Dot(targetVel, targetVel) - (projSpeed * projSpeed);
        float b = 2f * Vector3.Dot(r, targetVel);
        float c = Vector3.Dot(r, r);

        // If a is nearly zero, fallback to linear solution: b t + c = 0 => t = -c / b
        if (Mathf.Abs(a) < 1e-6f)
        {
            if (Mathf.Abs(b) < 1e-6f)
                return Mathf.Min(fallbackLeadTime, maxLeadTime);
            if ((-c / b) > 0f)
                return Mathf.Min((-c / b), maxLeadTime);
            return Mathf.Min(fallbackLeadTime, maxLeadTime);
        }

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
        {
            // no solution: projectile too slow to intercept moving target; fallback to distance / projSpeed
            return Mathf.Min(r.magnitude / projSpeed, fallbackLeadTime);
        }

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtD) / (2f * a);
        float t2 = (-b - sqrtD) / (2f * a);

        // pick smallest positive time
        float t = float.PositiveInfinity;
        if (t1 > 0f && t1 < t) t = t1;
        if (t2 > 0f && t2 < t) t = t2;

        if (float.IsPositiveInfinity(t))
        {
            // no positive roots
            return Mathf.Min(r.magnitude / projSpeed, fallbackLeadTime);
        }

        return Mathf.Min(t, maxLeadTime);
    }

}
