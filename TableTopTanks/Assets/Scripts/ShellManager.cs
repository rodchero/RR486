using UnityEditor;
using UnityEngine;

// this class manages the lifetime and behaviour of a shell object. The Physics engine does the heavy lifting of movement and collision detection,
// but this script will destroy the shell after a certain number of bounces or a certain lifetime, or upon collision with a non-bouncy surface.
public class ShellManager : MonoBehaviour
{
    // Unity Parameters (need to be assigned in inspector)
    [Header("Unity Parameters")]
    [SerializeField] private int maxBounces = 1;
    [SerializeField] private float maxLifetime = 5.0f;

    // internal variables
    private int numBounces = 0;
    private GameObject shell;
    private float shellLifetime = 0.0f;
    private LayerMask bounceLayer;

    void Start()
    {
        // get a reference to the shell object
        shell = gameObject;
        bounceLayer = LayerMask.GetMask("BouncyWall");
    }

    // Update is called once per frame
    void Update()
    {
        // destroy shell after max bounces
        if (numBounces > maxBounces)
        {
            Destroy(shell);
        }   

        // destroy shell after max lifetime *using deltatimes
        shellLifetime += Time.deltaTime;
        if (shellLifetime > maxLifetime)
        {
            Destroy(shell);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Handle case of shell bouncing off a wall (bouncywall layer)
        // else destroy shell, damage logic handled by tank scripts
        if (((1 << collision.gameObject.layer) & bounceLayer.value) != 0)
        {
            numBounces++;
        }
        else
        {
            Destroy(shell);
        }
    }

}
