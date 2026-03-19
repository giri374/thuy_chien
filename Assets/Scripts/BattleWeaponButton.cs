using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Component cho mỗi nút vũ khí trong BattleScene
/// Hiển thị thông tin vũ khí và xử lý lựa chọn
/// </summary>
public class BattleWeaponButton : MonoBehaviour
{
    [Header("UI References")]
    public Image weaponIconImage;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponCPCostText;
    public Button selectButton;

    // Visual state tracking
    private Color selectedColor = Color.green;
    private Color normalColor = Color.white;
    private Color disabledColor = Color.gray;

    private WeaponType weaponType;
    private WeaponData currentWeaponData;
    private int playerIndex;
    private Action<WeaponType> onWeaponSelected;
    private bool isSelected = false;

    /// <summary>
    /// Setup button với dữ liệu vũ khí
    /// </summary>
    public void Setup (WeaponType type, WeaponData weaponData, Action<WeaponType> onSelected, int currentPlayerIndex = 1)
    {
        weaponType = type;
        currentWeaponData = weaponData;
        playerIndex = currentPlayerIndex;
        onWeaponSelected = onSelected;
        isSelected = false;

        UpdateUI(weaponData);
        SetupButton();
        UpdateAvailability();
    }

    private void UpdateUI (WeaponData weaponData)
    {
        if (weaponData == null)
        {
            return;
        }

        // Cập nhật icon
        if (weaponIconImage != null && weaponData.icon != null)
        {
            weaponIconImage.sprite = weaponData.icon;
        }

        // Cập nhật tên vũ khí
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponData.weaponName;
        }

        // Cập nhật CP cost
        if (weaponCPCostText != null)
        {
            weaponCPCostText.text = $"CP: {weaponData.cpCost}";
        }
    }

    private void SetupButton ()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Set trạng thái selected
    /// </summary>
    public void SetSelected (bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }

    /// <summary>
    /// Get trạng thái selected
    /// </summary>
    public bool IsSelected () => isSelected;

    /// <summary>
    /// Get weapon type
    /// </summary>
    public WeaponType GetWeaponType () => weaponType;

    /// <summary>
    /// Cập nhật màu sắc button
    /// </summary>
    private void UpdateVisuals ()
    {
        if (selectButton != null)
        {
            var colors = selectButton.colors;
            colors.normalColor = isSelected ? selectedColor : normalColor;
            selectButton.colors = colors;
        }
    }

    private void OnButtonClicked ()
    {
        // Check CP trước khi select
        if (!CanSelectWeapon())
        {
            Debug.LogWarning($"[BattleWeaponButton] Player {playerIndex} không đủ CP để sử dụng {weaponType}");
            return;
        }

        isSelected = true;
        Debug.Log($"[BattleWeaponButton] Player {playerIndex} selected weapon: {weaponType}");
        UpdateVisuals();
        onWeaponSelected?.Invoke(weaponType);
    }

    /// <summary>
    /// Check xem có thể select vũ khí không (kiểm tra CP)
    /// </summary>
    private bool CanSelectWeapon ()
    {
        // NormalShot không cần CP cost
        if (weaponType == WeaponType.NormalShot)
        {
            return true;
        }

        // Check CP cho vũ khí khác
        if (currentWeaponData == null || GameManager.Instance == null)
        {
            return false;
        }

        int playerCP = GameManager.Instance.GetPlayerCP(playerIndex);
        return playerCP >= currentWeaponData.cpCost;
    }

    /// <summary>
    /// Update availability (enabled/disabled) dựa trên CP hiện tại
    /// </summary>
    public void UpdateAvailability ()
    {
        bool canSelect = CanSelectWeapon();
        if (selectButton != null)
        {
            selectButton.interactable = canSelect;
        }
    }
}
