using UnityEngine;
using PurrNet;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;

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
    private SyncVar<int> currentSceneIndex = new SyncVar<int>(0);


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
    private bool wasHost = false;

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
        Debug.Log("Scene: " + scene.buildIndex + " loaded!");

        // Level/menu specific setup after scene load
        if (scene.buildIndex == 0)
        {
            multiplayerPanel = GameObject.Find("MultiplayerPanel");
            mainMenuPanel = GameObject.Find("MainMenuPanel");
            _gameState = gameState.MainMenu;
            _levelResult = levelResult.None;
            multiplayerPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            Button singlePlayerButton = GameObject.Find("1P").GetComponent<Button>();
            if (!nm.isHost) singlePlayerButton.interactable = false;
        }
        else
        {
            _gameState = gameState.InLevel;
        }
        isSceneLoaded = true;
    }


    void Update()
    {
        if (!nm.isHost && !wasHost)
        {
            return;
        } else
        {
            wasHost = true;
        }

        // Do nothing until scene fully loaded
        if (!isSceneLoaded) return;

        // Ensure correct scene is loaded at all times
        if (SceneManager.GetActiveScene().buildIndex != currentSceneIndex.value)
        {
            nm.sceneModule.LoadSceneAsync(currentSceneIndex.value);
            isSceneLoaded = false;
        }

        // main gamestate fsm
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
                    Debug.Log("Win! (enemy count = 0)");
                    _levelResult = levelResult.Won;
                    _gameState = gameState.BetweenLevels;
                    if (winPopup == null) winPopup = Instantiate(winPopupCanvas);
                }
                else if (pc == 0)
                {
                    Debug.Log("Loss! (player count = 0)");
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
                            // if more levels remain, load next level on win (else menu)
                            if (currentSceneIndex.value + 1 <= numberOfLevels)
                            {
                                currentSceneIndex.value++;
                            }
                            else
                            {
                                currentSceneIndex.value = 0;
                            }
                            break;
                        case levelResult.Lost:
                            // return to main menu on loss
                            currentSceneIndex.value = 0;
                            break;
                    }

                    // reset levelResult
                    _levelResult = levelResult.None;
                }
                break;
        }
    }

    // main menu singleplayer button event
    public void OnSinglePlayerButtonPress()
    {
        Debug.Log("Singleplayer Mode Starting");
        currentSceneIndex.value = 1;
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
    }
}