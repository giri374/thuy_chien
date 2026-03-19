## 🏗️ ARCHITECTURE DIAGRAM - PHASE 1

### Pure Core Layer (No Dependencies)

```
┌─────────────────────────────────────────────────────────────┐
│                      CORE LAYER                              │
│              (No MonoBehaviour, No UI)                        │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                   Core.Models (Data)                     ││
│  ├─────────────────────────────────────────────────────────┤│
│  │                                                           ││
│  │  CellState (Enum)          CellData (Struct)             ││
│  │  ┌─────────────┐           ┌──────────────────┐          ││
│  │  │ Unknown     │           │ position         │          ││
│  │  │ Empty       │           │ state            │          ││
│  │  │ Hit         │           │ shipInstanceId   │          ││
│  │  │ Sunk        │           │ HasShip (prop)   │          ││
│  │  └─────────────┘           └──────────────────┘          ││
│  │                                                           ││
│  │  ShipInstanceData (Struct)                               ││
│  │  ┌──────────────────┐                                    ││
│  │  │ shipId           │                                    ││
│  │  │ position         │                                    ││
│  │  │ isHorizontal     │                                    ││
│  │  │ hitCount         │                                    ││
│  │  │ occupiedCells[]  │                                    ││
│  │  │ IsSunk (prop)    │                                    ││
│  │  └──────────────────┘                                    ││
│  │                                                           ││
│  └─────────────────────────────────────────────────────────┘│
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                   Core (Logic)                           ││
│  ├─────────────────────────────────────────────────────────┤│
│  │                                                           ││
│  │  ┌──────────────────────┐    ┌────────────────────────┐││
│  │  │       Board          │    │    GameSession         │││
│  │  ├──────────────────────┤    ├────────────────────────┤││
│  │  │ cells[10,10]         │    │ player1Board           │││
│  │  │ ships {}             │    │ player2Board           │││
│  │  │                      │    │ currentPlayer          │││
│  │  │ Placement:           │    │ isGameOver             │││
│  │  │ • CanPlaceShip()     │    │ winnerPlayer           │││
│  │  │ • PlaceShip()        │    │                        │││
│  │  │ • RemoveShip()       │    │ Setup Phase:           │││
│  │  │                      │    │ • IsSetupComplete()    │││
│  │  │ Attacks:             │    │                        │││
│  │  │ • Attack()           │    │ Battle Phase:          │││
│  │  │ • AttackMultiple()   │    │ • Attack()             │││
│  │  │ • MarkAdjacentEmpty()│    │ • EndTurn()            │││
│  │  │                      │    │ • CheckGameOver()      │││
│  │  │ Queries:             │    │                        │││
│  │  │ • GetCell()          │    │ Serialization:         │││
│  │  │ • GetShip()          │    │ • ToJson()             │││
│  │  │ • GetAllShips()      │    │ • FromJson()           │││
│  │  │ • GetCellsWithShips()│    │                        │││
│  │  │ • AllShipsSunk()     │    │                        │││
│  │  │ • GetShipCount()     │    │                        │││
│  │  │                      │    │                        │││
│  │  │ Serialization:       │    │ AttackResult           │││
│  │  │ • ToJson()           │    │ • position             │││
│  │  │ • FromJson()         │    │ • isHit                │││
│  │  │                      │    │ • isSunk               │││
│  │  │                      │    │ • isGameOver           │││
│  │  └──────────────────────┘    └────────────────────────┘││
│  │                                                           ││
│  └─────────────────────────────────────────────────────────┘│
│                                                               │
└─────────────────────────────────────────────────────────────┘
         ▲                                      ▲
         │                                      │
         │ Used by:                             │ Used by:
         │                                      │
    ┌────┴─────────────────────────┬───────────┴──────┐
    │                               │                   │
    │                               │                   │
┌───▼──────────────┐        ┌───────▼────────┐   ┌────▼────────┐
│  SetupScene      │        │  BattleScene   │   │  AI/Bot     │
│  (will use       │        │  (will use     │   │  (can use   │
│   Board)         │        │   Board x2)    │   │   Board)    │
└──────────────────┘        └────────────────┘   └─────────────┘
```

---

## 🔄 Data Flow Diagrams

