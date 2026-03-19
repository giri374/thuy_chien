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

    private WeaponType weaponType;
    private Action<WeaponType> onWeaponSelected;
    private bool isSelected = false;

    /// <summary>
    /// Setup button với dữ liệu vũ khí
    /// </summary>
    public void Setup (WeaponType type, WeaponData weaponData, Action<WeaponType> onSelected)
    {
        weaponType = type;
        onWeaponSelected = onSelected;
        isSelected = false;

        UpdateUI(weaponData);
        SetupButton();
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
        isSelected = true;
        UpdateVisuals();
        onWeaponSelected?.Invoke(weaponType);
    }
}
