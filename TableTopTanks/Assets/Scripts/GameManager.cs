using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.EditorTools;


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
    [SerializeField] private Scene[] levels;
    private int numLevels;
    private int currentLevel = -1; // -1 means no level selected

    private enum levelResult { None, Win, Lose };
    private levelResult previousLevelResult = levelResult.None;

    // other variables
    [Header("Between Levels Settings")]
    [SerializeField] private float betweenLevelsTimer = 0.0f;
    [SerializeField] private GameObject winPopupCanvas;
    [SerializeField] private GameObject losePopupCanvas;
    
    
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
    }


    void Start()
    {
        currentGameState = gameState.MainMenu;
        numLevels = levels.Length;

    }

    // Update is called once per frame
    void Update()
    {
        // wait for level to be selected (button click will update internal variable)
        // once level is selected, do level setup and loop the play switch until WIN or LOSE
        // if WIN, load next level (in level list), if LOSE, load main menu and unset level selection variable
        switch (currentGameState)
        {
            case gameState.MainMenu:
                if (currentLevel != -1)
                {
                    // load and setup level
                    SceneManager.LoadScene(levels[currentLevel].name);
                    SetupLevel();
                    currentGameState = gameState.InLevel;
                    previousLevelResult = levelResult.None;
                }
                break;
            case gameState.InLevel:
                PlayLevel();
                break;
            case gameState.BetweenLevels:
                // wait 5 seconds (with a win/loss screen popup)
                betweenLevelsTimer += Time.deltaTime;
                if (betweenLevelsTimer > 5.0f)
                {
                    if (previousLevelResult == levelResult.Win)
                    {
                        // won the level, go to next level (or main menu if last level)
                        if (currentLevel >= numLevels)
                        {
                            currentGameState = gameState.MainMenu;
                            currentLevel = -1;
                        }
                        else
                        {
                            currentLevel++;
                        }
                        winPopupCanvas.SetActive(false);
                        currentGameState = gameState.InLevel;
                        SceneManager.LoadScene(levels[currentLevel].name);
                        SetupLevel();
                    }
                    else
                    {
                        // lost the level, go to main menu
                        currentLevel = -1;
                        losePopupCanvas.SetActive(false);
                        currentGameState = gameState.MainMenu;
                        SceneManager.LoadScene("Menu");
                    }
                    
                    betweenLevelsTimer = 0.0f;
                }
                break;
        }
    }

    void PlayLevel()
    {
        switch (currentLevelState)
        {
            case levelState.Ongoing:
                // check win and lose conditions (enemy/player counts)
                enemies.RemoveAll(item => item == null);
                players.RemoveAll(item => item == null);
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
                winPopupCanvas.SetActive(true);
                break;
            case levelState.Lose:
                Debug.Log("Level " + currentLevel + " Failed!");
                previousLevelResult = levelResult.Lose;
                currentGameState = gameState.BetweenLevels;
                losePopupCanvas.SetActive(true);
                break;
        }
    }

    void SetupLevel()
    {
        currentLevelState = levelState.Ongoing;
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        players = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }

    public void OnSinglePlayerLevelSelected()
    {
        currentLevel = 0; // first level
    }

}
