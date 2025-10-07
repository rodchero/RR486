using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent (typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    // Unity Objects (need to be assigned in inspector)
    [Header("Unity Parameters")]

    public Camera mainCamera;
    public LayerMask groundLayer;
    [SerializeField] private GameObject tankTurret;
    [SerializeField] private GameObject shellSpawner;
    [SerializeField] private GameObject mineSpawner;
    [SerializeField] private GameObject shellObject;
    [SerializeField] private int maxShells = 5;

    // Movement Properties
    [Header("Movement Properties")]

    public float tankMoveSpeed = 8.0f;
    public float tankRotationSpeed = 180.0f;
    public float tankTurretRotationSpeed = 120.0f;

    // Internal variables
    private Vector2 moveVal;
    private Rigidbody rb;
    private int curShells = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate is called once per fixed time period 
    void FixedUpdate()
    {
        // move tank forward/backward

        Vector3 moveDirection = transform.forward * moveVal.y * tankMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveDirection);
        //rb.AddForce(moveDirection.normalized * tankMoveSpeed, ForceMode.Acceleration);


        // rotate tank

        Quaternion turnDirection = Quaternion.Euler(0.0f, (moveVal.x * tankRotationSpeed * Time.fixedDeltaTime), 0.0f);
        rb.MoveRotation(rb.rotation * turnDirection);


        // rotate tank turret in direction of mouse

        // using old method of input here because the new inputsystem onlu returns mouse deltas (not what I need)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        // raycast mouse position to ground plane (table surface)
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
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



    void LateUpdate()
    {
        // camera follows tank from above
        Vector3 cameraOffset = new Vector3(0.0f, 40.0f, 0.0f);
        mainCamera.transform.position = transform.position + cameraOffset;
    }

    void Update()
    {
        // keep track of current shells (destroyed shells should decrement this value)
        GameObject[] shells = GameObject.FindGameObjectsWithTag("PlayerShell");
        curShells = shells.Length;

    }


    void OnMove(InputValue value)
    {
        moveVal = value.Get<Vector2>();
    }

    void OnShoot(InputValue value)
    {
        if ( curShells < maxShells )
        {
            // spawn shell at shell spawner position and rotation
            GameObject shell = Instantiate(shellObject, shellSpawner.transform.position, shellSpawner.transform.rotation);
            // add forward force to shell
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            shellRb.AddForce(shellSpawner.transform.forward * 10.0f, ForceMode.Impulse);
        }
        

    }

    void OnPlaceMine(InputValue value)
    {
    }

}

