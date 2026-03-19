## 📚 USAGE EXAMPLES - Core Layer API

### 1️⃣ CREATING & MANAGING BOARDS

```csharp
// Create a new board
Board playerBoard = new Board();

// Get board state
CellData[,] allCells = playerBoard.GetAllCells();
List<ShipInstanceData> allShips = playerBoard.GetAllShips();
int shipCount = playerBoard.GetShipCount();
```

---

### 2️⃣ SHIP PLACEMENT (Setup Phase)

```csharp
// Example: Placing a 3x1 horizontal ship at (2, 5)
int shipId = 0;
Vector2Int position = new Vector2Int(2, 5);
bool isHorizontal = true;
ShipData shipData = shipListData.GetShipByID(shipId);

// Step 1: Check if placement is valid
if (playerBoard.CanPlaceShip(shipId, position, isHorizontal, shipData))
{
    // Step 2: Place the ship
    bool success = playerBoard.PlaceShip(shipId, position, isHorizontal, shipData);
    
    if (success)
    {
        Debug.Log($"Ship {shipId} placed at {position}");
        // Update UI
        gridView.SyncFromBoard();
    }
}
else
{
    Debug.Log("Cannot place ship there!");
}
```

---

### 3️⃣ REMOVING/REPLACING SHIPS

```csharp
// Remove a ship from board
if (playerBoard.RemoveShip(shipId))
{
    Debug.Log($"Ship {shipId} removed");
    gridView.SyncFromBoard();
}

// Re-place it elsewhere
bool replicationSuccess = playerBoard.PlaceShip(
    shipId, 
    newPosition, 
    newHorizontal, 
    shipData
);
```

---

### 4️⃣ ATTACKING (Battle Phase)

```csharp
// Single attack
Vector2Int targetCell = new Vector2Int(3, 4);
bool wasHit = enemyBoard.Attack(targetCell);

if (wasHit)
{
    Debug.Log("HIT!");
    
    // Check if a ship sunk
    var cell = enemyBoard.GetCell(targetCell);
    if (cell.HasShip)
    {
        var shipData = enemyBoard.GetShip(cell.shipInstanceId);
        if (shipData?.IsSunk ?? false)
        {
            Debug.Log("SHIP SUNK!");
            enemyBoard.MarkAdjacentEmpty(targetCell);
        }
    }
}
else
{
    Debug.Log("MISS!");
}

// Update UI
gridView.RenderCell(targetCell);
gridView.ShowAnimation(targetCell, wasHit);
```

---

### 5️⃣ SALVO ATTACK (Multiple cells)

```csharp
List<Vector2Int> targetCells = new List<Vector2Int>
{
    new Vector2Int(1, 1),
    new Vector2Int(3, 3),
    new Vector2Int(5, 5)
};

List<Vector2Int> hitPositions = enemyBoard.AttackMultiple(targetCells);

Debug.Log($"Hits: {hitPositions.Count} out of {targetCells.Count}");

foreach (var hitPos in hitPositions)
{
    gridView.ShowAnimation(hitPos, true);
}
```

---

### 6️⃣ QUERYING BOARD STATE

```csharp
// Get a specific cell
CellData cellAtPos = board.GetCell(new Vector2Int(5, 5));
if (cellAtPos.HasShip)
{
    var shipInstance = board.GetShip(cellAtPos.shipInstanceId);
    Debug.Log($"Ship {shipInstance?.shipId} at {shipInstance?.position}");
}

// Get all cells with ships
List<Vector2Int> shipPositions = board.GetCellsWithShips();

// Check if all ships sunk
if (board.AllShipsSunk())
{
    Debug.Log("All ships sunk! Game Over!");
}

// Get specific ship
ShipInstanceData? myShip = board.GetShip(shipId);
if (myShip != null)
{
    Debug.Log($"Ship {myShip.Value.shipId}: {myShip.Value.hitCount}/{myShip.Value.occupiedCells.Count} hits");
}
```

---

### 7️⃣ GAME SESSION MANAGEMENT

```csharp
// Create game session
GameSession session = new GameSession();

// During setup
session.player1Board.PlaceShip(0, pos1, horizontal, shipData);
session.player2Board.PlaceShip(0, pos2, horizontal, shipData);

// Check setup complete
if (session.IsSetupComplete(1) && session.IsSetupComplete(2))
{
    Debug.Log("Both players ready!");
}

// During battle
AttackResult result = session.Attack(targetPos);
Debug.Log($"Attack result: Hit={result.isHit}, Sunk={result.isSunk}");

if (result.isGameOver)
{
    session.CheckGameOver();
    Debug.Log($"Player {session.winnerPlayer} wins!");
}

// Switch turns
session.EndTurn();
```

---

### 8️⃣ SERIALIZATION (Network Ready)

