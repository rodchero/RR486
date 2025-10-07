using UnityEditor;
using UnityEngine;

public class ShellManager : MonoBehaviour
{
    // Unity Parameters (need to be assigned in inspector)
    [Header("Unity Parameters")]
    [SerializeField] private int maxBounces = 1;
    [SerializeField] private float maxLifetime = 5.0f;
    [SerializeField] public LayerMask bounceLayer;

    // internal variables
    private int numBounces = 0;
    private GameObject shell;
    private float shellLifetime = 0.0f;

    void Start()
    {
        // get a reference to the shell object
        shell = gameObject;
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
        // Handle case of shell colliding with a tank (enemy or friendly)
        //TODO

        // Handle case of shell colliding with a wall
        if (((1 << collision.gameObject.layer) & bounceLayer.value) != 0)
        {
            numBounces++;
        }
        else
        {
            Destroy(shell, 0.1f);
        }
    }

}
