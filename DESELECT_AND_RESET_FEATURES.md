# WeaponSetupScene - Deselect & Reset Features

## 🎯 Tính Năng Mới

### 1. **Deselect Vũ Khí** (Toggle Selection)
- Người chơi có thể **click lại** vũ khí đã chọn để **bỏ chọn**
- Khi bỏ chọn, **gold được hoàn lại**
- Button thay đổi màu sắc để hiển thị trạng thái:
  - **Trắng**: Có thể mua (đủ gold)
  - **Xanh**: Đã chọn
  - **Xám**: Không thể mua (không đủ gold)

### 2. **Reset Button**
- Nút mới để **reset tất cả lựa chọn**
- Xóa tất cả vũ khí đã chọn
- Phục hồi gold về giá trị ban đầu
- Đưa tất cả buttons về trạng thái ban đầu

---

## 📝 Thay Đổi Code

### **WeaponButton.cs**

#### Thêm tracking state:
```csharp
private bool isSelected = false;  // Track whether weapon is selected
private Action<WeaponData> onWeaponDeselected;  // Callback for deselect
```

#### Cập nhật `Setup()` method:
```csharp
public void Setup(WeaponData weapon, int gold, Action<WeaponData> onSelected, Action<WeaponData> onDeselected = null)
```
- Thêm parameter `onDeselected` callback
- Initialize `isSelected = false`

#### Cập nhật `UpdateAvailability()`:
- Nếu `isSelected` → Màu xanh (0.2f, 0.8f, 0.2f)
- Nếu kiếm không đủ và chưa chọn → Màu xám (0.5f, 0.5f, 0.5f)
- Nếu có thể mua → Trắng

#### Thêm methods:
```csharp
public void SetSelected(bool selected)  // Set selected state
public bool IsSelected() => isSelected;  // Get selected state
```

#### Cập nhật `OnButtonClicked()`:
- **Toggle logic**: 
  - Nếu đã chọn → gọi `onWeaponDeselected`
  - Nếu chưa chọn → gọi `onWeaponSelected`

---

### **WeaponSetupUIManager.cs**

#### Thêm references:
```csharp
public Button resetButton;  // Nút reset
private int initialGold = 0;  // Lưu gold ban đầu
private Dictionary<WeaponType, WeaponButton> selectedWeaponsMap;  // Map weapons
```

#### Cập nhật `LoadWeaponSetupData()`:
```csharp
initialGold = currentGold;  // Lưu gold ban đầu
```

#### Cập nhật `CreateWeaponButtons()`:
- Pass `OnWeaponDeselected` callback khi setup
- Clear `selectedWeaponsMap` trước tạo buttons
- Store mapping vào `selectedWeaponsMap[weapon.type] = weaponButton`

#### Thêm `OnWeaponDeselected()` method:
```csharp
private void OnWeaponDeselected(WeaponData weapon)
{
    currentGold += weapon.goldCost;  // Hoàn lại gold
    GameManager.Instance.SetPlayerGold(currentPlayer, currentGold);
    GameManager.Instance.RemoveWeapon(currentPlayer, weapon.type);
    UpdateGoldText();
    RefreshWeaponButtons();
}
```

#### Cập nhật `SetupButtonListeners()`:
```csharp
if (resetButton != null)
{
    resetButton.onClick.AddListener(OnResetClicked);
}
```

#### Thêm `OnResetClicked()` method:
```csharp
private void OnResetClicked()
{
    GameManager.Instance.ClearWeapons(currentPlayer);
    currentGold = initialGold;
    GameManager.Instance.SetPlayerGold(currentPlayer, currentGold);
    selectedWeaponsMap.Clear();
    RefreshWeaponButtons();
    UpdateGoldText();
}
```

---

## 🎨 UI Setup

### Canvas Hierarchy (Updated):
```
Canvas
├── Panel (Background)
├── Panel_Header
│   ├── PlayerTitleText
│   └── CurrentGoldText
├── ScrollView
│   └── Content
│       └── [Weapon Button Prefabs]
└── Panel_Buttons
    ├── ConfirmButton
    ├── CancelButton
    └── ResetButton (NEW)  ← Thêm button này
```

### Inspector Assignment:
**WeaponSetupUIManager**:
- Reset Button = [ResetButton]

---

## 🧪 Testing

### Test Case 1: Select & Deselect
1. Start WeaponSetupScene
2. Click weapon (button chuyển xanh, gold giảm)
3. Click lại weapon (button chuyển trắng, gold hoàn lại)
4. ✅ Verify gold value restored

### Test Case 2: Reset Button
1. Select multiple weapons
2. Click "Reset" button
3. ✅ All buttons return to white
4. ✅ Gold returns to initial value

### Test Case 3: Select After Reset
1. Select weapons, reset
2. Select different weapons
3. ✅ Gold calculation still correct

### Test Case 4: Deselect & Reselect
1. Select weapon A (gold -= 50)
2. Deselect weapon A (gold += 50)
3. Select weapon B (gold -= 40)
4. ✅ Final gold = initial - 40

---

## 🔄 Behavior Summary

| Action | Gold | Button Color | GameManager |
|--------|------|--------------|-------------|
| Click selectable | -cost | Xanh | AddWeapon |
| Click selected | +cost | Trắng | RemoveWeapon |
| Click unaffordable | - | Xám | - |
| Click Reset | Reset | Trắng | ClearWeapons |

---

## 📋 Checklist

- [x] WeaponButton supports deselect
- [x] WeaponButton tracks selected state
- [x] WeaponButton visual feedback (colors)
- [x] WeaponSetupUIManager tracks initial gold
- [x] OnWeaponDeselected method added
- [x] Reset button listener setup
- [x] OnResetClicked method added
- [x] Dictionary for weapon mapping

---

**Next Step**: Create the ResetButton in your WeaponSetupScene UI and assign it in Inspector!
