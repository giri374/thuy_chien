# WeaponSetupScene - Implementation Summary

## 📋 Tổng Quan
Implemented complete WeaponSetupScene system với:
- ✅ Gold loading từ Cloud (ProgressManager)
- ✅ Weapon selection UI với gold deduction
- ✅ Multi-player support (PlayWithBot, PlayWithFriend)
- ✅ Proper scene flow navigation

## 📁 Files Created/Modified

### 1. **WeaponListData.cs** (NEW)
**Location**: `Assets/Data/WeaponListData.cs`
- ScriptableObject để quản lý danh sách vũ khí
- Methods: GetWeaponByType(), GetWeaponByName()
- Reference: Assign vào GameManager.weaponListData

### 2. **GameManager.cs** (MODIFIED)
**Location**: `Assets/Scripts/GameManager.cs`
**Changes**:
- Thêm `WeaponListData weaponListData` reference
- Thêm weapon selection data:
  - `player1SelectedWeapons`, `player2SelectedWeapons`
  - `player1Gold`, `player2Gold`
- Thêm methods:
  - `SetPlayerGold(playerIndex, goldAmount)`
  - `GetPlayerGold(playerIndex)`
  - `AddWeapon(playerIndex, weaponType)`
  - `RemoveWeapon(playerIndex, weaponType)`
  - `GetSelectedWeapons(playerIndex)`
  - `ClearWeapons(playerIndex)`
  - `ClearAllWeapons()`

### 3. **WeaponSetupUIManager.cs** (NEW)
**Location**: `Assets/Scripts/WeaponSetupUIManager.cs`
**Responsibility**: Main manager cho WeaponSetupScene
**Features**:
- Creates weapon buttons dynamically từ WeaponListData
- Handles weapon selection với gold deduction
- Manages multi-player flow (Player 1 → Player 2)
- Scene transitions to SetupScene

**Key Methods**:
- `LoadWeaponSetupData()`: Tải gold từ ProgressManager
- `CreateWeaponButtons()`: Tạo UI buttons cho từng vũ khí
- `OnWeaponSelected()`: Xử lý khi người chơi chọn vũ khí
- `OnConfirmClicked()`: Chuyển sang scene tiếp theo
- `OnCancelClicked()`: Quay lại MainMenu

### 4. **WeaponButton.cs** (NEW)
**Location**: `Assets/Scripts/WeaponButton.cs`
**Responsibility**: Individual weapon button UI component
**Features**:
- Displays weapon icon, name, gold cost
- Handles button click to select weapon
- Updates button state based on available gold (enable/disable)
- Shows weapon description

**Key Methods**:
- `Setup()`: Initialize với WeaponData
- `UpdateAvailability()`: Enable/disable dựa trên gold

## 🎮 Game Flow

### AdvancedMap + PlayWithBot
```
MenuScene (select PlayWithBot + AdvancedMap)
    ↓
WeaponSetupScene (Player 1 selects weapons)
    ↓ [Confirm]
SetupScene (Player 1 places ships)
    ↓ [Confirm]
BattleScene
```

### AdvancedMap + PlayWithFriend
```
MenuScene (select PlayWithFriend + AdvancedMap)
    ↓
WeaponSetupScene (Player 1 selects weapons)
    ↓ [Confirm]
WeaponSetupScene (Player 2 selects weapons)
    ↓ [Confirm]
SetupScene (Player 1 places ships)
    ↓ [Confirm / Pass Device]
SetupScene (Player 2 places ships)
    ↓ [Confirm]
BattleScene
```

### NormalMap (any mode)
```
MenuScene (select any mode + NormalMap)
    ↓
SetupScene (ships placement)
    ↓
BattleScene
(WeaponSetupScene is SKIPPED)
```

## 📊 Data Flow

### Gold Loading
```
1. Player enters WeaponSetupScene
2. WeaponSetupUIManager checks: GameManager.GetPlayerGold(currentPlayer) == 0?
3. If yes: Load from ProgressManager.Data.gold
4. GameManager.SetPlayerGold(currentPlayer, loadedGold)
5. UI displays: "Gold: {currentGold}"
```

