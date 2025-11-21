using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using PurrNet;
using System;
using System.Runtime.ExceptionServices;
using PurrNet.Modules;
using System.Runtime.CompilerServices;
using UnityEngine.AI;






#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : NetworkIdentity
{
    // internal variables for level state (when in a level)
    private SyncVar<levelState> currentLevelState = new(levelState.Ongoing);
    private SyncArray<GameObject> enemies;
    private SyncArray<GameObject> players;
    private enum levelState { Ongoing, Win, Lose };

    private bool isSinglePlayer = true;
    // used to (not) spawn in 2nd player tank in singleplayer mode & scene syncronization 

    // variables for level management
    private enum gameState { MainMenu, InLevel, BetweenLevels };
    private gameState currentGameState;
    [Header("Level Settings")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset[] levelScenes; // For editor reference
#endif
    private string[] levels; // For runtime use
    private int numLevels;
    private int currentLevel = -1; // -1 means no level selected

    private enum levelResult { None, Win, Lose };
    private levelResult previousLevelResult = levelResult.None;

    // other variables
    [Header("Between Levels Settings")]
    private float betweenLevelsTimer = 0.0f;
    [SerializeField] private float timeBetweenLevels = 5.0f;
    [SerializeField] private GameObject winPopupCanvas;
    [SerializeField] private GameObject losePopupCanvas;
    private GameObject wC, lC;

    private bool isLoadingLevel = false;

    // Scene object references
    public GameObject multiplayerPanel;
    public GameObject mainMenuPanel;
    [Header("Player Spawning")]
    [SerializeField] public GameObject playerTankSpawnPoint;
    [SerializeField] public GameObject playerTankPrefab;

    // Singleton instance - this script should only have one instance and always persists between scenes (to manage game state)
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not this, delete this.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // fix for inital load into Menu scene
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            RebindMenuPanels();
            multiplayerPanel.SetActive(false);
        }

        // Set the instance and make it persistent
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // subscribe to sceneLoaded so we can initialize after load completes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Convert SceneAsset references to scene names
#if UNITY_EDITOR
        levels = new string[levelScenes.Length];
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] != null)
                levels[i] = levelScenes[i].name;
        }
