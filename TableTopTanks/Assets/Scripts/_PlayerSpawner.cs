// using UnityEngine;
// using UnityEngine.AI;
// using PurrNet;

// public class PlayerSpawner : NetworkBehaviour
// {

//     [SerializeField] private GameObject playerPrefab;
//     private NetworkManager nm;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         nm = FindFirstObjectByType<NetworkManager>();

//         nm.onPlayerJoined += SpawnPlayer;
//     }

//     [ServerRpc]
//     public void SpawnPlayer(PlayerID id, bool reconnect, bool asServer)
//     {
//         if (!asServer && !reconnect)
//         {
//             SpawnPlayerLocal(id);
//         }
//     }

//     [TargetRpc(bufferLast:true)]
//     public void SpawnPlayerLocal(PlayerID id)
//     {
//         GameObject p = Instantiate(playerPrefab, transform.position, Quaternion.identity);
//         p.GetComponent<NetworkIdentity>().GiveOwnership(id);
//     }

    

// }
