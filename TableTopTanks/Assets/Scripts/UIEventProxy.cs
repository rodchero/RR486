using UnityEngine;

public class UIEventProxy : MonoBehaviour
{
    // Parameterless public method so Unity Button can call it from the Inspector.
    public void StartSinglePlayer()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSinglePlayerLevelSelected();
        }
        else
        {
            Debug.LogWarning("UIEventProxy: GameManager.Instance is null when calling StartSinglePlayer.");
        }
    }

    // Add more proxy methods as needed:
    //public void StartMultiplayer() { /* GameManager.Instance?.OnMultiplayer(); */ }
    //public void OpenSettings()    { /* ... */ }
}