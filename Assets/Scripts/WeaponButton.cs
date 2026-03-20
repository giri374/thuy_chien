using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Component cho mỗi nút vũ khí trên UI
/// Hiển thị thông tin vũ khí và xử lý sự kiện click
/// </summary>
public class WeaponButton : MonoBehaviour
{
    [Header("UI References")]
    public Image weaponIconImage;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI goldCostText;
    public TextMeshProUGUI descriptionText;
    public Button selectButton;

    [Header("Configuration")]
    public WeaponType assignedWeaponType = WeaponType.NormalShot;

    private WeaponData weaponData;
    private Action<WeaponData> onWeaponSelected;
    private Action<WeaponData> onWeaponDeselected;
    private int availableGold = 0;
    private bool isSelected = false;

    /// <summary>
    /// Setup component với dữ liệu vũ khí
    /// </summary>
    public void Setup (WeaponData weapon, int gold, Action<WeaponData> onSelected, Action<WeaponData> onDeselected = null)
    {
        weaponData = weapon;
        availableGold = gold;
        onWeaponSelected = onSelected;
        onWeaponDeselected = onDeselected;
        isSelected = false;  // Reset selected state

        UpdateUI();
        SetupButton();
    }

    private void UpdateUI ()
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

        // Cập nhật giá vàng
        if (goldCostText != null)
        {
            goldCostText.text = $"{weaponData.goldCost}";
        }

        // Cập nhật mô tả
        if (descriptionText != null)
        {
            descriptionText.text = weaponData.description;
        }

        // Cập nhật availability
        UpdateAvailability(availableGold);
    }

    /// <summary>
    /// Cập nhật trạng thái button (enable/disable dựa trên gold)
    /// </summary>
    public void UpdateAvailability (int gold)
    {
        availableGold = gold;

        if (selectButton != null)
        {
            bool canAfford = weaponData != null && (availableGold >= weaponData.goldCost || isSelected);
            selectButton.interactable = canAfford;

            // Cập nhật màu sắc để hiện trạng thái
            var colors = selectButton.colors;
            if (isSelected)
            {
                colors.normalColor = new Color(1f, 0.8f, 0f, 1f); // Vàng - được chọn
            }
            else if (!canAfford)
            {
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Xám - không đủ gold
            }
            else
            {
                colors.normalColor = Color.white; // Trắng - có thể mua
            }
            selectButton.colors = colors;
        }
    }

    /// <summary>
    /// Set trạng thái selected của button
    /// </summary>
    public void SetSelected (bool selected)
    {
        isSelected = selected;
        UpdateAvailability(availableGold);
    }

    /// <summary>
    /// Get loại vũ khí được assign cho button này
    /// </summary>
    public WeaponType GetAssignedWeaponType () => assignedWeaponType;

    /// <summary>
    /// Get trạng thái selected
    /// </summary>
    public bool IsSelected () => isSelected;

    private void SetupButton ()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked ()
    {
        // Toggle: nếu đã chọn thì deselect, nếu chưa chọn thì select
        if (isSelected)
        {
            isSelected = false;
            onWeaponDeselected?.Invoke(weaponData);
        }
        else
        {
            isSelected = true;
            onWeaponSelected?.Invoke(weaponData);
        }
    }
}
