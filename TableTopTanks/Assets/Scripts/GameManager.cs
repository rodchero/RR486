using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    // internal variables for level state (when in a level)
    private levelState currentLevelState;
    private List<GameObject> enemies;
    private List<GameObject> players;
    private enum levelState { Ongoing, Win, Lose };

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

    private void OnDestroy()
    {
        // unsubscribe to avoid memory leaks / dangling handlers
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // only initialize level when we just loaded the selected level
        if (currentLevel != -1 && levels != null && currentLevel >= 0 && currentLevel < levels.Length)
        {
            if (scene.name == levels[currentLevel])
            {
                // wait one frame so scene objects' Start/Awake/OnEnable have run
                StartCoroutine(DelayedSetupAfterLoad(scene.name));
            }
        }
        // optional: handle Menu load cleanup if needed
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
                        Debug.LogError($"GameManager: Invalid level index {currentLevel}. levels length={(levels==null?0:levels.Length)}. Clear selection or configure levels in inspector.");
                        currentLevel = -1;
                        break;
                    }

                    Debug.Log("Loading Level " + currentLevel);
                    isLoadingLevel = true;
                    // load scene and wait for OnSceneLoaded to call SetupLevel
                    SceneManager.LoadScene(levels[currentLevel]);
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
                            currentLevelState = levelState.Ongoing;
                            previousLevelResult = levelResult.None;
                            isLoadingLevel = false;

                            currentGameState = gameState.MainMenu;
                            SceneManager.LoadScene("Menu");
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
                            SceneManager.LoadScene(levels[currentLevel]);
                            // OnSceneLoaded/DelayedSetupAfterLoad will set currentGameState = InLevel
                        }
                    }
                    else // lost
                    {
                        Debug.Log("Level failed. Returning to Main Menu.");
                        if (lC != null) { Destroy(lC); lC = null; }

                        currentLevel = -1;
                        previousLevelResult = levelResult.None;
                        currentLevelState = levelState.Ongoing;
                        isLoadingLevel = false;

                        currentGameState = gameState.MainMenu;
                        SceneManager.LoadScene("Menu");
                    }
                }
                break;
        }
    }

    void PlayLevel()
    {
        switch (currentLevelState)
        {
            case levelState.Ongoing:
                // defensive: if lists haven't been initialized yet, don't decide win/lose
                if (enemies == null || players == null)
                {
                    Debug.Log("PlayLevel: waiting for SetupLevel to initialize lists.");
                    return;
                }

                enemies.RemoveAll(item => item == null);
                players.RemoveAll(item => item == null);

                Debug.Log($"PlayLevel: players={players.Count} enemies={enemies.Count}");

                if (enemies.Count == 0)
                {
                    currentLevelState = levelState.Win;
                }
                else if (players.Count == 0)
                {
                    currentLevelState = levelState.Lose;
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

    // updated SetupLevel signature to accept pre-found arrays (keeps one Find step)
    void SetupLevel(GameObject[] foundEnemies, GameObject[] foundPlayers)
    {
        currentLevelState = levelState.Ongoing;

        enemies = new List<GameObject>(foundEnemies ?? new GameObject[0]);
        players = new List<GameObject>(foundPlayers ?? new GameObject[0]);

        // defensive cleanup
        enemies.RemoveAll(item => item == null);
        players.RemoveAll(item => item == null);

        Debug.Log($"SetupLevel: initialized players={players.Count} enemies={enemies.Count}");
    }

    // keep original overload so other code calling SetupLevel() still works if present
    void SetupLevel()
    {
        SetupLevel(GameObject.FindGameObjectsWithTag("Enemy"), GameObject.FindGameObjectsWithTag("Player"));
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

}