### Setup Phase Flow
```
ShipPlacement.cs
       │
       ▼
  Board.CanPlaceShip()
       │
       ├─ YES ──▶ Board.PlaceShip()
       │              │
       │              ▼
       │         cells[x,y].shipInstanceId = shipId
       │         ships[shipId] = ShipInstanceData
       │              │
       │              ▼
       │         GridView.SyncFromBoard()
       │              │
       │              ▼
       │         Cell.UpdateVisual()
       │
       └─ NO ──▶ Show Red Preview
```

### Battle Phase Flow
```
Cell.OnPointerClick()
       │
       ▼
GridManager.OnCellClicked()
       │
       ▼
Board.Attack(position)
       │
       ├─ HIT ──▶ cellData.state = Hit
       │          ship.hitCount++
       │          │
       │          ▼
       │     ship.IsSunk?
       │     ├─ YES ──▶ Board.AllShipsSunk()?
       │     │          ├─ YES ──▶ GAME OVER
       │     │          └─ NO ──▶ Continue
       │     └─ NO ──▶ Continue
       │
       └─ MISS ──▶ cellData.state = Empty
                   │
                   ▼
              Switch Turn
```

### Serialization Flow
```
Board Instance              Network              Enemy Board
      │                         │                     │
      │ ToJson()                │                     │
      ├─────────────────────────────▶                 │
      │   (JSON string)        │                     │
      │                         │ FromJson()          │
      │                         ├────────────────────▶
      │                         │                     │
      │                         │                Board
      │                         │                Instance
```

---

## 📊 Class Dependency Graph

```
INDEPENDENT:
  • CellState (no dependencies)
  • CellData (depends on CellState)
  • ShipInstanceData (depends on Vector2Int)

CORE LOGIC:
  Board
    ├─ depends on: CellData, ShipInstanceData, Core.Models.CellState
    ├─ no external dependencies
    └─ 10x10 grid system

  GameSession
    ├─ depends on: Board, AttackResult
    ├─ no external dependencies
    └─ game state machine

CONSUMERS (not yet refactored):
  SetupSceneLogic
    └─ will use: Board, ShipData

  BattleSceneLogic
    └─ will use: Board x2, GameSession

  ShipPlacement
    └─ will use: Board

  GridManager
    └─ will use: Board, GridView (after Phase 2)
```

---

## 🌐 Integration Points (Phase 2+)

```
┌─────────────────────┐
│   Core Layer        │
│  (Pure Logic)       │
└──────────┬──────────┘
           │
           │ implements
           │
┌──────────▼──────────┐
│   GridView          │
│  (Maps Core→View)   │
└──────────┬──────────┘
           │
           │ updates
           │
┌──────────▼──────────┐
│   Cell UI Views     │
│  (MonoBehaviour)    │
└─────────────────────┘
```

---

## 🎯 Memory Layout

### Board Instance
```
[Board]
├─ cells[10,10] ───────────────▶ [CellData] x 100
│                               • position
│                               • state
│                               • shipInstanceId
│
└─ ships {} ───────────────────▶ [ShipInstanceData] x 5
                                • shipId
                                • position
                                • isHorizontal
                                • hitCount
                                • occupiedCells[] ──▶ [Vector2Int] x 3
```

### GameSession Instance
```
[GameSession]
├─ player1Board ───────────────▶ [Board] (see above)
├─ player2Board ───────────────▶ [Board] (see above)
├─ currentPlayer (int)
├─ isGameOver (bool)
└─ winnerPlayer (int)
```

---

## ✨ Why This Architecture Works

| Aspect | Benefit |
|--------|---------|
| **Separation of Concerns** | Core knows nothing about UI |
| **Pure Data Structs** | CellData, ShipInstanceData are simple + serializable |
| **Pure Logic Class** | Board has all game rules in one place |
| **Stateful but Deterministic** | Board state always consistent |
| **Easy to Test** | No MonoBehaviour, no asset dependencies |
| **Network Ready** | Can serialize to JSON and send |
| **Reusable** | Can use same Board in different UIs |
| **Extensible** | Can add new attack types, power-ups later |

---

**This is the foundation for a scalable, testable, multiplayer-ready game!** 🚀
