using UnityEngine;
using UnityEngine.AI;
using PurrNet;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject playerPrefab;
    private NetworkManager nm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nm = FindFirstObjectByType<NetworkManager>();
        
        if (nm.isHost)
        {
            foreach (PlayerID p in nm.players)
            {
                // send SpawnPlayerLocal TargetRPC to each connected player
                SpawnPlayerLocal(p);
                Debug.Log("SpawnPlayerLocal RPC on :" + p.id.ToString());
            }
        }
    }

    [TargetRpc(bufferLast: true)]
    public void SpawnPlayerLocal(PlayerID id)
    {
        GameObject p = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        p.GetComponent<NetworkIdentity>().GiveOwnership(id);
    }
}
