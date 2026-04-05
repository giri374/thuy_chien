using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quản lý hiển thị và lựa chọn vũ khí trong BattleScene
/// - Hiển thị vũ khí đã chọn của từng người chơi
/// - Xử lý lựa chọn vũ khí hiện tại
/// - Update khi turn thay đổi
/// </summary>
public class BattleWeaponManager : MonoBehaviour
{
    public static BattleWeaponManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI selectedWeaponText;
    public TextMeshProUGUI player1CPText;
    public TextMeshProUGUI player2CPText;
    public Transform weaponButtonContainer;
    public GameObject weaponButtonPrefab;

    [Header("Settings")]
    public bool autoShowCurrentPlayerWeapons = true;

    // Current state
    private Turn currentTurn = Turn.Player1;
    private WeaponType currentWeapon = WeaponType.NormalShot;
    private Dictionary<WeaponType, BattleWeaponButton> weaponButtons = new Dictionary<WeaponType, BattleWeaponButton>();

    private void Start ()
    {
        Instance = this;

        // Subscribe to turn changes
        if (BattleSceneLogic.Instance != null)
        {
            BattleSceneLogic.Instance.onTurnChanged += OnTurnChanged;
        }

        // Initialize with current player's weapons
        RefreshWeaponDisplay();
    }

    private void OnDestroy ()
    {
        if (BattleSceneLogic.Instance != null)
        {
            BattleSceneLogic.Instance.onTurnChanged -= OnTurnChanged;
        }
    }

    // ── Turn Management ────────────────────────────────────────

    private void OnTurnChanged (Turn turn, GameMode gameMode)
    {
        currentTurn = turn;

        if (autoShowCurrentPlayerWeapons)
        {
            RefreshWeaponDisplay();
        }

        // Update CP display for current player
        UpdateCPDisplay();

        Debug.Log($"[BattleWeaponManager] Turn changed to {turn}");
    }

    // ── Weapon Display ────────────────────────────────────────

    /// <summary>
    /// Load và hiển thị vũ khí của người chơi hiện tại
    /// </summary>
    private void RefreshWeaponDisplay ()
    {
        // Lấy player index từ turn
        int playerIndex = currentTurn == Turn.Player1 ? 1 : 2;
        var selectedWeapons = GameManager.Instance.GetSelectedWeapons(playerIndex);

        // Cập nhật CP display
        UpdateCPDisplay();

        // Đảm bảo container có VerticalLayoutGroup
        var layoutGroup = weaponButtonContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = weaponButtonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
        }

        // Clear old buttons
        ClearWeaponButtons();

        // Nếu không có vũ khí được chọn, hiển thị default
        if (selectedWeapons.Count == 0)
        {
            Debug.LogWarning($"[BattleWeaponManager] Player {playerIndex} has no selected weapons!");
            SetSelectedWeaponInfo(WeaponType.NormalShot);
            return;
        }

        // Tạo buttons cho mỗi vũ khí
        int buttonCount = 0;
        foreach (var weaponType in selectedWeapons)
        {
            CreateWeaponButton(weaponType);
            buttonCount++;
        }

        // Set first weapon as current
        if (buttonCount > 0)
        {
            currentWeapon = selectedWeapons[0];
            SelectWeapon(currentWeapon);
        }

        // Force rebuild layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(weaponButtonContainer.GetComponent<RectTransform>());

