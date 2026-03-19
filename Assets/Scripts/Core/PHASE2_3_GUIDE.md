## 🎯 NEXT STEPS - PHASE 2 & 3 ROADMAP

### What's Done (Phase 1):
✅ Core.Models namespace with CellState, CellData, ShipInstanceData
✅ Board.cs - pure game logic
✅ GameSession.cs - game state management

---

### PHASE 2: Create View Layer Mapping (Assets/Scripts/UI/)

#### 2.1 Create **Assets/Scripts/UI/GridView.cs**
Purpose: Map Core.Board data → Visual representation

```csharp
public class GridView : MonoBehaviour
{
    private Cell[,] cellViews;        // Visual representations
    private Board logicBoard;          // Reference to core Board
    
    public void Initialize(Board board, Grid grid, Cell[,] cellPrefabs)
    public void RenderCell(Vector2Int position)
    public void SyncFromBoard()        // Update all cells from Board
    public void ShowAttackAnimation(Vector2Int pos, bool isHit)
}
```

Methods to implement:
- `Initialize()` - link core board with UI cells
- `SyncFromBoard()` - push Board state → Cell views
- `RenderCell()` - single cell rendering based on CellData
- `ShowAnimation()` - visual feedback

---

### PHASE 3: Refactor Existing Files

#### 3.1 Refactor **Cell.cs** (UI/View only)
- Remove game logic
- Keep only: Display + event delegation
- `OnPointerClick()` → callback to GridManager
- Update visuals from CellData

#### 3.2 Refactor **GridManager.cs**
- Add: `Board logicBoard` field
- Add: `GridView gridView` component reference
- Methods now delegate to Board:
  * `CanPlaceShip()` → logicBoard.CanPlaceShip()
  * `PlaceShip()` → logicBoard.PlaceShip() + gridView.SyncFromBoard()
  * `RemoveShip()` → logicBoard.RemoveShip() + gridView.SyncFromBoard()
- Keep: Grid/cell creation (visual)

#### 3.3 Refactor **ShipPlacement.cs**
- Change: `gridManager.CanPlaceShip(ship)` 
  → `shipBoard.CanPlaceShip(shipId, pos, horizontal)`
- Change: `gridManager.PlaceShip(ship)` 
  → `shipBoard.PlaceShip(shipId, pos, horizontal)`
- Add: reference to Board instance

#### 3.4 Refactor **SetupSceneLogic.cs**
- Add: `Board playerBoard` field
- Replace GridManager calls with Board calls
- Validate all ships placed via `playerBoard.GetShipCount()`

#### 3.5 Refactor **BattleSceneLogic.cs** (if exists)
- Add: `Board player1Board`, `Board player2Board`
- Add: `GameSession gameSession` (or create new)
- Change: Cell click → call Board.Attack()
- Update grid views after attack

---

### INTEGRATION PATTERN

**Old Flow (GridManager-centric):**
```
ShipPlacement 
  → GridManager.CanPlaceShip(ship)
    → GridManager.ships.Count check
    → Cell.occupyingShip check
```

**New Flow (Board-centric):**
```
ShipPlacement 
  → Board.CanPlaceShip(shipId, pos, horizontal, shipData)
    → Check cells[x,y].HasShip
    → Return result
  → Board.PlaceShip(...)
    → Update cells & ships dict
  → GridView.SyncFromBoard()
    → Update Cell views
```

---

### HOW TO USE IN SETUP SCENE

```csharp
// In SetupSceneLogic.cs
private Board playerBoard = new Board();

public void OnShipPlaced(int shipId, Vector2Int pos, bool horizontal, ShipData shipData)
{
    if (playerBoard.CanPlaceShip(shipId, pos, horizontal, shipData))
    {
        playerBoard.PlaceShip(shipId, pos, horizontal, shipData);
        gridView.SyncFromBoard();
        
        // Check if all ships placed
        if (playerBoard.GetShipCount() == 5)
        {
            AllShipsPlacedCallback();
        }
    }
}

public bool CanConfirm()
{
    return playerBoard.GetShipCount() == 5;
}
```

---

### HOW TO USE IN BATTLE SCENE

```csharp
// In BattleSceneLogic.cs
private Board player1Board = new Board();
private Board player2Board = new Board();
private int currentPlayer = 1;

public void OnCellClicked(Vector2Int pos)
{
    var enemyBoard = currentPlayer == 1 ? player2Board : player1Board;
    bool wasHit = enemyBoard.Attack(pos);
    
    // Update UI
    gridView.RenderCell(pos);
    gridView.ShowAnimation(pos, wasHit);
    
    // Switch turn
    currentPlayer = currentPlayer == 1 ? 2 : 1;
    
    // Check game over
    if (enemyBoard.AllShipsSunk())
    {
        GameOverCallback(currentPlayer);
    }
}
```

---

### NAMING CONVENTIONS

| Purpose | Type | Location | Naming |
|---------|------|----------|--------|
| Core Logic | Class | Core/ | Board, GameSession, etc. |
| Data Models | Struct | Core/Models/ | CellData, ShipInstanceData |
| Enums | Enum | Core/Models/ | CellState (in Core.Models namespace) |
| Views | MonoBehaviour | UI/ | GridView, Cell (keep existing) |
| Managers | MonoBehaviour | Scripts/ | GridManager, etc. |

---

### SERIALIZATION EXAMPLE (for future network)

```csharp
// Save board state
var boardJson = playerBoard.ToJson();
NetworkManager.SendBoard(boardJson);

// Load board state
Board receivedBoard = Board.FromJson(jsonString);
```

---

### TESTING OPPORTUNITIES

Phase 1 allows pure unit tests (no Unity needed):
```csharp
[TestMethod]
public void Board_CanPlaceShip_WithValidPosition_ReturnsTrue()
{
    var board = new Board();
    var shipData = new ShipData { size = new Vector2Int(2, 1) };
    
    bool result = board.CanPlaceShip(0, Vector2Int.zero, true, shipData);
    
    Assert.IsTrue(result);
}
```

---

**Ready for Phase 2? Create GridView.cs and refactor Cell.cs!**
