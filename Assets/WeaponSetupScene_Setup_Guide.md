# WeaponSetupScene - Hướng Dẫn Thiết Lập

## Tổng Quan
WeaponSetupScene cho phép người chơi chọn vũ khí trước khi vào BattleScene.
- **Gold**: Được tải từ Cloud (ProgressManager)
- **Vũ khí**: Được tải từ WeaponListData ScriptableObject
- **PlayWithFriend**: WeaponSetupScene chạy 2 lần (Player 1, Player 2)

## Game Flow
```
MenuScene 
  ↓ (chọn AdvancedMap)
WeaponSetupScene (Player 1 chọn vũ khí)
  ↓
[Nếu PlayWithFriend] WeaponSetupScene (Player 2 chọn vũ khí) → SetupScene
[Nếu PlayWithBot] → SetupScene
  ↓
BattleScene (Chiến đấu)
```

## 1. Tạo WeaponListData ScriptableObject

### Bước 1: Tạo Weapon List Asset
1. Trong Project window, navigate: `Assets > Data`
2. Right-click → Create → BattleShip → Weapon List
3. Đặt tên: `WeaponList`

### Bước 2: Thêm Vũ Khí
1. Chọn WeaponList vừa tạo
2. Trong Inspector, click "+" để thêm vũ khí
3. Với mỗi vũ khí, drag WeaponData assets vào list

**Vũ khí cần tạo** (nếu chưa có):
- NormalShot (gold cost: 10)
- NuclearBomb (gold cost: 50)
- Bomber (gold cost: 40)
- Torpedoes (gold cost: 30)
- Radar (gold cost: 25)
- AntiAircraft (gold cost: 35)

## 2. Assign WeaponListData vào GameManager

1. Mở scene chứa GameManager (thường là MenuScene)
2. Chọn GameManager object trong Hierarchy
3. Trong Inspector, tìm section [Weapon Data]
4. Drag WeaponList asset vào "Weapon List Data" field

## 3. Tạo WeaponSetupScene

### Bước 1: Tạo Scene mới
1. File → New Scene → 2D
2. Đặt tên: `WeaponSetupScene`
3. Save vào: `Assets/Scenes/`

### Bước 2: Setup Canvas and UI
1. Tạo Canvas (hoặc dùng auto-create với UI element)
2. Rename: `WeaponSetupCanvas`
3. Thêm các UI elements sau:

#### UI Hierarchy:
```
Canvas (WeaponSetupCanvas)
├── Panel (Main Background)
├── Panel_Header
│   ├── PlayerTitleText (TextMeshProUGUI)
│   └── CurrentGoldText (TextMeshProUGUI)
├── ScrollView (Weapon Selection)
│   └── Content
│       └── [Weapon Button Prefabs]
└── Panel_Buttons
    ├── ConfirmButton (Button)
    └── CancelButton (Button)
```

### Bước 3: Tạo Weapon Button Prefab

1. Tạo GameObject mới: `WeaponButton`
2. Thêm components:
   - Button component
   - Layout Element (nếu cần)
   
3. Tạo child elements:
   ```
   WeaponButton (Button)
   ├── Image (weapon icon)
   ├── WeaponNameText (TextMeshProUGUI)
   ├── GoldCostText (TextMeshProUGUI)
   └── DescriptionText (TextMeshProUGUI)
   ```

4. Configure Button:
   - Thêm Image component (để hiển thị icon)
   - Set Target Graphic = Image

5. Thêm WeaponButton.cs script:
   - Inspector → Add Component → WeaponButton
   - Assign references:
     - Weapon Icon Image = [Image component]
     - Weapon Name Text = [WeaponNameText]
     - Gold Cost Text = [GoldCostText]
     - Description Text = [DescriptionText]
     - Select Button = [Button component]

6. Lưu prefab:
   - Drag WeaponButton từ Hierarchy vào: `Assets/Prefabs/WeaponButton.prefab`
   - Delete từ scene

## 4. Setup WeaponSetupScene

