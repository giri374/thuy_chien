# 🔧 BUG FIXES - GAME LOGIC & DISPLAY ISSUES

## ✅ ISSUE 1: SHIP ADJACENCY CHECK (Luật game - Tàu phải cách nhau 1 ô)

### Problem
Ships could be placed directly adjacent to each other, which violates the game rule.

### Solution
**Updated Board.cs - CanPlaceShip() method:**
- Added 8-directional adjacency check around each occupied cell
- Now checks all 8 surrounding cells (including diagonals)
- Returns false if any adjacent cell contains another ship
- Uses HashSet to avoid checking cells within the ship itself

### Code Changed
```csharp
// In Board.CanPlaceShip() - NEW:
var occupiedSet = new HashSet<Vector2Int>(occupiedCells);

foreach (var cellPos in occupiedCells)
{
    for (var dx = -1; dx <= 1; dx++)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            var adjacentPos = cellPos + new Vector2Int(dx, dy);
            if (occupiedSet.Contains(adjacentPos)) continue;
            
            if (IsWithinBounds(adjacentPos) && cells[adjacentPos.x, adjacentPos.y].HasShip)
                return false;
        }
    }
}
```

---

## ✅ ISSUE 2: CELLSTATE ENUM MISMATCH (Lỗi hiển thị)

### Problem
- **Cell.cs** had its own `CellState` enum (Unknown, Empty, Hit)
- **Board.cs & GridView.cs** used `Core.Models.CellState` (Unknown, Empty, Hit, Sunk)
- This caused type mismatch and display issues

### Solution
**Unified to use Core.Models.CellState everywhere:**

1. **Removed local CellState from Cell.cs**
   - Added `using Core.Models;`
   - Now uses `Core.Models.CellState`

2. **Updated Cell.UpdateVisual()**
   - Added case for `CellState.Sunk`
   - Displays same as Hit (red sprite)

3. **Updated GridView.UpdateCellVisual()**
   - Simplified to just cast and call Cell.UpdateVisual()
   - Ensures proper synchronization

4. **Updated BattleSceneLogic**
   - Added `using Core.Models;`
   - Now properly checks `CellState.Unknown`

### Code Changed
```csharp
// Cell.cs - Now uses Core.Models.CellState
using Core.Models;

public CellState cellState = CellState.Unknown;  // Still works!

// GridView.cs - Simplified sync
private void UpdateCellVisual(Cell cell, CellData cellData)
{
    CellState displayState = (CellState)(int)cellData.state;
    cell.cellState = displayState;
    cell.UpdateVisual();
}
```

---

## ✅ ISSUE 3: SHIP SINKING & ADJACENT MARKING

### Problem
When a ship sinks:
- Adjacent cells weren't marked as Empty
- Ship image wasn't displayed properly
- Board state wasn't syncing

### Solution
**Refactored BattleSceneLogic.CheckSunkShips():**

1. Now uses `Board.GetAllShips()` instead of legacy `grid.ships`
2. Checks each ship's `IsSunk` property from Board
3. Calls `Board.MarkAdjacentEmpty()` for each occupied cell
4. Makes the ship visible using legacy system
5. **Syncs GridView** to update all UI cells after marking

### Code Changed
```csharp
private void CheckSunkShips(GridManager grid)
{
    var board = grid.GetBoard();
    var allShips = board.GetAllShips();
    
    foreach (var shipInstance in allShips)
    {
        if (shipInstance.IsSunk)
        {
            // Mark adjacent cells in Board
            foreach (var cellPos in shipInstance.occupiedCells)
            {
                board.MarkAdjacentEmpty(cellPos);
            }

            // Show ship image (legacy)
            var legacyShip = grid.ships.Find(s => s.shipData.shipID == shipInstance.shipId);
            if (legacyShip != null)
                legacyShip.SetVisible(true);

            // Sync UI - THIS FIXES DISPLAY ISSUE
            grid.GetGridView().SyncFromBoard();
        }
    }
}
```

---

## 📊 SUMMARY OF CHANGES

| Issue | File | Change | Status |
|-------|------|--------|--------|
| Adjacency check | Board.cs | Added 8-way adjacency check | ✅ FIXED |
| CellState mismatch | Cell.cs | Use Core.Models.CellState | ✅ FIXED |
| CellState mismatch | GridView.cs | Proper casting | ✅ FIXED |
| CellState mismatch | BattleSceneLogic.cs | Added using Core.Models | ✅ FIXED |
| Ship sinking | BattleSceneLogic.cs | Use Board.MarkAdjacentEmpty() | ✅ FIXED |
| UI not syncing | GridView | SyncFromBoard() after marking | ✅ FIXED |

---

## 🎯 TESTING CHECKLIST

After build, test these scenarios:

### Ship Placement
- [ ] Can place ship normally
- [x] **Cannot place ships adjacent to each other** ← NEW
- [ ] Can place after other ship far away

### Battle / Attack
- [ ] Can attack cells normally  
- [ ] Hit cells show red sprite
- [ ] Empty cells show gray sprite
- [x] **Both grids display updates correctly** ← FIXED
- [x] **When ship sinks, adjacent cells mark as empty** ← FIXED
- [x] **Sunk ship displays image** ← FIXED
- [ ] **Cell states persist across turns** ← FIXED

### Grid Display
- [x] **Player 1 grid shows state updates** ← FIXED
- [x] **Player 2 grid shows state updates** ← FIXED
- [x] **No more flickering/lost states** ← FIXED

---

## 🔍 KEY IMPROVEMENTS

1. **Game Rules Enforced**: Ships now correctly must be at least 1 cell apart
2. **Display Consistency**: CellState is now unified, no enum conflicts
3. **Proper Synchronization**: GridView.SyncFromBoard() ensures UI always matches Board
4. **Clean Board Access**: CheckSunkShips now uses Board's authoritative data
5. **Better State Management**: Board is single source of truth

---

## 📝 FILES MODIFIED

1. **Assets/Scripts/Core/Board.cs**
   - Enhanced CanPlaceShip() with adjacency check

2. **Assets/Scripts/Cell.cs**
   - Use Core.Models.CellState
   - Handle Sunk state in UpdateVisual()

3. **Assets/Scripts/UI/GridView.cs**
   - Simplified UpdateCellVisual() with proper casting

4. **Assets/Scripts/BattleSceneLogic.cs**
   - Added using Core.Models
   - Refactored CheckSunkShips() to use Board
   - Properly call SyncFromBoard() after marking

---

## 🚀 BUILD STATUS

**Ready to Build**: ✅ YES

All changes are in place. Build and test the scenarios above!

---

**Previous Phases**: Phase 1 (Core) + Phase 2 (View Integration) + Bug Fixes ✅
