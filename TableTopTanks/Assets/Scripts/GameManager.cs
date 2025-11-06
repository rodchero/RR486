using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// each level has a controller object that manages win/lose conditions and level transitions
// may also manage multiplayer aspects in the future (this will track the gamestate later)
public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Level Settings")]
    [SerializeField] private int levelNumber = 0;


    // internal variables for level state (when in a level)
    private levelState currentLevelState;
    private List<GameObject> enemies;
    private List<GameObject> players;
    private enum levelState { Ongoing, Win, Lose };

    // variables for level management
    private enum gameState{ MainMenu, InLevel, BetweenLevels };
    private gameState currentGameState;
    [SerializeField] private Scene[] levels;
    private int numLevels;
    private int selectedLevel = -1; // -1 means no level selected

    void Start()
    {
        currentGameState = gameState.MainMenu;
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
                if (selectedLevel != -1)
                {
                    levelNumber = selectedLevel;
                    SetupLevel();
                    currentGameState = gameState.InLevel;
                }
                break;
            case gameState.InLevel:
                PlayLevel();
                break;
            case gameState.BetweenLevels:
                // load next level or return to main menu
                break;
        }
    }

    void PlayLevel()
    {
        switch (currentLevelState)
        {
            case levelState.Ongoing:
                // check win and lose conditions
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
                Debug.Log("Level " + levelNumber + " Complete!");
                // show victory screen and load next level after a delay
                SceneManager.LoadScene("Level");
                break;
            case levelState.Lose:
                // show game over screen + menu
                SceneManager.LoadScene("Level");
                break;
        }
    }

    void SetupLevel()
    {
        currentLevelState = levelState.Ongoing;
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        players = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }
}