        Debug.Log($"[BattleWeaponManager] Loaded {buttonCount} weapons for Player {playerIndex}");
    }

    /// <summary>
    /// Tạo button cho một vũ khí
    /// </summary>
    private void CreateWeaponButton (WeaponType weaponType)
    {
        var weaponData = GameManager.Instance.weaponListData.GetWeaponByType(weaponType);
        if (weaponData == null)
        {
            Debug.LogError($"[BattleWeaponManager] WeaponData not found for type {weaponType}");
            return;
        }

        var buttonObj = Instantiate(weaponButtonPrefab, weaponButtonContainer);
        var buttonRect = buttonObj.GetComponent<RectTransform>();

        // Đặt lại anchor để nằm đúng trong container
        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(0.5f, 1);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
        }

        // Thêm LayoutElement nếu chưa có
        var layoutElement = buttonObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = 80; // Chiều cao button

        var battleWeaponButton = buttonObj.GetComponent<BattleWeaponButton>();

        if (battleWeaponButton != null)
        {
            int playerIndex = currentTurn == Turn.Player1 ? 1 : 2;
            battleWeaponButton.Setup(weaponType, weaponData, OnWeaponSelected, playerIndex);
            weaponButtons[weaponType] = battleWeaponButton;
        }
    }

    /// <summary>
    /// Xóa tất cả weapon buttons
    /// </summary>
    private void ClearWeaponButtons ()
    {
        foreach (Transform child in weaponButtonContainer)
        {
            Destroy(child.gameObject);
        }
        weaponButtons.Clear();
    }

    // ── Weapon Selection ──────────────────────────────────────

    /// <summary>
    /// Xử lý khi đã chọn vũ khí
    /// </summary>
    private void OnWeaponSelected (WeaponType weaponType)
    {
        SelectWeapon(weaponType);
    }

    /// <summary>
    /// Set vũ khí hiện tại
    /// </summary>
    public void SelectWeapon (WeaponType weaponType)
    {
        // Update previous button
        if (weaponButtons.ContainsKey(currentWeapon))
        {
            weaponButtons[currentWeapon].SetSelected(false);
        }

        // Update current weapon
        currentWeapon = weaponType;
        SetSelectedWeaponInfo(weaponType);

        // Update new button
        if (weaponButtons.ContainsKey(currentWeapon))
        {
            weaponButtons[currentWeapon].SetSelected(true);
        }

        Debug.Log($"[BattleWeaponManager] Selected weapon: {weaponType}");
    }

    /// <summary>
    /// Get vũ khí được chọn hiện tại
    /// </summary>
    public WeaponType GetCurrentWeapon () => currentWeapon;

    /// <summary>
    /// Get vũ khí được chọn hiện tại (WeaponData)
    /// </summary>
    public WeaponData GetCurrentWeaponData ()
    {
        return GameManager.Instance.weaponListData.GetWeaponByType(currentWeapon);
    }

    // ── UI Updates ────────────────────────────────────────────

    /// <summary>
    /// Cập nhật text hiển thị vũ khí hiện tại
    /// </summary>
    private void SetSelectedWeaponInfo (WeaponType weaponType)
    {
        var weaponData = GameManager.Instance.weaponListData.GetWeaponByType(weaponType);
        if (weaponData == null)
        {
            return;
        }

        if (selectedWeaponText != null)
        {
            selectedWeaponText.text = $"{weaponData.weaponName} in use";
        }
    }

    /// <summary>
    /// Cập nhật hiển thị CP của cả hai người chơi
    /// </summary>
    private void UpdateCPDisplay ()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        // Update Player 1 CP
        if (player1CPText != null)
        {
            int player1CP = GameManager.Instance.GetPlayerCP(1);
            player1CPText.text = $"{player1CP}";
        }

        // Update Player 2 CP
        if (player2CPText != null)
        {
            int player2CP = GameManager.Instance.GetPlayerCP(2);
            player2CPText.text = $"{player2CP}";
        }

        // Update weapon button availability
        UpdateWeaponButtonsAvailability();

        Debug.Log($"[BattleWeaponManager] Player 1 CP: {GameManager.Instance.GetPlayerCP(1)} | Player 2 CP: {GameManager.Instance.GetPlayerCP(2)}");
    }

    /// <summary>
    /// Cập nhật trạng thái available/disabled của các weapon button dựa trên CP
    /// </summary>
    private void UpdateWeaponButtonsAvailability ()
    {
        foreach (var kvp in weaponButtons)
        {
            var button = kvp.Value;
            if (button != null)
            {
                button.UpdateAvailability();
            }
        }
    }

    // ── Weapon Preview System ────────────────────────────────

    /// <summary>
    /// Shows weapon effect preview on the target grid at the hovered position.
    /// Called when mouse hovers over enemy grid with a weapon selected.
    /// </summary>
    public void ShowWeaponPreview (Vector2Int hoverPosition, GridManager targetGrid, WeaponType weaponType)
    {
        if (targetGrid == null)
        {
            return;
        }

        // Create weapon to get affected cells
        Weapon weapon = WeaponFactory.CreateWeapon(weaponType);
        if (weapon == null)
        {
            return;
        }

        // Get the affected cells for this weapon at this position
        List<Vector2Int> affectedCells = weapon.GetStrategy().GetAffectedCells(hoverPosition, targetGrid);

        // Show preview sprites on all affected cells
        foreach (var position in affectedCells)
        {
            Cell cell = targetGrid.GetCell(position);
            if (cell != null)
            {
                cell.spriteRenderer.sprite = cell.previewSprite;
            }
        }

        // Cleanup
        Destroy(weapon.gameObject);
    }

    /// <summary>
    /// Clears weapon effect preview (called on mouse exit).
    /// Resets all cells to their normal visual state.
    /// </summary>
    public void HideWeaponPreview (GridManager targetGrid)
    {
        if (targetGrid == null)
        {
            return;
        }

        // Reset all cells to their normal visuals
        for (int x = 0; x < targetGrid.gridWidth; x++)
        {
            for (int y = 0; y < targetGrid.gridHeight; y++)
            {
                Cell cell = targetGrid.GetCell(x, y);
                if (cell != null)
                {
                    cell.UpdateVisual();
                }
            }
        }
    }
}
