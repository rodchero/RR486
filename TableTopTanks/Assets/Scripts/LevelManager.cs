using UnityEngine;
using System.Collections.Generic;

// each level has a controller object that manages win/lose conditions and level transitions
// may also manage multiplayer aspects in the future (this will track the gamestate later)
public class LevelManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Level Settings")]
    [SerializeField] private int levelNumber = 0;


    // internal variables
    private levelState currentState;
    private List<GameObject> enemies;
    private List<GameObject> players;
    private enum levelState { Ongoing, Win, Lose };


    void Start()
    {
        currentState = levelState.Ongoing;
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        players = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case levelState.Ongoing:
                // check win and lose conditions
                enemies.RemoveAll(item => item == null);
                players.RemoveAll(item => item == null);
                if (enemies.Count == 0)
                {
                    currentState = levelState.Win;
                }
                else if (players.Count == 0)
                {
                    currentState = levelState.Lose;
                }
                    break;
            case levelState.Win:
                Debug.Log("Level " + levelNumber + " Complete!");
                // show victory screen and load next level after a delay
                break;
            case levelState.Lose:
                // show game over screen + menu
                break;
        }
        
    }
}
