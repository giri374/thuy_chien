using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý MenuScene.
/// Gán các Button trong Inspector để kết nối với các hàm bên dưới.
/// </summary>
public class MenuSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button playWithBotButton;
    public Button playWithFriendButton;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start()
    {
        // Đảm bảo GameManager tồn tại (trường hợp start thẳng từ MenuScene)
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MenuSceneManager] GameManager not found in scene! " +
                             "Please add a GameManager prefab to MenuScene.");
        }

        // Xóa data cũ khi vào menu (tránh carry-over từ ván trước)
        GameManager.Instance?.ClearAllPlacements();

        // Gán sự kiện cho button (backup nếu không drag-drop trong Inspector)
        if (playWithBotButton != null) playWithBotButton.onClick.AddListener(OnPlayWithBot);
        if (playWithFriendButton != null) playWithFriendButton.onClick.AddListener(OnPlayWithFriend);
    }

    // ── Button Callbacks ──────────────────────────────────────

    /// <summary>
    /// Nút "Chơi với Bot"
    /// </summary>
    public void OnPlayWithBot()
    {
        GameManager.Instance.SetGameMode(GameMode.PlayWithBot);
        // Vào SetupScene cho Player 1, sau đó thẳng vào BattleScene
        SceneManager.LoadScene("SetupScene");
    }

    /// <summary>
    /// Nút "Chơi với Bạn"
    /// </summary>
    public void OnPlayWithFriend()
    {
        GameManager.Instance.SetGameMode(GameMode.PlayWithFriend);
        // Vào SetupScene cho Player 1, sau đó SetupScene Player 2, rồi BattleScene
        SceneManager.LoadScene("SetupScene");
    }
}