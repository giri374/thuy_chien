## 🎴 QUICK REFERENCE CARD - CORE API

**Print this or bookmark for quick access!**

---

### 📊 BOARD - Main Game Logic

```csharp
// Create
Board board = new Board();

// Placement Phase
bool canPlace = board.CanPlaceShip(id, pos, horizontal, shipData);
bool placed = board.PlaceShip(id, pos, horizontal, shipData);
board.RemoveShip(id);

// Battle Phase  
bool hit = board.Attack(pos);
List<Vector2Int> hits = board.AttackMultiple(positions);

// Queries
CellData cell = board.GetCell(pos);
ShipInstanceData? ship = board.GetShip(shipId);
List<ShipInstanceData> allShips = board.GetAllShips();
bool allSunk = board.AllShipsSunk();
int count = board.GetShipCount();
List<Vector2Int> shipCells = board.GetCellsWithShips();

// Optimization
board.MarkAdjacentEmpty(sunkShipPos);

// Serialization
string json = board.ToJson();
Board restored = Board.FromJson(json);
```

---

### 🎮 GAME SESSION - Turn Management

```csharp
// Create
GameSession session = new GameSession();

// Access
Board p1 = session.player1Board;
Board p2 = session.player2Board;
int current = session.currentPlayer;

// Setup
bool ready = session.IsSetupComplete(1);

// Battle
AttackResult result = session.Attack(pos);
session.EndTurn();
bool over = session.CheckGameOver();
int winner = session.winnerPlayer;

// Reset
session.Reset();
```

---

### 📦 DATA STRUCTURES

```csharp
// CellState - Enum
Core.Models.CellState.Unknown
Core.Models.CellState.Empty
Core.Models.CellState.Hit
Core.Models.CellState.Sunk

// CellData - Struct
var cell = new CellData(
    position: new Vector2Int(5, 5),
    state: Core.Models.CellState.Unknown,
    shipInstanceId: -1
);
bool hasShip = cell.HasShip;

// ShipInstanceData - Struct
var ship = new ShipInstanceData(
    id: 0,
    pos: new Vector2Int(0, 0),
    horizontal: true
);
ship.hitCount++;
bool sunk = ship.IsSunk;

// AttackResult - Struct
AttackResult result = new AttackResult
{
    position = new Vector2Int(5, 5),
    isHit = true,
    isSunk = false,
    isGameOver = false
};
```

---

### ✅ COMMON PATTERNS

```csharp
// Setup: Place 5 ships
for (int i = 0; i < 5; i++)
{
    var shipData = shipList.GetShipByID(i);
    if (board.CanPlaceShip(i, positions[i], horizontals[i], shipData))
    {
        board.PlaceShip(i, positions[i], horizontals[i], shipData);
    }
}

// Battle: Attack a cell
var cell = enemyBoard.GetCell(targetPos);
if (cell.state == Core.Models.CellState.Unknown)
{
    bool hit = enemyBoard.Attack(targetPos);
    if (hit && cell.HasShip)
    {
        var ship = enemyBoard.GetShip(cell.shipInstanceId);
        if (ship?.IsSunk ?? false)
        {
            enemyBoard.MarkAdjacentEmpty(targetPos);
        }
    }
}

// Win: Check game over
if (enemyBoard.AllShipsSunk())
{
    Debug.Log("Game Over! You Win!");
}
```

---

### 🔍 VALIDATION PATTERNS

```csharp
// Safe cell access
CellData cell = board.GetCell(pos);
if (cell.state == Core.Models.CellState.Unknown)
{
    // Can attack
}

// Safe ship lookup
ShipInstanceData? ship = board.GetShip(shipId);
if (ship.HasValue && ship.Value.IsSunk)
{
    // Ship is sunk
}

// Safe placement
var shipData = shipList.GetShipByID(shipId);
if (shipData != null && board.CanPlaceShip(shipId, pos, horizontal, shipData))
{
    board.PlaceShip(shipId, pos, horizontal, shipData);
}
```

---

### 🚀 METHOD CHEAT SHEET

| Task | Method | Returns |
|------|--------|---------|
| Check valid placement | `CanPlaceShip()` | `bool` |
| Place ship | `PlaceShip()` | `bool` |
| Remove ship | `RemoveShip()` | `bool` |
| Attack cell | `Attack()` | `bool` (hit?) |
| Attack multiple | `AttackMultiple()` | `List<Vector2Int>` (hits) |
| Get cell state | `GetCell()` | `CellData` |
| Get ship | `GetShip()` | `ShipInstanceData?` |
| Get all ships | `GetAllShips()` | `List<ShipInstanceData>` |
| Ships sunk? | `AllShipsSunk()` | `bool` |
| Ship count | `GetShipCount()` | `int` |
| Ship cells | `GetCellsWithShips()` | `List<Vector2Int>` |
| Mark adjacent | `MarkAdjacentEmpty()` | `void` |
| To JSON | `ToJson()` | `string` |
| From JSON | `FromJson()` | `Board` |

---

### 💡 GOTCHAS & TIPS

⚠️ **Always** check `CanPlaceShip()` before `PlaceShip()`  
⚠️ **Always** check `cell.state == Unknown` before attacking  
⚠️ **Use** `ship.HasValue` before accessing `ship.Value`  
✅ **Use** `ship?.IsSunk ?? false` for null-safe sinking check  
✅ **Call** `MarkAdjacentEmpty()` when ship sinks (AI hint)  
✅ **Serialize** board with `ToJson()` for network play  

---

### 📝 NAMESPACE REMINDER

- Core logic types: `using <no namespace needed>`
- Data structures: `using Core.Models;` (or prefix `Core.Models.CellState`)
- Game state: `GameSession` (no namespace)

---

### 🎯 INTEGRATION CHECKLIST

When using Board in your code:
- [ ] Import `using Core.Models;` if using CellState
- [ ] Create Board instance: `new Board()`
- [ ] Check CanPlaceShip before PlaceShip
- [ ] Check cell.state before Attack
- [ ] Handle null returns with `?.` operator
- [ ] Update UI after state changes
- [ ] Call ToJson before network transmission
- [ ] Call FromJson after network reception

---

**Questions? Check USAGE_EXAMPLES.md for 11 complete examples!**

**Need architectural help? Check ARCHITECTURE_DIAGRAMS.md**
