using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using PurrNet;
using PurrNet.Transports;

public class MultiplayerSimpleUI : MonoBehaviour
{
    [Header("UI refs")]
    public TextMeshProUGUI statusText;                     // status message
    public Button startButton;
    public Button backButton;

    [Header("Panels (wiring)")]
    public GameObject multiplayerPanel;         // this panel (will be disabled on Back)
    public GameObject mainMenuPanel;            // menu panel to enable on Back

    // internal state
    bool player2Connected = false;
    bool connectedToServer = false; // for client: whether connected to host
    private NetworkManager nm;

    void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(HandleStartPressed);
        if (backButton != null) backButton.onClick.AddListener(HandleBackPressed);

        nm = FindFirstObjectByType<NetworkManager>();

        if (nm.isHost)
        {
            Debug.Log("MultiplayerSimpleUI: operating as HOST");
        }
        else
        {
            Debug.Log("MultiplayerSimpleUI: operating as CLIENT");
        }
        RefreshUI();
    }

    void Update()
    {
        // if host, check if 2nd player connected
        if (nm.isHost)
        {
            player2Connected = FindFirstObjectByType<NetworkManager>().playerCount == 2;
        }
        else  // if client, check if connected to server
        {
            connectedToServer = FindFirstObjectByType<NetworkManager>().clientState == ConnectionState.Connected;
        }
        RefreshUI();
    }


    void HandleBackPressed()
    {
        // default panel toggle behavior (caller may override)
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
    
    void HandleStartPressed()
    {
            FindFirstObjectByType<Manager>().OnMultiplayerStartGame();
    }

    // internal UI refresh logic -------------------------------------------------
    void RefreshUI()
    {
        if (statusText != null)
        {
            if (nm.isHost)
            {
                statusText.text = player2Connected ? "Player 2 connected" : "Waiting for player 2";
            }
            else
            {
                statusText.text = connectedToServer ? "Connected to server" : "Waiting for server";
            }
        }

        // Start button: only enabled when player2 is connected AND this client is host
        if (startButton != null)
            startButton.interactable = (nm.isHost && player2Connected);
    }
}