### Bước 1: Tạo GameManager reference
1. Nếu chưa có GameManager trong scene, thêm:
   - Scene menu không cần GameManager (nó từ MenuScene dùng DontDestroyOnLoad)
   
### Bước 2: Thêm WeaponSetupUIManager
1. Tạo empty GameObject: `WeaponSetupUIManager`
2. Add Component → WeaponSetupUIManager
3. Assign references trong Inspector:
   - Player Title Text = [PlayerTitleText]
   - Current Gold Text = [CurrentGoldText]
   - Weapon Button Container = [Content (ScrollView)]
   - Confirm Button = [ConfirmButton]
   - Cancel Button = [CancelButton]
   - Weapon Button Prefab = [WeaponButton.prefab]

### Bước 3: Verify SceneNames
- Đảm bảo SceneNames.cs có:
  ```csharp
  public const string WeaponSetup = "WeaponSetupScene";
  public const string Setup = "SetupScene";
  public const string Battle = "BattleScene";
  public const string MainMenu = "MenuScene";
  ```

## 5. ProgressManager Setup (nếu chưa có)

Đảm bảo MenuScene có ProgressManager:
1. Tạo empty GameObject: `ProgressManager`
2. Add Component → ProgressManager
3. Mark: DontDestroyOnLoad

## 6. Testing

### Test Case 1: PlayWithBot (NormalMap)
1. Start from MenuScene
2. Click "Play with Bot" → "Normal Map"
3. Xác nhận: Chuyển thẳng đến SetupScene (KHÔNG vào WeaponSetupScene)

### Test Case 2: PlayWithBot (AdvancedMap)
1. Start from MenuScene
2. Click "Play with Bot" → "Advanced Map"
3. Xác nhận: Vào WeaponSetupScene
4. Chọn vũ khí, gold giảm
5. Click Confirm → SetupScene

### Test Case 3: PlayWithFriend (AdvancedMap)
1. Start from MenuScene
2. Click "Play with Friend" → "Advanced Map"
3. Xác nhận: Player 1 vào WeaponSetupScene
4. Player 1 chọn vũ khí, click Confirm
5. Xác nhận: Player 2 vào WeaponSetupScene (text hiển thị "Player 2 - Weapon Setup")
6. Player 2 chọn vũ khí, click Confirm
7. Xác nhận: Chuyển đến SetupScene (Player 1)

## Data Flow

### Gold Loading
```
ProgressManager.Data.gold 
  → WeaponSetupUIManager.LoadWeaponSetupData()
  → GameManager.SetPlayerGold()
  → WeaponSetupUIManager.UpdateGoldText()
```

### Weapon Selection
```
OnWeaponSelected()
  → Check if gold >= weapon.goldCost
  → Deduct gold: currentGold -= weapon.goldCost
  → GameManager.AddWeapon()
  → GameManager.SetPlayerGold()
  → RefreshWeaponButtons()
```

### Scene Transitions
```
ConfirmButton click
  → if PlayWithFriend && Player 1: Load WeaponSetupScene (Player 2)
  → else: Load SetupScene
  
CancelButton click
  → Clear weapons & gold
  → Load MainMenu
```

## Kỹ Thuật Chiến Đấu (Battle Scene)

Khi vào BattleScene, các vũ khí được chọn sẽ được sử dụng:
```csharp
var selectedWeapons = GameManager.Instance.GetSelectedWeapons(currentPlayer);
// Sử dụng selectedWeapons trong logic tấn công
```

## Troubleshooting

| Vấn đề | Nguyên Nhân | Giải Pháp |
|--------|-----------|----------|
| WeaponListData is null | Không assign vào GameManager | Assign WeaponList vào GameManager.weaponListData |
| Button không respond | Prefab không setup đúng | Check WeaponButton.cs có assign SelectButton không |
| Gold không load | ProgressManager không tìm thấy | Ensure ProgressManager có trong MenuScene |
| Scene không load | SceneNames sai | Check SceneNames.cs có tên scene chính xác |

---
**Ngày cập nhật**: 2026-03-20
**Phiên bản**: 1.0