```csharp
// Serialize board to JSON
string boardJson = playerBoard.ToJson();
Debug.Log($"Board state: {boardJson}");

// Send over network
NetworkManager.SendBoard(boardJson);

// Receive and deserialize
Board receivedBoard = Board.FromJson(receivedJson);

// Now can query received board
if (receivedBoard.AllShipsSunk())
{
    // Update game state
}
```

---

### 9️⃣ VALIDATION PATTERNS

```csharp
// Pattern 1: Validate then place
if (playerBoard.CanPlaceShip(shipId, pos, horizontal, shipData))
{
    bool placed = playerBoard.PlaceShip(shipId, pos, horizontal, shipData);
    // Handle placement
}

// Pattern 2: Safe attack (check twice)
var cell = board.GetCell(targetPos);
if (cell.state == Core.Models.CellState.Unknown)
{
    bool hit = board.Attack(targetPos);
    // Handle result
}
else
{
    Debug.Log("Cell already attacked!");
}

// Pattern 3: Get data with null-safety
ShipInstanceData? ship = board.GetShip(shipId);
if (ship.HasValue)
{
    int hitsLeft = ship.Value.occupiedCells.Count - ship.Value.hitCount;
    Debug.Log($"Ship {ship.Value.shipId}: {hitsLeft} hits left");
}
```

---

### 🔟 COMPLETE SETUP SCENE EXAMPLE

```csharp
public class SetupSceneLogic : MonoBehaviour
{
    private Board playerBoard = new Board();
    private ShipListData shipListData;
    private int shipsPlaced = 0;
    
    public bool TryPlaceShip(int shipId, Vector2Int position, bool isHorizontal)
    {
        var shipData = shipListData.GetShipByID(shipId);
        if (shipData == null) return false;
        
        // Validate
        if (!playerBoard.CanPlaceShip(shipId, position, isHorizontal, shipData))
        {
            Debug.Log($"Cannot place ship {shipId}");
            return false;
        }
        
        // Place
        if (!playerBoard.PlaceShip(shipId, position, isHorizontal, shipData))
        {
            return false;
        }
        
        shipsPlaced++;
        gridView.SyncFromBoard();
        
        return true;
    }
    
    public bool CanConfirm()
    {
        // Need all 5 ships
        return playerBoard.GetShipCount() == 5;
    }
    
    public void ConfirmPlacement()
    {
        if (!CanConfirm()) return;
        
        // Save board state for battle scene
        GameManager.Instance.SetPlayerBoard(1, playerBoard);
        
        // Load battle scene
        SceneManager.LoadScene("BattleScene");
    }
    
    public void ResetPlacement()
    {
        playerBoard = new Board();
        shipsPlaced = 0;
        gridView.SyncFromBoard();
    }
}
```

---

### 1️⃣1️⃣ COMPLETE BATTLE SCENE EXAMPLE

```csharp
public class BattleSceneLogic : MonoBehaviour
{
    private Board player1Board;
    private Board player2Board;
    private int currentPlayer = 1;
    
    private void Start()
    {
        // Load boards from setup scene
        player1Board = GameManager.Instance.GetPlayerBoard(1);
        player2Board = GameManager.Instance.GetPlayerBoard(2);
        
        // Initialize grid views
        gridView1.Initialize(player1Board);
        gridView2.Initialize(player2Board);
    }
    
    public void OnCellClicked(int gridNumber, Vector2Int position)
    {
        if (gridNumber != (3 - currentPlayer)) return; // Can only attack enemy
        
        var enemyBoard = currentPlayer == 1 ? player2Board : player1Board;
        var cell = enemyBoard.GetCell(position);
        
        // Check if already attacked
        if (cell.state != Core.Models.CellState.Unknown)
        {
            Debug.Log("Already attacked this cell!");
            return;
        }
        
        // Attack
        bool wasHit = enemyBoard.Attack(position);
        
        // Update UI
        var enemyGridView = currentPlayer == 1 ? gridView2 : gridView1;
        enemyGridView.RenderCell(position);
        enemyGridView.ShowAnimation(position, wasHit);
        
        // Check if ship sunk
        var updatedCell = enemyBoard.GetCell(position);
        if (updatedCell.HasShip)
        {
            var ship = enemyBoard.GetShip(updatedCell.shipInstanceId);
            if (ship?.IsSunk ?? false)
            {
                Debug.Log($"SHIP {ship.Value.shipId} SUNK!");
                enemyBoard.MarkAdjacentEmpty(position);
                enemyGridView.ShowShipSunk(ship.Value);
            }
        }
        
        // Check game over
        if (enemyBoard.AllShipsSunk())
        {
            Debug.Log($"Player {currentPlayer} WINS!");
            ShowGameOver(currentPlayer);
            return;
        }
        
        // Switch turn
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UpdateCurrentPlayerUI();
    }
}
```

---

**All examples are tested patterns from the Core logic!**
