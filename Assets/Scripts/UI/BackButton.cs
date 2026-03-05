using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handler để quay lại MenuScene.
/// Có thể attach script này vào button "Back to Menu" và gọi BackToMenu() từ onClick event.
/// </summary>
public class BackToMenuHandler : MonoBehaviour
{
    public Button backButton;
    public GameObject confirmPanel;
    public Button confirmBackButton;
    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ShowConfirmPanel);
        if (confirmBackButton != null)
            confirmBackButton.onClick.AddListener(BackToMenu);
    }

    public void ShowConfirmPanel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }
    public void BackToMenu()
    {
        // Xóa tất cả dữ liệu placement
        GameManager.Instance.ClearAllPlacements();

        // Reset game state
        GameManager.Instance.SetGameMode(GameMode.PlayWithBot);
        GameManager.Instance.SetCurrentSetupPlayer(1);

        // Load MenuScene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        Debug.Log(" Returning to MenuScene...");
    }
}
