using UnityEngine;

public class UIEventProxy : MonoBehaviour
{
    // Parameterless public method so Unity Button can call it from the Inspector.
    public void UIProxyStartSinglePlayer()
    {
        if (Manager.Instance != null)
        {
            Manager.Instance.OnSinglePlayerButtonPress();
        }
        else
        {
            Debug.LogWarning("UIEventProxy: GameManager.Instance is null when calling StartSinglePlayer.");
        }
    }

    public void UIProxyStartMultiplayer()
    {
        if (Manager.Instance != null)
        {
            Manager.Instance.OnMultiplayerButtonPress();
        }
        else
        {
            Debug.LogWarning("UIEventProxy: GameManager.Instance is null when calling StartMultiplayer.");
        }
    }
}