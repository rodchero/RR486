using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using NUnit.Framework;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent (typeof(Rigidbody))]
public class TankController : NetworkIdentity
{
    // Unity Objects (need to be assigned in inspector)
    [Header("Unity Parameters")]

    public Camera playerCamera;
    
    [SerializeField] private GameObject tankTurret;
    [SerializeField] private GameObject shellSpawner;
    [SerializeField] private GameObject mineSpawner;
    [SerializeField] private GameObject shellObject;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private int maxShells = 5;
    [SerializeField] private float shellSpeed = 8.0f;

    // Movement Properties
    [Header("Movement Properties")]

    public float tankMoveSpeed = 8.0f;
    public float tankRotationSpeed = 180.0f;
    public float tankTurretRotationSpeed = 120.0f;

    // Internal variables
    private Vector2 moveVal;
    private Rigidbody rb;
    List<GameObject> currentShells;
    private LayerMask projectileLayer;
    private LayerMask groundLayer;
    private PlayerInput _pinput;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentShells = new List<GameObject>();
        projectileLayer = LayerMask.GetMask("Projectile");
        groundLayer = LayerMask.GetMask("Ground");

        // cache camera once from children
        playerCamera = GetComponentInChildren<Camera>(true);

        // ensure we have PlayerInput
        if (!_pinput)
            TryGetComponent(out _pinput);

        if (!isController)
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (_pinput != null) _pinput.enabled = false;
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(true);
            if (_pinput != null) _pinput.enabled = true;
        }
    }

    private void Awake()
    {
        if (!TryGetComponent(out _pinput))
            Debug.LogError("Couldn't get the Player Input Component", this);
    }


    void Update()
    {
        if (!isController) return;

        // Only update camera rotation for the local player's camera
        if (playerCamera != null && playerCamera.gameObject.activeSelf)
        {
            // prevent camera from rotating with tank (since it's a child object)
            playerCamera.transform.rotation = Quaternion.Euler(88, 0, 0);
        }
    }

    // FixedUpdate is called once per fixed time period 
    void FixedUpdate()
    {
        if (!isController) return;
        // move tank forward/backward

        Vector3 moveDirection = transform.forward * moveVal.y * tankMoveSpeed * Time.fixedDeltaTime;
        //rb.MovePosition(rb.position + moveDirection);
        //rb.AddForce(moveDirection.normalized * tankMoveSpeed, ForceMode.Acceleration);
        rb.linearVelocity = moveDirection.normalized * tankMoveSpeed;


        // rotate tank

        Quaternion turnDirection = Quaternion.Euler(0.0f, (moveVal.x * tankRotationSpeed * Time.fixedDeltaTime), 0.0f);
        rb.MoveRotation(rb.rotation * turnDirection);


        // rotate tank turret in direction of mouse

        // using old method of input here because the new inputsystem only returns mouse deltas (not what I need)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        // raycast mouse position to ground plane (table surface)
        Ray ray = playerCamera.ScreenPointToRay(mouseScreenPos);
        Vector3 aimPos = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            aimPos = hit.point;
        }
        // get rotation direction from turret to aim position
        Vector3 rotateDirection = aimPos - tankTurret.transform.position;
        rotateDirection.y = 0.0f;
        // don't rotate if direction is too small
        if (rotateDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotateDirection);
            // Only rotate around the Y axis
            Vector3 currentEuler = tankTurret.transform.rotation.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;
            float newY = Mathf.MoveTowardsAngle(currentEuler.y, targetEuler.y, tankTurretRotationSpeed * Time.fixedDeltaTime);
            tankTurret.transform.rotation = Quaternion.Euler(0, newY, 0);
        }
    }

    // updated input system event functions
    void OnMove(InputValue value)
    {
        moveVal = value.Get<Vector2>();
    }

    void OnShoot(InputValue value)
    {
        // clean up null entries in current shells list (from destroyed shells)
        currentShells.RemoveAll(item => item == null);
        if (currentShells.Count < maxShells)
        {
            // spawn shell at shell spawner position and rotation
            GameObject shell = Instantiate(shellObject, shellSpawner.transform.position, shellSpawner.transform.rotation);
            // add forward force to shell
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            shellRb.AddForce(shellSpawner.transform.forward * shellSpeed, ForceMode.Impulse);
            currentShells.Add(shell);
        }
    }
    // Placeholder for mine placement - not implemented, will be similar to shooting except with a different prefab and no force applied
    void OnPlaceMine(InputValue value)
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if hit by a projectile, destroy player tank
        if (((1 << collision.gameObject.layer) & projectileLayer.value) != 0)
        {
            if (explosionVFX != null)
            {
                GameObject vfxInstance = Instantiate(explosionVFX, transform.position, Quaternion.identity);
                Destroy(vfxInstance, 2.0f);
            }

            // finally destroy this player GameObject
            Destroy(gameObject);
        }
    }
    
}

