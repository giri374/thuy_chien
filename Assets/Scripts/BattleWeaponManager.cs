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

    [Header("Weapon Buttons (Pre-placed)")]
    public BattleWeaponButton[] weaponButtons;

    [Header("Settings")]
    public bool autoShowCurrentPlayerWeapons = true;

    // Current state
    private Turn currentTurn = Turn.Player1;
    private WeaponType currentWeapon = WeaponType.NormalShot;
    private Dictionary<WeaponType, BattleWeaponButton> weaponButtonMap = new Dictionary<WeaponType, BattleWeaponButton>();

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
    /// Setup vũ khí của người chơi hiện tại cho các pre-placed buttons
    /// </summary>
    private void RefreshWeaponDisplay ()
    {
        // Lấy player index từ turn
        int playerIndex = currentTurn == Turn.Player1 ? 1 : 2;
        var selectedWeapons = GameManager.Instance.GetSelectedWeapons(playerIndex);

        // Cập nhật CP display
        UpdateCPDisplay();

        // Setup tất cả các buttons từ array
        SetupWeaponButtons(playerIndex, selectedWeapons);

        // Set first weapon as current
        if (selectedWeapons.Count > 0)
        {
            currentWeapon = selectedWeapons[0];
            SelectWeapon(currentWeapon);
        }
        else
        {
            Debug.LogWarning($"[BattleWeaponManager] Player {playerIndex} has no selected weapons!");
            SetSelectedWeaponInfo(WeaponType.NormalShot);
        }

        Debug.Log($"[BattleWeaponManager] Setup weapons for Player {playerIndex}");
    }

    /// <summary>
    /// Setup tất cả pre-placed buttons dựa vào vũ khí đã chọn của player
    /// </summary>
    private void SetupWeaponButtons (int playerIndex, List<WeaponType> selectedWeapons)
    {
        if (weaponButtons == null || weaponButtons.Length == 0)
        {
            Debug.LogError("[BattleWeaponManager] No weapon buttons assigned in inspector!");
            return;
        }

        // Xây dựng dictionary: assignedWeaponType -> button
        weaponButtonMap.Clear();
        foreach (var button in weaponButtons)
        {
            if (button != null)
            {
                var assignedType = button.GetAssignedWeaponType();
                weaponButtonMap[assignedType] = button;
            }
        }

        // Setup từng button
        foreach (var button in weaponButtons)
        {
            if (button == null) continue;

            var assignedType = button.GetAssignedWeaponType();
            var weaponData = GameManager.Instance.weaponListData.GetWeaponByType(assignedType);

            if (weaponData == null)
            {
                Debug.LogError($"[BattleWeaponManager] WeaponData not found for type {assignedType}");
                continue;
            }

            // Setup button với weapon data
            button.Setup(assignedType, weaponData, OnWeaponSelected, playerIndex);

            // Update interactable state dựa vào selected weapons list + CP
            // Button sẽ hiển thị tất cả, chỉ set interactable = false nếu không được phép sử dụng
            button.gameObject.SetActive(true);
            button.UpdateAvailability();
        }
    }

    /// <summary>
    /// Reset weapon button listeners để tránh duplicate callbacks
    /// </summary>
    private void ResetWeaponButtonListeners ()
    {
        if (weaponButtons == null || weaponButtons.Length == 0) return;

        foreach (var button in weaponButtons)
        {
            if (button != null && button.selectButton != null)
            {
                button.selectButton.onClick.RemoveAllListeners();
            }
        }
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
        if (weaponButtonMap.ContainsKey(currentWeapon))
        {
            weaponButtonMap[currentWeapon].SetSelected(false);
        }

        // Update current weapon
        currentWeapon = weaponType;
        SetSelectedWeaponInfo(weaponType);

        // Update new button
        if (weaponButtonMap.ContainsKey(currentWeapon))
        {
            weaponButtonMap[currentWeapon].SetSelected(true);
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
    /// Public API để force refresh CP UI ngay khi CP thay đổi.
    /// </summary>
    public void RefreshCPDisplay ()
    {
        UpdateCPDisplay();
    }

    /// <summary>
    /// Cập nhật trạng thái available/disabled của các weapon button dựa trên CP
    /// </summary>
    private void UpdateWeaponButtonsAvailability ()
    {
        if (weaponButtons == null || weaponButtons.Length == 0) return;

        foreach (var button in weaponButtons)
        {
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
