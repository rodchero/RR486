using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent (typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    // Unity Objects
    private Rigidbody rb;
    public Camera cam;
    public GameObject tankTurret;
    public LayerMask groundLayer;

    // Movement Properties
    public float tankMoveSpeed = 10.0f;
    public float tankRotationSpeed = 120.0f;
    public float tankTurretRotationSpeed = 60.0f;

    // Internal variables
    private Vector2 mouseVal;
    private Vector2 moveVal;


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
        // rotate tank in direction of movement
        Quaternion turnDirection = Quaternion.Euler(0.0f, (moveVal.x * tankRotationSpeed * Time.fixedDeltaTime), 0.0f);
        rb.MoveRotation(rb.rotation * turnDirection);
        // rotate tank turret in direction of mouse
        Ray ray = cam.ScreenPointToRay(mouseVal);
        Vector3 aimPos = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            aimPos = hit.point;
        }
        Vector3 rotateDirection = aimPos - tankTurret.transform.position;
        rotateDirection.y = 0.0f;
        if (rotateDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(rotateDirection);
            tankTurret.transform.rotation *= Quaternion.Euler(0.0f, lookRotation.eulerAngles.y * tankTurretRotationSpeed * Time.fixedDeltaTime, 0.0f);
        }
        
        //TS does not work :(



    }

    void OnMove(InputValue value)
    {
        moveVal = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        mouseVal = value.Get<Vector2>();
    }
}

