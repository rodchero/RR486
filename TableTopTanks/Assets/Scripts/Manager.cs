using UnityEngine;
using PurrNet;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Analytics;
using System.Threading.Tasks;
using UnityEngine.Windows.WebCam;

public class Manager : MonoBehaviour
{
    // Data Section ------------------------------------------------------------

    // Prefab references
    [Header("Prefabs")]
    [SerializeField] public GameObject playerTankPrefab;
    [SerializeField] private GameObject winPopupCanvas;
    [SerializeField] private GameObject losePopupCanvas;


    // General Game State 
    private enum gameState { MainMenu, InLevel, BetweenLevels };
    private gameState _gameState = gameState.MainMenu;
    private int currentSceneIndexLocal = 0;
    private SyncVar<int> currentSceneIndex = new SyncVar<int>(0);
    private bool isSinglePlayer = true;


    // Main Menu State
    private GameObject multiplayerPanel;
    private GameObject mainMenuPanel;


    // In-Level variables
    private GameObject playerTankSpawnPoint;
    private bool isSceneLoaded = true;


    // Between-Levels State (after win/lose, before next level/menu loads)
    private enum levelResult { None, Won, Lost };
    private levelResult _levelResult = levelResult.None;
    private GameObject winPopup, losePopup;
    private float betweenLevelsTimeElapsed = 0.0f;
    [Header("Between Levels")]
    [SerializeField] private float timeBetweenLevels = 2.0f;


    // Scenes (so that this script can load them in order provided)
    [Header("Level Settings")]
    [SerializeField] private int numberOfLevels;


    // Other
    private NetworkManager nm;

    // End of Data Section -----------------------------------------------------

    // Singleton instance
    public static Manager Instance { get; private set; }

    void Awake()
    {
        // Ensure singleton instance (i.e, when reloading menu scene)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Get references
        nm = FindFirstObjectByType<NetworkManager>();
        currentSceneIndexLocal = 0;
        multiplayerPanel = GameObject.Find("MultiplayerPanel");
        mainMenuPanel = GameObject.Find("MainMenuPanel");

        // initial setup
        _gameState = gameState.MainMenu;
        _levelResult = levelResult.None;
        multiplayerPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        // Subscribe to sceneLoaded so OnSceneLoaded runs when a scene finishes loading
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Callback to finish settup up the scene after it loads
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene: " + currentSceneIndexLocal + " loaded!");

        // Level/menu specific setup after scene load
        if (currentSceneIndexLocal == 0)
        {
            multiplayerPanel = GameObject.Find("MultiplayerPanel");
            mainMenuPanel = GameObject.Find("MainMenuPanel");
            _gameState = gameState.MainMenu;
            _levelResult = levelResult.None;
            multiplayerPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
        else
        {
            // call playerSpawner to spawn a character for each player
        }
        isSceneLoaded = true;
    }

   void Update()
    {
        // Handle multiplayer scene sync
        if (!isSinglePlayer) currentSceneIndexLocal = currentSceneIndex.value;

        // Do nothing until scene fully loaded
        if (!isSceneLoaded) return;

        // Ensure correct scene is loaded at all times
        if (SceneManager.GetActiveScene().buildIndex != currentSceneIndexLocal)
        {
            //if (nm.isHost && !isSinglePlayer) nm.sceneModule.LoadSceneAsync(currentSceneIndexLocal); // host-driven scene loading
            //else SceneManager.LoadScene(currentSceneIndexLocal);
            nm.sceneModule.LoadSceneAsync(currentSceneIndexLocal);
            isSceneLoaded = false;
        }

        // main gamestat fsm
        switch (_gameState)
        {
            case gameState.MainMenu:

                break;

            case gameState.InLevel:
                // check win/lose conditions while level is ongoing
                int pc = GameObject.FindGameObjectsWithTag("Player").Count();
                int ec = GameObject.FindGameObjectsWithTag("Enemy").Count();
                if (ec == 0)
                {
                    _levelResult = levelResult.Won;
                    _gameState = gameState.BetweenLevels;
                    if (winPopup == null) winPopup = Instantiate(winPopupCanvas);
                }
                else if (pc == 0)
                {
                    _levelResult = levelResult.Lost;
                    _gameState = gameState.BetweenLevels;
                    if (losePopup == null) losePopup = Instantiate(losePopupCanvas);
                }

                break;

            case gameState.BetweenLevels:
                betweenLevelsTimeElapsed += Time.deltaTime;
                if (betweenLevelsTimeElapsed >= timeBetweenLevels)
                {
                    // cleanup popups 
                    if (winPopup != null) Destroy(winPopup);
                    if (losePopup != null) Destroy(losePopup);
                    betweenLevelsTimeElapsed = 0.0f;

                    // decide next scene to load based on levelResult var
                    switch (_levelResult)
                    {
                        case levelResult.Won:
                            // if more levels remain, load next level on win
                            if (currentSceneIndexLocal + 1 <= numberOfLevels)
                            {
                                if (nm.isHost) currentSceneIndex.value++;
                                currentSceneIndexLocal++;
                            }
                            else
                            {
                                if (nm.isHost) currentSceneIndex.value = 0;
                                currentSceneIndexLocal = 0;
                            }
                            break;
                        case levelResult.Lost:
                            // return to main menu on loss
                            if (nm.isHost) currentSceneIndex.value = 0;
                            currentSceneIndexLocal = 0;
                            break;
                    }
                    _levelResult = levelResult.None;
                    if (currentSceneIndexLocal != 0) _gameState = gameState.InLevel;
                    else _gameState = gameState.MainMenu;
                }
                break;
        }
    }

    // main menu singleplayer button event
    public void OnSinglePlayerButtonPress()
    {
        Debug.Log("Singleplayer Mode Starting");
        currentSceneIndexLocal = 1;
        currentSceneIndex.value = 1;
        isSinglePlayer = true;
    }

    // main menu multiplayer button event
    public void OnMultiplayerButtonPress()
    {
        multiplayerPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    // multiplayer meny start button event
    public void OnMultiplayerStartGame()
    {
        Debug.Log("Multiplayer Mode Starting");
        currentSceneIndex.value = 1;
        isSinglePlayer = false;
    }
}