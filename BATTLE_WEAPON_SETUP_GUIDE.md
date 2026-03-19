# BattleScene Weapon System - Setup Guide

## 📋 Tổng Quan

Hệ thống vũ khí cho BattleScene với các tính năng:
- ✅ Hiển thị vũ khí đã chọn của người chơi hiện tại
- ✅ Lựa chọn vũ khí bằng click
- ✅ Tự động cập nhật khi turn thay đổi
- ✅ Tracking current weapon cho mỗi turn

---

## 📁 Files Tạo Mới

### 1. **BattleWeaponButton.cs**
- Component cho mỗi vũ khí button trong panel
- Hiển thị icon, tên vũ khí
- Toggle selection state (white/green)
- Methods:
  - `Setup(WeaponType, WeaponData, Action<WeaponType>)`
  - `SetSelected(bool)`
  - `IsSelected()`
  - `GetWeaponType()`

### 2. **BattleWeaponManager.cs**
- Main manager cho weapon selection
- Lấy vũ khí từ GameManager.GetSelectedWeapons()
- Tạo buttons dynamically
- Tracks current weapon
- Subscribes to BattleSceneLogic.onTurnChanged
- Methods:
  - `RefreshWeaponDisplay()`
  - `SelectWeapon(WeaponType)`
  - `GetCurrentWeapon()`
  - `GetCurrentWeaponData()`

---

## 🎮 Game Flow

```
BattleScene Start
  ↓
BattleWeaponManager.Start()
  → Subscribe to BattleSceneLogic.onTurnChanged
  → Call RefreshWeaponDisplay()
    → Get player's selected weapons from GameManager
    → Create buttons for each weapon
    → Set first weapon as current
  ↓
Player clicks weapon button
  → BattleWeaponButton.OnButtonClicked()
  → Calls BattleWeaponManager.SelectWeapon()
  → Updates UI (green highlight)
  ↓
Turn changes
  → BattleSceneLogic fires onTurnChanged event
  → BattleWeaponManager.OnTurnChanged()
  → RefreshWeaponDisplay() for new player
```

---

## 🎨 UI Setup

### Canvas Hierarchy:
```
Canvas
├── Panel_WeaponSelection (NEW)
│   ├── Text: SelectedWeaponText (TextMeshProUGUI)
│   │   └── Display: "Selected: Weapon Name"
│   │
│   └── ScrollView (hoặc HorizontalLayoutGroup)
│       └── Content (Viewport)
│           └── [BattleWeaponButton Prefabs]
│               ├── Button: SelectButton
│               ├── Image: WeaponIcon
│               └── Text: WeaponName
│
└── [Other UI elements]
```

### Detailed Setup:

#### **Panel_WeaponSelection**
```
Type: Panel (Image)
Position: Bottom of screen or side panel
Layout: VerticalLayoutGroup
  ├── Child Force Expand Height: OFF
  ├── Child Force Expand Width: ON
  └── Spacing: 10
```

#### **SelectedWeaponText**
```
Type: TextMeshProUGUI
Text: "Selected: NormalShot"
Anchor: Top-Left
Font Size: 28
Color: White
```

#### **ScrollView**
```
Type: ScrollView
Direction: Horizontal (nếu buttons cạnh nhau)
          hoặc Vertical (nếu buttons chồng lên nhau)

Content (RectTransform):
  ├── Width: Preferred
  ├── Height: 100 (hoặc Preferred)
  └── LayoutGroup: HorizontalLayoutGroup hoặc VerticalLayoutGroup

Scrollbar: Tùy chọn
```

#### **BattleWeaponButton Prefab**
```
WeaponButton (Button)
├── RectTransform
│   └── Height: 80-100px
│   └── Width: 80-100px (nếu horizontal)
│
├── Button component
│   └── Target Graphic: Button Image
│
├── Image (weaponIconImage)
│   └── Display weapon icon
│
└── Text (weaponNameText)
    └── Show weapon name
```

---

## 🛠️ Inspector Setup

