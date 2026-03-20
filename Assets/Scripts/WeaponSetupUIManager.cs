using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Quản lý WeaponSetupScene.
/// - Load gold từ cloud (ProgressManager)
/// - Cho người chơi chọn vũ khí
/// - Deduct gold khi chọn vũ khí
/// - Xử lý logic chuyển sang SetupScene hoặc tiếp tục sang player 2
/// </summary>
public class WeaponSetupUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerTitleText;
    public TextMeshProUGUI currentGoldText;
    public Button confirmButton;
    public Button cancelButton;
    public Button resetButton;

    [Header("Weapon Buttons")]
    public WeaponButton[] weaponButtons;

    private int currentPlayer = 1;
    private int currentGold = 0;
    private int initialGold = 0;
    private GameMode gameMode = GameMode.PlayWithBot;

    // Tracking selected weapons
    private Dictionary<WeaponType, WeaponButton> selectedWeaponsMap = new Dictionary<WeaponType, WeaponButton>();

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start ()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[WeaponSetupUIManager] GameManager not found!");
            return;
        }

        currentPlayer = GameManager.Instance.currentSetupPlayer;
        gameMode = GameManager.Instance.gameMode;

        // Load dữ liệu
        LoadWeaponSetupData();
        CreateWeaponButtons();
        SetupButtonListeners();
    }

    // ── Data Loading ───────────────────────────────────────────

    /// <summary>
    /// Tải dữ liệu setup vũ khí từ ProgressManager và GameManager
    /// </summary>
    private void LoadWeaponSetupData ()
    {
        // Load gold từ ProgressManager (nếu lần đầu chơi)
        if (GameManager.Instance.GetPlayerGold(currentPlayer) == 0)
        {
            if (ProgressManager.Instance != null)
            {
                int goldFromCloud = ProgressManager.Instance.Data.gold;
                GameManager.Instance.SetPlayerGold(currentPlayer, goldFromCloud);
                Debug.Log($"[WeaponSetupUIManager] Loaded gold from cloud: {goldFromCloud}");
            }
            else
            {
                // Fallback nếu không có ProgressManager
                GameManager.Instance.SetPlayerGold(currentPlayer, 100);
                Debug.LogWarning("[WeaponSetupUIManager] ProgressManager not found, using default gold");
            }
        }

        // Thêm NormalShot vào mặc định (không cần chọn)
        if (!GameManager.Instance.GetSelectedWeapons(currentPlayer).Contains(WeaponType.NormalShot))
        {
            GameManager.Instance.AddWeapon(currentPlayer, WeaponType.NormalShot);
            Debug.Log($"[WeaponSetupUIManager] Player {currentPlayer} added NormalShot by default");
        }

        currentGold = GameManager.Instance.GetPlayerGold(currentPlayer);
        initialGold = currentGold;

        // Update UI
        UpdatePlayerTitleText();
        UpdateGoldText();
    }

    /// <summary>
    /// Setup các nút chọn vũ khí từ inspector (không hiển thị NormalShot - nó được thêm mặc định)
    /// Tự động match button theo assignedWeaponType, không phụ thuộc vào thứ tự kéo thả
    /// </summary>
    private void CreateWeaponButtons ()
    {
        var weaponListData = GameManager.Instance.weaponListData;
        if (weaponListData == null)
        {
            Debug.LogError("[WeaponSetupUIManager] WeaponListData not found in GameManager!");
            return;
        }

        if (weaponButtons == null || weaponButtons.Length == 0)
        {
            Debug.LogError("[WeaponSetupUIManager] No weapon buttons assigned in inspector!");
            return;
        }

        selectedWeaponsMap.Clear();

        // Xây dựng dictionary: weaponType -> WeaponButton để match tự động
        var buttonDictionary = new Dictionary<WeaponType, WeaponButton>();
        foreach (var weaponButton in weaponButtons)
        {
            if (weaponButton != null)
            {
                var assignedType = weaponButton.GetAssignedWeaponType();
                buttonDictionary[assignedType] = weaponButton;
            }
        }

        // Setup button cho mỗi vũ khí dựa vào assignedWeaponType
        int setupCount = 0;
        foreach (var weapon in weaponListData.weapons)
        {
            // Skip NormalShot - nó được thêm vào danh sách mặc định
            if (weapon.type == WeaponType.NormalShot)
            {
                continue;
            }

            // Tìm button dựa vào weaponType
            if (buttonDictionary.TryGetValue(weapon.type, out var weaponButton))
            {
                if (weaponButton != null)
                {
                    weaponButton.Setup(weapon, currentGold, OnWeaponSelected, OnWeaponDeselected);
                    // Store mapping
                    selectedWeaponsMap[weapon.type] = weaponButton;
                    setupCount++;
                }
            }
            else
            {
                Debug.LogWarning($"[WeaponSetupUIManager] Không tìm thấy button cho weapon type: {weapon.type}. Hãy kiểm tra assignedWeaponType trong inspector!");
            }
        }

        Debug.Log($"[WeaponSetupUIManager] Setup {setupCount} weapon buttons thành công");
    }

    // ── UI Callbacks ───────────────────────────────────────────

    /// <summary>
    /// Được gọi khi người chơi chọn một vũ khí
    /// </summary>
    private void OnWeaponSelected (WeaponData weapon)
    {
        // Kiểm tra đủ gold không
        if (currentGold >= weapon.goldCost)
        {
            currentGold -= weapon.goldCost;
            GameManager.Instance.SetPlayerGold(currentPlayer, currentGold);
            GameManager.Instance.AddWeapon(currentPlayer, weapon.type);

            UpdateGoldText();
            RefreshWeaponButtons();

            Debug.Log($"[WeaponSetupUIManager] Player {currentPlayer} selected {weapon.weaponName}, gold remaining: {currentGold}");
        }
        else
        {
            Debug.LogWarning($"[WeaponSetupUIManager] Not enough gold! Need {weapon.goldCost}, have {currentGold}");
            ShowInsufficientGoldMessage();
        }
    }

    /// <summary>
    /// Được gọi khi người chơi deselect một vũ khí
    /// </summary>
    private void OnWeaponDeselected (WeaponData weapon)
    {
        // Thêm gold trở lại
        currentGold += weapon.goldCost;
        GameManager.Instance.SetPlayerGold(currentPlayer, currentGold);
        GameManager.Instance.RemoveWeapon(currentPlayer, weapon.type);

        UpdateGoldText();
        RefreshWeaponButtons();

        Debug.Log($"[WeaponSetupUIManager] Player {currentPlayer} deselected {weapon.weaponName}, gold restored: {currentGold}");
    }

    /// <summary>
    /// Cập nhật UI các nút vũ khí (ví dụ disable nút nếu không đủ gold)
    /// </summary>
    private void RefreshWeaponButtons ()
    {
        if (weaponButtons == null || weaponButtons.Length == 0)
        {
            return;
        }

        foreach (var weaponButton in weaponButtons)
        {
            if (weaponButton != null)
            {
                weaponButton.UpdateAvailability(currentGold);
            }
        }
    }

    /// <summary>
    /// Cập nhật text hiển thị tên người chơi
    /// </summary>
    private void UpdatePlayerTitleText ()
    {
        if (playerTitleText != null)
        {
            string title = gameMode == GameMode.PlayWithBot
                ? "Player 1 - Weapon Setup"
                : $"Player {currentPlayer} - Weapon Setup";
            playerTitleText.text = title;
        }
    }

    /// <summary>
    /// Cập nhật text hiển thị gold hiện tại
    /// </summary>
    private void UpdateGoldText ()
    {
        if (currentGoldText != null)
        {
            currentGoldText.text = $"Gold: {currentGold}";
        }
    }

    /// <summary>
    /// Hiển thị thông báo gold không đủ
    /// </summary>
    private void ShowInsufficientGoldMessage ()
    {
        Debug.LogWarning("[WeaponSetupUIManager] Insufficient gold!");
        // TODO: Hiển thị UI thông báo (ví dụ toast notification)
    }

    // ── Button Setup ───────────────────────────────────────────

    private void SetupButtonListeners ()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }

    private void OnConfirmClicked ()
    {
        // Chuyển sang player tiếp theo hoặc SetupScene
        if (gameMode == GameMode.PlayWithFriend && currentPlayer == 1)
        {
            // Chuyển sang player 2
            GameManager.Instance.SetCurrentSetupPlayer(2);

            // Load lại scene để player 2 chọn vũ khí
            SceneManager.LoadScene(SceneNames.WeaponSetup);
        }
        else
        {
            // Chuyển sang SetupScene
            SceneManager.LoadScene(SceneNames.Setup);
        }
    }

    private void OnCancelClicked ()
    {
        // Hủy lựa chọn vũ khí của player hiện tại và quay lại
        GameManager.Instance.ClearWeapons(currentPlayer);
        GameManager.Instance.SetPlayerGold(currentPlayer, 0);

        // Quay lại MenuScene hoặc xử lý khác tùy yêu cầu
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Reset tất cả lựa chọn vũ khí và đặt lại gold
    /// </summary>
    private void OnResetClicked ()
    {
        // Xóa tất cả weapons từ GameManager
        GameManager.Instance.ClearWeapons(currentPlayer);

        // Reset gold
        currentGold = initialGold;
        GameManager.Instance.SetPlayerGold(currentPlayer, currentGold);

        // Reset UI buttons
        selectedWeaponsMap.Clear();
        RefreshWeaponButtons();
        UpdateGoldText();

        Debug.Log($"[WeaponSetupUIManager] Player {currentPlayer} reset all weapons, gold: {currentGold}");
    }
}
