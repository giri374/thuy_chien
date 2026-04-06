using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// Quản lý MenuScene.
/// Gán các Button trong Inspector để kết nối với các hàm bên dưới.
/// </summary>
public class MenuSceneUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Button playWithBotButton;
    public Button playWithFriendButton;
    public Button playOnlineButton;

    [Header("Set Map")]
    public GameObject setMapPanel;
    public Button setNormalMapButton;
    public Button setAdvancedMapButton;
    public Button setAdvancedMap2Button;
    public Button setMapCancelButton;


    // ── Lifecycle ─────────────────────────────────────────────

    private void Start ()
    {
        // Đảm bảo GameManager tồn tại (trường hợp start thẳng từ MenuScene)
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MenuSceneManager] GameManager not found in scene! " +
                             "Please add a GameManager prefab to MenuScene.");
        }

        // Xóa data cũ khi vào menu (tránh carry-over từ ván trước)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllPlacements();
        }


        // Gán sự kiện cho button (backup nếu không drag-drop trong Inspector)
        if (playWithBotButton != null)
        {
            playWithBotButton.onClick.AddListener(OnPlayWithBot);
        }

        if (playWithFriendButton != null)
        {
            playWithFriendButton.onClick.AddListener(OnPlayWithFriend);
        }

        if (playOnlineButton != null)
        {
            playOnlineButton.onClick.AddListener(OnPlayOnline);
        }

        if (setNormalMapButton != null)
        {
            setNormalMapButton.onClick.AddListener(OnSetNormalMap);
        }
        if (setAdvancedMapButton != null)
        {
            setAdvancedMapButton.onClick.AddListener(OnSetAdvancedMap);
        }
        if (setMapCancelButton != null)
        {
            setMapCancelButton.onClick.AddListener(OnSetMapCancel);
        }
        if (setAdvancedMap2Button != null)
        {
            setAdvancedMap2Button.onClick.AddListener(OnSetAdvancedMap2);
        }

        LockMapButton();
    }

    private void OnEnable ()
    {
        // Poll liên tục để cập nhật button lock/unlock khi level thay đổi
        InvokeRepeating(nameof(LockMapButton), 0f, 0.5f);
    }

    private void OnDisable ()
    {
        CancelInvoke(nameof(LockMapButton));
    }

    // ── Button Callbacks ──────────────────────────────────────

    /// <summary>
    /// Nút "Chơi với Bot"
    /// </summary>
    public void OnPlayWithBot ()
    {
        GameManager.Instance.SetGameMode(GameMode.PlayWithBot);
        // Vào WeaponSetupScene Player 1, SetupScene cho Player 1,  sau đó thẳng vào BattleScene
        setMapPanel.SetActive(true);
    }

    /// <summary>
    /// Nút "Chơi với Bạn"
    /// </summary>
    public void OnPlayWithFriend ()
    {
        GameManager.Instance.SetGameMode(GameMode.PlayWithFriend);
        // Vào WeaponSetupScene Player 1,WeaponSetupScene Player 2, SetupScene cho Player 1, SetupScene Player 2,  rồi mới vào BattleScene
        setMapPanel.SetActive(true);
    }

    /// <summary>
    /// Nút "Chơi Online"
    /// </summary>
    public void OnPlayOnline ()
    {
        GameManager.Instance.SetGameMode(GameMode.PlayOnline);
        // Vào WeaponSetupScene (nếu AdvancedMap), SetupScene, sau đó OnlineConnectionScene trước BattleScene
        setMapPanel.SetActive(true);
    }

    public void OnSetNormalMap ()
    {
        GameManager.Instance.SetGameMap(GameMap.NormalMap);
        GameManager.Instance.SetRemoveEmptyCells(false);
        GoBattle();
    }
    public void OnSetAdvancedMap ()
    {
        GameManager.Instance.SetGameMap(GameMap.AdvancedMap);
        GameManager.Instance.SetRemoveEmptyCells(false);
        GoBattle();
    }

    public void OnSetMapCancel ()
    {
        setMapPanel.SetActive(false);
    }

    private void GoBattle ()
    {
        // Reset setup players
        GameManager.Instance.SetCurrentSetupPlayer(1);

        // Nếu AdvancedMap: WeaponSetupScene → SetupScene → BattleScene
        if (GameManager.Instance.gameMap == GameMap.AdvancedMap)
        {
            SceneManager.LoadScene(SceneNames.WeaponSetup);
        }
        // Nếu NormalMap: SetupScene → BattleScene
        else
        {
            SceneManager.LoadScene(SceneNames.Setup);
        }
    }

    public void OnSetAdvancedMap2 ()
    {
        GameManager.Instance.SetGameMap(GameMap.AdvancedMap);
        GameManager.Instance.SetRemoveEmptyCells(true);
        GoBattle();
    }

    private void LockMapButton ()
    {
        if (ProgressManager.Instance.Data.level < 3)
        {
            setAdvancedMap2Button.interactable = false;
        }
        else
        {
            setAdvancedMap2Button.interactable = true;
        }
        if (ProgressManager.Instance.Data.level < 2)
        {
            setAdvancedMapButton.interactable = false;
        }
        else
        {
            setAdvancedMapButton.interactable = true;
        }


    }





}