### Weapon Selection
```
1. Player clicks weapon button
2. OnWeaponSelected(weapon) called
3. Check: currentGold >= weapon.goldCost?
4. If yes:
   - currentGold -= weapon.goldCost
   - GameManager.AddWeapon(currentPlayer, weapon.type)
   - GameManager.SetPlayerGold(currentPlayer, currentGold)
   - RefreshWeaponButtons() (update UI)
5. If no: ShowInsufficientGoldMessage()
```

### Scene Transition (Confirm)
```
1. Player clicks Confirm button
2. Check: PlayWithFriend && currentPlayer == 1?
3. If yes:
   - GameManager.SetCurrentSetupPlayer(2)
   - Reload WeaponSetupScene (Player 2 setup)
4. If no:
   - Load SetupScene
```

## 🔧 Inspector Setup (Required)

### GameManager (in MenuScene)
**Assign in Inspector**:
- [Weapon Data] → Weapon List Data: `WeaponList` (ScriptableObject)

### WeaponSetupScene
**Canvas Structure**:
```
Canvas
├── Panel (Background)
├── PlayerTitleText (TextMeshProUGUI)
├── CurrentGoldText (TextMeshProUGUI)
├── ScrollView
│   └── Content (for weapon buttons)
├── ConfirmButton
└── CancelButton
```

**WeaponSetupUIManager (in scene)**:
- Player Title Text: [PlayerTitleText]
- Current Gold Text: [CurrentGoldText]
- Weapon Button Container: [ScrollView/Content]
- Confirm Button: [ConfirmButton]
- Cancel Button: [CancelButton]
- Weapon Button Prefab: [WeaponButton.prefab]

**WeaponButton Prefab Structure**:
```
WeaponButton (Button)
├── Image (icon)
├── WeaponNameText (TextMeshProUGUI)
├── GoldCostText (TextMeshProUGUI)
└── DescriptionText (TextMeshProUGUI)
```

## ✅ Features Implemented

### Gold Management
- ✅ Load gold from cloud (ProgressManager.Data.gold)
- ✅ Each player gets their own gold pool
- ✅ Gold deducted when weapon selected
- ✅ Display current gold in UI

### Weapon Selection
- ✅ Display weapons from WeaponListData
- ✅ Show weapon cost, icon, description
- ✅ Check if enough gold before allowing selection
- ✅ Add weapon to player's selected weapons list
- ✅ Prevent duplicate weapon selection

### Multi-Player Support
- ✅ PlayWithBot: Single player weapon selection
- ✅ PlayWithFriend: Two rounds (Player 1, then Player 2)
- ✅ Each player has independent gold and weapon selection
- ✅ Scene reload for Player 2 with proper UI update

### Scene Navigation
- ✅ AdvancedMap uses WeaponSetupScene
- ✅ NormalMap skips WeaponSetupScene
- ✅ Proper transition to SetupScene
- ✅ Cancel button returns to MainMenu

### Error Handling
- ✅ ProgressManager not found → fallback to 100 gold
- ✅ WeaponListData not found → error logging
- ✅ Insufficient gold → warning message
- ✅ Validation before scene transitions

## 🧪 Testing Checklist

- [ ] Create WeaponList ScriptableObject
- [ ] Add weapons to WeaponList
- [ ] Assign WeaponList to GameManager
- [ ] Create WeaponSetupScene
- [ ] Setup UI (Canvas, buttons, texts)
- [ ] Create WeaponButton prefab
- [ ] Assign all inspector references
- [ ] Test PlayWithBot + AdvancedMap
- [ ] Test PlayWithFriend + AdvancedMap
- [ ] Test NormalMap (no WeaponSetupScene)
- [ ] Test gold loading from ProgressManager
- [ ] Test insufficient gold message
- [ ] Test weapon selection deduction
- [ ] Test Player 2 scene reload
- [ ] Test Cancel button navigation

## 📝 Code Quality
- ✅ Full Vietnamese comments
- ✅ Proper error handling
- ✅ Defensive checks for null references
- ✅ Consistent naming conventions
- ✅ Modular design (separate concerns)
- ✅ Reusable components

## 🚀 Future Enhancements
- Add weapon limit (max X weapons per player)
- Add weapon preview/tooltip
- Add toast notifications for insufficient gold
- Track weapon selection history
- Add weapon special effects preview
- Implement PlayOnline with different gold per player