### **BattleWeaponManager** (Add to Canvas or Manager GameObject)

```
BattleWeaponManager
├── Selected Weapon Text = [SelectedWeaponText]
├── Weapon Button Container = [Content]
├── Weapon Button Prefab = [BattleWeaponButton prefab]
└── Auto Show Current Player Weapons = ON
```

### **BattleWeaponButton Prefab**

```
BattleWeaponButton
├── Weapon Icon Image = [Icon Image component]
├── Weapon Name Text = [WeaponNameText]
└── Select Button = [Button component]
```

---

## 🧪 Testing

### Test Case 1: Load Weapons
1. Start BattleScene (after WeaponSetupScene)
2. ✅ Weapon panel shows selected weapons
3. ✅ First weapon is highlighted (green)
4. ✅ Text shows "Selected: [Weapon Name]"

### Test Case 2: Switch Weapons
1. Click different weapon button
2. ✅ Previous button becomes white
3. ✅ New button becomes green
4. ✅ Text updates to show new weapon

### Test Case 3: Turn Change (PlayWithFriend)
1. Player 1's turn
2. ✅ Weapon panel shows Player 1's weapons
3. Switch turn to Player 2
4. ✅ Weapon panel updates with Player 2's weapons
5. ✅ First Player 2 weapon is selected

### Test Case 4: No Weapons Selected
1. Enter BattleScene without selecting weapons
2. ✅ Should fallback to NormalShot
3. ✅ No errors in console

---

## 💻 Code Integration

### How to Get Current Weapon in Attack Code:

```csharp
// In BattleSceneLogic or attack handler
private BattleWeaponManager weaponManager;

private void Start()
{
    weaponManager = GetComponent<BattleWeaponManager>();
}

public void ExecuteAttack(Vector2Int position)
{
    WeaponType currentWeapon = weaponManager.GetCurrentWeapon();
    WeaponData weaponData = weaponManager.GetCurrentWeaponData();
    
    // Use currentWeapon in attack logic
    CreateAndExecuteAttackCommandAsync(currentWeapon, position, BattleSceneLogic.Instance.currentTurn);
}
```

---

## 🔧 Architecture

```
BattleWeaponManager (Singleton-like in scene)
├── Listens to: BattleSceneLogic.onTurnChanged
├── Reads from: GameManager.GetSelectedWeapons(playerIndex)
├── Manages: BattleWeaponButton instances
└── Provides: GetCurrentWeapon(), GetCurrentWeaponData()

BattleWeaponButton (Instantiated by BattleWeaponManager)
├── UI Event: selectButton.onClick
└── Callback: onWeaponSelected (Action<WeaponType>)
```

---

## 📊 Data Flow

```
GameManager
├── player1SelectedWeapons: List<WeaponType>
└── player2SelectedWeapons: List<WeaponType>
    ↓
BattleWeaponManager.RefreshWeaponDisplay()
    ↓ GetSelectedWeapons(playerIndex)
    ↓
For each WeaponType
    ├── Get WeaponData from weaponListData
    ├── Create BattleWeaponButton
    └── Add to UI
    ↓
User clicks button
    ↓
SelectWeapon(WeaponType)
    ├── Update button visuals
    ├── Update selectedWeaponText
    └── Store in currentWeapon
```

---

## 🚀 Next Steps

1. Create UI elements in BattleScene
2. Add BattleWeaponManager to manager GameObject or Canvas
3. Create BattleWeaponButton prefab
4. Assign all references in Inspector
5. Integrate with attack execution code:
   - Use `BattleWeaponManager.GetCurrentWeapon()` in attack handler
   - Pass to `CreateAndExecuteAttackCommandAsync()`

---

## 📝 Notes

- BattleWeaponManager automatically unsubscribes from events in OnDestroy()
- Weapons are loaded from GameManager's selected weapons list
- First weapon is always selected by default
- Button color: White (unselected) → Green (selected)
- Compatible with both PlayWithBot and PlayWithFriend modes

---

**Last Updated**: March 20, 2026
**Status**: Ready for Integration