#endif

        // ensure numLevels is known as early as possible
        numLevels = levels != null ? levels.Length : 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            RebindMenuPanels();
            multiplayerPanel.SetActive(false);
            isSinglePlayer = true; // reset to singleplayer on menu load (set when multiplayer selected)
        }
        // only initialize level when we just loaded the selected level
        if (currentLevel != -1 && levels != null && currentLevel >= 0 && currentLevel < levels.Length)
        {
            if (scene.name == levels[currentLevel])
            {
                // wait one frame so scene objects' Start/Awake/OnEnable have run
                StartCoroutine(DelayedSetupAfterLoad(scene.name));
            }
        }
    }

    private void RebindMenuPanels()
    {
        // Try find by name 
        multiplayerPanel = GameObject.Find("MultiplayerPanel");
        mainMenuPanel = GameObject.Find("MainMenuPanel");
    }

    private System.Collections.IEnumerator DelayedSetupAfterLoad(string sceneName)
    {
        // give one frame for Start/Awake to run
        yield return null;
        yield return new WaitForEndOfFrame(); // extra safety for static objects' Start to finish

        // wait until at least one Player and at least one Enemy are present (or timeout)
        float maxWait = 2.0f; // seconds
        float waited = 0f;
        GameObject[] foundPlayers = new GameObject[0];
        GameObject[] foundEnemies = new GameObject[0];

        while (waited < maxWait)
        {
            foundPlayers = GameObject.FindGameObjectsWithTag("Player");
            foundEnemies = GameObject.FindGameObjectsWithTag("Enemy");

            if ((foundPlayers != null && foundPlayers.Length > 0) && (foundEnemies != null && foundEnemies.Length > 0))
                break;

            waited += Time.deltaTime;
            yield return null;
        }

        // debug info so we can see what was present at setup time
        Debug.Log($"DelayedSetupAfterLoad: scene='{sceneName}' waited={waited:F2}s players={foundPlayers?.Length ?? 0} enemies={foundEnemies?.Length ?? 0}");

        // now run setup and switch into the InLevel state
        // pass the found arrays to SetupLevel so it doesn't rely on a second Find call
        SetupLevel(foundEnemies, foundPlayers);

        if (players == null || players.Count == 0)
            Debug.LogWarning("GameManager: No players found after waiting. Verify 'Player' tag and spawn timing.");
        if (enemies == null || enemies.Count == 0)
            Debug.Log("GameManager: No enemies found at setup. If enemies spawn later, consider notifying GameManager when spawn completes.");

        currentGameState = gameState.InLevel;
        previousLevelResult = levelResult.None;
        isLoadingLevel = false;
        Debug.Log("Scene loaded and level setup complete: " + sceneName);
    }

    void Start()
    {
        currentGameState = gameState.MainMenu;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentGameState)
        {
            case gameState.MainMenu:
                if (currentLevel != -1 && !isLoadingLevel)
                {
                    // guard against invalid level index / empty levels array
                    if (levels == null || currentLevel < 0 || currentLevel >= levels.Length)
                    {
                        Debug.LogError($"GameManager: Invalid level index {currentLevel}. levels length={(levels == null ? 0 : levels.Length)}. Clear selection or configure levels in inspector.");
                        currentLevel = -1;
                        break;
                    }

                    Debug.Log("Loading Level " + currentLevel);
                    isLoadingLevel = true;
                    // load scene and wait for OnSceneLoaded to call SetupLevel
                    if (isSinglePlayer)
                    {
                        SceneManager.LoadScene(levels[currentLevel]);
                    }
                    else
                    {
                        LoadNetworkedLevel(levels[currentLevel]);
                    }
                }
                break;
            case gameState.InLevel:
                PlayLevel();
                break;
            case gameState.BetweenLevels:
                betweenLevelsTimer += Time.deltaTime;
                if (betweenLevelsTimer > timeBetweenLevels)
                {
                    betweenLevelsTimer = 0f; // reset timer immediately

                    if (previousLevelResult == levelResult.Win)
                    {
                        // If last level, go back to menu
                        if (currentLevel >= numLevels - 1)
                        {
                            Debug.Log("Last level completed. Returning to Main Menu.");
                            // cleanup popups/canvases
                            if (wC != null) { Destroy(wC); wC = null; }
                            if (lC != null) { Destroy(lC); lC = null; }

                            // reset level selection and state, then load Menu
                            currentLevel = -1;
                            currentLevelState.value = levelState.Ongoing;
                            previousLevelResult = levelResult.None;
                            isLoadingLevel = false;

                            currentGameState = gameState.MainMenu;
                            if (isSinglePlayer)
                            {
                                SceneManager.LoadScene("Menu");
                            }
                            else
                            {
                                LoadNetworkedLevel("Menu");
                            }
                        }
                        else
                        {
                            // advance to next level
                            currentLevel++;
                            Debug.Log($"Advancing to next level: {currentLevel}");
                            // cleanup popup
                            if (wC != null) { Destroy(wC); wC = null; }

                            isLoadingLevel = true;
                            currentGameState = gameState.MainMenu; // stay in menu-loading state until sceneLoaded handles setup
                            if (isSinglePlayer)
                            {
                                SceneManager.LoadScene(levels[currentLevel]);
                            }
                            else
                            {
                                LoadNetworkedLevel(levels[currentLevel]);
                            }
                            // OnSceneLoaded/DelayedSetupAfterLoad will set currentGameState = InLevel
                        }
                    }
                    else // lost
                    {
                        Debug.Log("Level failed. Returning to Main Menu.");
                        if (lC != null) { Destroy(lC); lC = null; }

                        currentLevel = -1;
                        previousLevelResult = levelResult.None;
                        currentLevelState.value = levelState.Ongoing;
                        isLoadingLevel = false;

                        currentGameState = gameState.MainMenu;
                        if (isSinglePlayer)
                        {
                            SceneManager.LoadScene("Menu");
                        }
                        else
                        {
                            LoadNetworkedLevel("Menu");
                        }
                    }
                }
                break;
        }
    }

    [ServerRpc]
    void PlayLevel()
    {
        switch (currentLevelState.value)
        {
            case levelState.Ongoing:
                // defensive: if lists haven't been initialized yet, don't decide win/lose
                if (enemies == null || players == null)
                {
                    Debug.Log("PlayLevel: waiting for SetupLevel to initialize lists.");
                    return;
                }

                // clean up any destroyed objects from enemy and player lists
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (enemies[i] == null)
                    {
                        enemies.RemoveAt(i);
                    }
                }
                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (players[i] == null)
                    {
                        players.RemoveAt(i);
                    }
                }

                Debug.Log($"PlayLevel: players={players.Count} enemies={enemies.Count}");

                if (enemies.Count == 0)
                {
                    currentLevelState.value = levelState.Win;
                }
                else if (players.Count == 0)
                {
                    currentLevelState.value = levelState.Lose;
                }
                break;
            case levelState.Win:
                Debug.Log("Level " + currentLevel + " Complete!");
                previousLevelResult = levelResult.Win;
                currentGameState = gameState.BetweenLevels;
                wC = GameObject.Instantiate(winPopupCanvas);
                break;
            case levelState.Lose:
                Debug.Log("Level " + currentLevel + " Failed!");
                previousLevelResult = levelResult.Lose;
                currentGameState = gameState.BetweenLevels;
                lC = GameObject.Instantiate(losePopupCanvas);
                break;
        }
    }

    void SetupLevel(GameObject[] foundEnemies, GameObject[] foundPlayers)
    {
        currentLevelState.value = levelState.Ongoing;
        GameObject playerTankSpawnPoint = GameObject.Find("PlayerTankSpawnPoint");

        Vector3 spawnOffset = playerTankSpawnPoint.transform.position + new Vector3(-3f, 0, 0); // 1st player spawns 3 units to the left
        // call server-side spawn method
        SpawnAndOwnTank_Server(spawnOffset);

        // setup 2nd playertank for player2 in multiplayer
        if (!isSinglePlayer)
        {
            Debug.Log("Spawning 2nd player tank for multiplayer.");
            spawnOffset = playerTankSpawnPoint.transform.position + new Vector3(3f, 0, 0); // 2nd player spawns 3 units to the right
            SpawnAndOwnTank_Server(spawnOffset);
        }

        // create and populate SyncArrays for enemies and players
        enemies = new SyncArray<GameObject>();
        for (int i = 0; i < foundEnemies.Length; i++)
        {
            enemies.Add(foundEnemies[i]);
        }
        players = new SyncArray<GameObject>();
        for (int i = 0; i < foundPlayers.Length; i++)
        {
            players.Add(foundPlayers[i]);
        }

        // defensive cleanup
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
            }
        }
        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] == null)
            {
                players.RemoveAt(i);
            }
        }

        Debug.Log($"SetupLevel: initialized players={players.Count} enemies={enemies.Count}");
    }

    // Replace your ObserversRpc method with a server-side spawn implementation
    [ServerRpc]
    private void SpawnAndOwnTank_Server(Vector3 offset)
    {
        var nm = FindFirstObjectByType<NetworkManager>();

        // instantiate on the server
        GameObject playerTank = Instantiate(playerTankPrefab, offset, Quaternion.identity);
        
        nm.Spawn(playerTank); 

        // Assign ownership: you must supply the correct connection / player reference from server context.
        // Example placeholder - replace 'targetConnection' with the actual server connection/player object:
        // playerTank.GetComponent<NetworkIdentity>().GiveOwnership(targetConnection);
        PlayerID targetConnection = help
        playerTank.GetComponent<NetworkIdentity>().GiveOwnership(targetConnection);
    }


    public void OnSinglePlayerLevelSelected()
    {
        Debug.Log("Single Player Level Selected");
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: No levels configured. Add level scenes in the GameManager inspector and in Build Settings.");
            return;
        }

        // pick first level (clamped in case levels changed)
        currentLevel = Mathf.Clamp(0, 0, levels.Length - 1);
    }

    public void OnMultiplayerLevelSelected()
    {
        if (multiplayerPanel == null || mainMenuPanel == null)
        {
            RebindMenuPanels();
        }
        multiplayerPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        isSinglePlayer = false;
    }

    public void OnMultiplayerStartGame()
    {
        Debug.Log("Multiplayer Start Game Selected");
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: No levels configured. Add level scenes in the GameManager inspector and in Build Settings.");
            return;
        }

        // pick first level (clamped in case levels changed)
        currentLevel = Mathf.Clamp(0, 0, levels.Length - 1);
    }

    private void LoadNetworkedLevel(string levelName)
    {
        // Use PurrNet NetworkManager to load level for all clients
        Debug.Log("Loading networked level: " + levelName);
        var nm = FindFirstObjectByType<NetworkManager>();
        nm.sceneModule.LoadSceneAsync(levelName);
    }


}
