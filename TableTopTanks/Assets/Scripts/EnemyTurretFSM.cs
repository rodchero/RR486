using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// This class controls the behaviour of a stationary enemy turret using a finite state machine (FSM)
public class EnemyTurretFSM : MonoBehaviour
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


    // internal variables
    float timer;
    enum State { Idle, Active, Destroyed }
    State currentState;
    bool canShoot;
    float distanceToPlayer;
    List<GameObject> currentShells;
    bool hasLOS = false;
    private LayerMask projectileLayer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canShoot = false;
        currentState = State.Idle;
        timer = 0.0f;
        playerTank = GameObject.FindGameObjectWithTag("Player");
        currentShells = new List<GameObject>();
        projectileLayer = LayerMask.GetMask("Projectile");
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

    private void Active()
    {
        if (playerTank == null)
        {
            currentState = State.Idle;
            return;
        }

        // adjust ray origin and target up by 1 unit so ray doesn't pass under player (had to find out the hard way)
        Vector3 origin = (turret != null ? turret.transform.position : transform.position) + Vector3.up * 1f;
        Vector3 target = playerTank.transform.position + Vector3.up * 1f;
        Vector3 toPlayer = target - origin;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer < Mathf.Epsilon)
        {
            hasLOS = false;
        }
        else
        {
            Vector3 dirToPlayer = toPlayer / distToPlayer;
            Ray ray = new Ray(origin, dirToPlayer);
            Debug.DrawRay(ray.origin, ray.direction * distToPlayer, Color.red);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, activationDistance))
            {
                // consider the hit as player if the hit collider belongs to the player root or its children
                if (hit.collider != null && (hit.collider.gameObject == playerTank || hit.collider.transform.IsChildOf(playerTank.transform) || hit.collider.CompareTag("Player")))
                {
                    hasLOS = true;
                }
                else
                {
                    hasLOS = false;
                }
            }
            else
            {
                hasLOS = false;
            }

            // rotate turret toward the adjusted direction (so aim lines up with the adjusted ray)
            Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
            turret.transform.rotation = Quaternion.RotateTowards(turret.transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

            if (hasLOS && (Quaternion.Angle(lookRotation, turret.transform.rotation) <= 2.0f))
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

}




