# Game Workflow & Data Saving Documentation

## 📋 Mục Lục
1. [Luồng Hoạt Động Chung](#luồng-hoạt-động-chung)
2. [Chi Tiết Từng Scene](#chi-tiết-từng-scene)
3. [Dữ Liệu Được Lưu](#dữ-liệu-được-lưu)
4. [Cấu Trúc Dữ Liệu](#cấu-trúc-dữ-liệu)
5. [Class Chính & Trách Nhiệm](#class-chính--trách-nhiệm)

---

## 🎮 Luồng Hoạt Động Chung

### Quy Trình Game Tổng Quát

```
┌─────────────┐
│  Main Menu  │
└──────┬──────┘
       │
       ├─→ PlayWithBot
       │   └─→ Setup Scene (P1 đặt tàu)
       │       └─→ Bot đặt tàu ngẫu nhiên
       │           └─→ Battle Scene (P1 vs Bot)
       │               └─→ Game Over
       │                   └─→ Save Progress
       │
       └─→ PlayWithFriend
           └─→ Setup Scene (P1 đặt tàu)
               └─→ Pass Device Panel
                   └─→ Setup Scene (P2 đặt tàu)
                       └─→ Pass Device Panel
                           └─→ Battle Scene (P1 vs P2)
                               └─→ Game Over
                                   └─→ Save Progress
```

---

## 🎯 Chi Tiết Từng Scene

### 1️⃣ Setup Scene (SetupSceneManager)

**Mục đích:** Người chơi setup vị trí tàu trước khi battle

#### Flow:
```
Start Setup
   ↓
Initialize Grid
   ↓
Player drag/rotate ships
   ↓
Can Place?
   ├─YES → Ship placed on grid → Save to GameManager
   └─NO  → Cancel placement
   ↓
Player clicks "Confirm"?
   ├─YES → Proceed to Next Step
   │       ├─PlayWithBot     → Go to Battle
   │       └─PlayWithFriend  → Show Pass Device Panel → P2 Setup
   └─NO  → Continue Setup
```

#### Hành Động Chính:

| Nút | Hành Động | Kết Quả |
|-----|-----------|---------|
| **Confirm** | Xác nhận vị trí tàu | Kiểm tra xem đã đặt tàu → Chuyển scene |
| **Reset** | Xóa tất cả placement | Grid reset, tàu trả về vị trí gốc |
| **Random** | Random placement | Tự động đặt tàu ở vị trí hợp lệ |
| **Rotate (Click)** | Xoay tàu 90 độ | Dọc ↔ Ngang |

#### Data Lưu:
- **ShipPlacementData**: position, shipID, isHorizontal → `GameManager`

---

### 2️⃣ Battle Scene (BattleSceneManager)

**Mục đích:** Hai người chơi/Bot lượt lượt tấn công nhau

#### Flow:
```
Load All Ships (từ GameManager & Bot random)
   ↓
P1's Turn:
   ├─ Click opponent's cell
   │  ├─HIT  → Cell state = Hit → Bonus turn
   │  └─MISS → Cell state = Empty → Switch turn
   └─ All opponent ships sunk? → END GAME (P1 WINS)
   ↓
P2's/Bot's Turn:
   ├─ (PlayWithFriend) Show Pass Device
   ├─ (PlayWithBot) Bot auto-attacks
   │  ├─HIT  → Bonus turn for Bot
   │  └─MISS → Switch to P1
   └─ All opponent ships sunk? → END GAME (P2/Bot WINS)
   ↓
Loop until someone wins
```

#### Hành Động Chính:

| Hành Động | Điều Kiện | Kết Quả |
|-----------|-----------|---------|
| **Cell Click** | Đúng lượt + chưa tấn công | Kiểm tra trúng/trượt |
| **HIT** | Cell có tàu | +30 XP, +1 Hit count, bonus turn |
| **MISS** | Cell trống | +10 XP, switch turn |
| **Sunk Ship** | Tàu mất hết HP | Mark 8 ô xung quanh = Empty |
| **Win** | All opponent ships sunk | Show Game Over Panel, Save Progress |

#### Data Lưu:
- **CellState**: Unknown → Hit/Empty
- **Ship HP**: -1 per hit
- **Progress**: Wins, Losses, Total XP, Total Gold

---

### 3️⃣ Game Over & Progress Saving

```
Game Over Triggered
   ↓
Determine Winner
   ├─P1 Wins  → XP: +30, Gold: +10, Wins: +1
   └─P1 Loses → XP: +10, Gold: +0, Losses: +1
   ↓
Save to ProgressManager
   ↓
Upload to UGS (Cloud)
   ↓
Show Game Over Panel
   ↓
Return to Main Menu
```

---

## 💾 Dữ Liệu Được Lưu

### 1. Player Setup Data (Tạm thời)
**Nơi lưu:** `GameManager.placements[player]`  
**Khi lưu:** Khi người chơi drag ship vào grid  
**Nội dung:**
- Player ID (1 hoặc 2)
- List các Ships với:
  - `shipID` (định danh tàu)
  - `position` (Vector2Int - tọa độ góc tàu)
  - `isHorizontal` (bool - hướng tàu)

```csharp
public struct ShipPlacementData
{
    public string shipID;
    public Vector2Int position;
    public bool isHorizontal;
}
```

### 2. Battle State Data (Tạm thời)
**Nơi lưu:** `BattleSceneManager`  
**Nội dung:**
- `currentTurn` (Turn.Player1/Player2)
- `currentState` (GameState.Playing/GameOver)
- Cell states (CellState.Unknown/Hit/Empty)
- Ship HP remaining

### 3. Player Progress Data (Vĩnh Viễn)
**Nơi lưu:** `ProgressManager` + UGS Cloud  
**Khi lưu:** Sau khi game over (tự động upload lên UGS)  
**Nội dung:**

```csharp
public class PlayerProgressData
{
    public int playerLevel;
    public int totalExperience;
    public int currentLevelExperience;
    public int totalGold;
    public int totalWins;
    public int totalLosses;
}
```

**Cập nhật theo Win/Loss:**
| Kết Quả | XP | Gold | Wins | Losses |
|---------|-----|------|------|--------|
| **Win** | +30 | +10 | +1 | - |
| **Loss** | +10 | +0 | - | +1 |

### 4. Game Mode Data
**Nơi lưu:** `GameManager.gameMode`  
**Giá trị:**
- `GameMode.PlayWithBot` - Chơi với Bot
- `GameMode.PlayWithFriend` - Chơi với bạn (cùng device)

---

## 📦 Cấu Trúc Dữ Liệu

### GameManager (Singleton)
```
GameManager
├─ gameMode: GameMode
├─ currentSetupPlayer: int (1 hoặc 2)
├─ placements: Dictionary<int, List<ShipPlacementData>>
│  ├─ Player1: [Ship1, Ship2, ...]
│  └─ Player2: [Ship1, Ship2, ...]
├─ shipListData: ShipListData
│  └─ ships: List<ShipData>
└─ Methods:
   ├─ SavePlacement(player, ship)
   ├─ GetPlacements(player)
   ├─ ClearPlacements(player)
   └─ SetCurrentSetupPlayer(player)
```

### ProgressManager (Singleton)
```
ProgressManager
├─ Data: PlayerProgressData
├─ IsReady: bool
├─ Methods:
│  ├─ AddExperience(amount)
│  ├─ AddGold(amount)
│  ├─ SaveProgress()
│  └─ LoadProgress()
└─ Integration:
   └─ UGS Cloud Save
```

### GridManager (Per Grid)
```
GridManager
├─ isPlayer1Grid: bool
├─ gridWidth/Height: int (10x10)
├─ cells: Cell[,]
├─ ships: List<Ship>
├─ Methods:
│  ├─ CanPlaceShip(ship, origin) → bool
│  ├─ PlaceShip(ship, origin) → bool
│  ├─ AttackCell(position) → bool (hit/miss)
│  ├─ AllShipsSunk() → bool
│  ├─ ResetGrid() // Destroy ships
│  ├─ ResetGridOnly() // Giữ ships, reset trạng thái
│  └─ MarkAdjacentCellsEmpty(ship)
└─ Ship Storage:
   └─ ships: List<Ship>
```

### Cell
```
Cell
├─ gridPosition: Vector2Int (x, y)
├─ cellState: CellState (Unknown/Hit/Empty)
├─ occupyingShip: Ship
├─ Visual: SpriteRenderer
└─ Methods:
   ├─ Attack() → bool (trúng/trượt)
   ├─ SetOccupyingShip(ship)
   ├─ IsAvailable() → bool
   └─ UpdateVisual()
```

### Ship
```
Ship
├─ shipData: ShipData
├─ hp: int (tàu dài bao nhiêu ô)
├─ isHorizontal: bool
├─ occupiedCells: List<Cell>
├─ gridPosition: Vector2Int
└─ Methods:
   ├─ Rotate()
   ├─ TakeHit(cell)
   ├─ IsSunk() → bool
   ├─ GetOccupiedPositions() → List<Vector2Int>
   └─ Initialize(shipData)
```

---

## 🔧 Class Chính & Trách Nhiệm

| Class | Scene | Trách Nhiệm |
|-------|-------|------------|
| **GameManager** | Global | Quản lý game state, ship placement, game mode |
| **SetupSceneManager** | Setup | UI setup, ship placement flow, confirm logic |
| **BattleSceneManager** | Battle | Turn management, attack logic, win condition |
| **GridManager** | Setup/Battle | Grid creation, ship placement validation, attack handling |
| **ShipPlacement** | Setup | Drag/drop, snap to grid, rotation logic |
| **Cell** | Setup/Battle | Cell state, visual update, attack resolution |
| **Ship** | Setup/Battle | Ship data, HP tracking, sunk detection |
| **BotController** | Battle | Bot AI, auto-placement, auto-attack |
| **AudioManager** | Global | Sound effects (Hit, Miss, etc.) |
| **EffectPoolManager** | Battle | VFX pooling (Hit, Miss animations) |
| **ProgressManager** | Global | Player progress, XP/Gold tracking, UGS saving |

---

## 🔄 Biểu Đồ Tương Tác Dữ Liệu

```
┌─────────────────────┐
│   SetupScene P1     │
└──────────┬──────────┘
           │
      saves to
           │
           ▼
┌─────────────────────┐
│  GameManager        │◄──── Kiểm tra vị trí hợp lệ
│  placements[1]      │
└──────────┬──────────┘
           │
           │ (PlayWithFriend)
           ├─→ Pass Device Panel
           │   │
           │   ▼
           │  ┌─────────────────────┐
           │  │   SetupScene P2     │
           │  └──────────┬──────────┘
           │             │
           │        saves to
           │             │
           │             ▼
           │  ┌─────────────────────┐
           │  │  GameManager        │
           │  │  placements[2]      │
           │  └──────────┬──────────┘
           │             │
           └─────────────┼─────────┐
                         │         │
                    loads from    loads from
                         │         │
                         ▼         ▼
                  ┌────────────────────────┐
                  │   BattleSceneManager   │
                  │   P1Grid & P2Grid      │
                  └────────┬───────────────┘
                           │
                     writes to
                           │
                           ▼
                  ┌────────────────────────┐
                  │   ProgressManager      │
                  │   PlayerProgressData   │
                  └────────┬───────────────┘
                           │
                     saves to
                           │
                           ▼
                  ┌────────────────────────┐
                  │   UGS Cloud Storage    │
                  └────────────────────────┘
```

---

## 📊 Summary: Lifecycle Của Dữ Liệu

### Setup Phase
1. **PlayerClicks Start Setup** → SetupSceneManager initializes
2. **PlayerDragsShip** → Grid validates placement → GridManager.CanPlaceShip()
3. **ShipPlaced** → ShipPlacement.TryPlaceAtCurrentSnap() saved to GameManager
4. **PlayerConfirms** → Check nếu đủ ships → Proceed next

### Battle Phase
1. **BattleStarts** → Load placements từ GameManager
2. **PlayerAttacks** → Cell.Attack() → Update CellState
3. **ShipSunk** → Mark adjacent cells + Update UI
4. **AllShipsSunk** → Win condition met

### Save Phase
1. **GameOver** → Calculate rewards (XP, Gold)
2. **ProgressManager.AddExperience/Gold()** → Update PlayerProgressData
3. **ProgressManager.SaveProgress()** → Async upload to UGS
4. **Return to Menu** → Data persisted

---

## ⚠️ Important Notes

- **Ship Placements** được lưu tạm thời trong GameManager, KHÔNG persistent
- **Player Progress** được lưu vĩnh viễn trong UGS Cloud
- **Cell States** chỉ tồn tại trong current battle session
- **Pass & Play** giú che dấu opponent's ships giữa 2 Player
- **GridManager** có 2 mode: `ResetGrid()` (Destroy ships) vs `ResetGridOnly()` (giữ ships)
- **Bonus Turn** xảy ra khi hit, không switch turn

