using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handler để quay lại MenuScene.
/// Có thể attach script này vào button "Back to Menu" và gọi BackToMenu() từ onClick event.
/// </summary>
public class BackToMenuHandler : MonoBehaviour
{
    public Button backButton;
    private void Start()
    {
        backButton.onClick.AddListener(BackToMenu);
    }

    public void BackToMenu()
    {
        // Xóa tất cả dữ liệu placement
        GameManager.Instance.ClearAllPlacements();

        // Reset game state
        GameManager.Instance.SetGameMode(GameMode.PlayWithBot);
        GameManager.Instance.SetCurrentSetupPlayer(1);

        // Load MenuScene
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        Debug.Log(" Returning to MenuScene...");
    }
}
