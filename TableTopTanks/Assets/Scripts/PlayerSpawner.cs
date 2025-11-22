using UnityEngine;
using UnityEngine.AI;
using PurrNet;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject playerPrefab;
    private NetworkManager nm;
    private bool serverSceneReady = false;
    private List<PlayerID> pendingSpawns = new List<PlayerID>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nm = FindFirstObjectByType<NetworkManager>();
        nm.onPlayerJoined += SpawnPlayer;
        if (nm.isHost)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    [ServerRpc]
    public void SpawnPlayer(PlayerID id, bool reconnect, bool asServer)
    {
        if (!asServer && !reconnect)
        {
            SpawnPlayerLocal(id);
        }
        // Only act on server side
        if (!asServer) return;

        // If server hasn't finished loading the game scene yet, queue the spawn
        if (!serverSceneReady)
        {
            pendingSpawns.Add(id);
            return;
        }

        // spawn now by telling the target client to create their local player object
        SpawnPlayerLocal(id);
    }

    [TargetRpc(bufferLast: true)]
    public void SpawnPlayerLocal(PlayerID id)
    {
        GameObject p = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        p.GetComponent<NetworkIdentity>().GiveOwnership(id);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != 0)
        {
            ProcessPendingSpawns();
            serverSceneReady = true;
        }
    }

    private void ProcessPendingSpawns()
    {
        if (!serverSceneReady) return;
        foreach (var id in pendingSpawns)
        {
            SpawnPlayerLocal(id);
        }
        pendingSpawns.Clear();
    }
}
