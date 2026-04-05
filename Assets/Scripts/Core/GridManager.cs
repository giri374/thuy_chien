using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject cellPrefab;

    [Header("Grid Type")]
    public bool isPlayer1Grid = true;

    private Cell[,] cells;
    private Board logicBoard;
    private GridView gridView;
    private SetupSceneUIManager setupSceneManager;

    public List<Ship> ships { get; private set; } = new List<Ship>();
    public Grid grid { get; private set; }

    private void Awake()
    {
        grid = GetComponent<Grid>();
        CreateGrid();

        logicBoard = new Board();
        gridView = gameObject.AddComponent<GridView>();
        gridView.grid = grid;
        gridView.gridWidth = gridWidth;
        gridView.gridHeight = gridHeight;
        gridView.Initialize(logicBoard, cells);
    }

    public void Initialize(SetupSceneUIManager manager)
    {
        setupSceneManager = manager;
    }

    private void CreateGrid()
    {
        cells = new Cell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CreateCell(x, y);
            }
        }
    }

    private void CreateCell(int x, int y)
    {
        var cellPosition = new Vector3Int(x, y, 0);
        var worldPosition = grid.CellToWorld(cellPosition);

        var cellObject = Instantiate(cellPrefab, worldPosition, Quaternion.identity, transform);
        cellObject.name = $"Cell_{x}_{y}";

        var cell = cellObject.GetComponent<Cell>();
        if (cell != null)
        {
            cell.Initialize(new Vector2Int(x, y), this);
            cells[x, y] = cell;
        }
    }

    public Cell GetCell(int x, int y)
    {
        return x < 0 || x >= gridWidth || y < 0 || y >= gridHeight ? null : cells[x, y];
    }

    public Cell GetCell(Vector2Int position) => GetCell(position.x, position.y);

    public void OnCellClicked(Cell cell)
    {
        if (BattleSceneLogic.Instance != null)
        {
            if (isPlayer1Grid)
            {
                BattleSceneLogic.Instance.OnPlayer1GridCellClicked(cell);
            }
            else
            {
                BattleSceneLogic.Instance.OnPlayer2GridCellClicked(cell);
            }

            return;
        }
    }

    public bool CanPlaceShip(Ship ship, Vector2Int origin)
    {
        if (ship?.shipData == null)
        {
            return false;
        }

        return logicBoard.CanPlaceShip(ship.shipData.shipID, origin, ship.isHorizontal, ship.shipData);
    }

    public bool PlaceShip(Ship ship, Vector2Int origin)
    {
        if (!CanPlaceShip(ship, origin))
        {
            return false;
        }

        var success = logicBoard.PlaceShip(ship.shipData.shipID, origin, ship.isHorizontal, ship.shipData);

        if (!success)
        {
            return false;
        }

        ship.SetGridPosition(origin);
        var occupiedCells = new List<Cell>();

        var shipInstance = logicBoard.GetShip(ship.shipData.shipID);
        if (shipInstance.HasValue)
        {
            foreach (var cellPos in shipInstance.Value.occupiedCells)
            {
                var cell = GetCell(cellPos);
                if (cell != null)
                {
                    occupiedCells.Add(cell);
                }
            }
        }

        ship.SetOccupiedCells(occupiedCells);
        ships.Add(ship);

        gridView.SyncFromBoard();

        var worldPosition = grid.CellToWorld(new Vector3Int(origin.x, origin.y, 0));
        worldPosition.z = 0f;

        if (!ship.isHorizontal)
        {
            worldPosition.x += grid.cellSize.x;
        }

        ship.transform.position = worldPosition;

        return true;
    }

    public void RemoveShip(Ship ship)
    {
        if (ship?.shipData == null)
        {
            return;
        }

        logicBoard.RemoveShip(ship.shipData.shipID);
        ships.Remove(ship);
        gridView.SyncFromBoard();
    }

    public bool AttackCell(Vector2Int position)
    {
        var cell = GetCell(position);
        if (cell == null)
        {
            return false;
        }

        var wasHit = logicBoard.Attack(position);

        var cellData = logicBoard.GetCell(position);
        gridView.RenderCell(cellData);
        gridView.ShowAttackAnimation(position, wasHit);

        return wasHit;
    }

    public void MarkAdjacentCellsEmpty(Ship sunkShip)
    {
        foreach (var occupiedCell in sunkShip.occupiedCells)
        {
            logicBoard.MarkAdjacentEmpty(occupiedCell.gridPosition);
        }

        gridView.SyncFromBoard();
    }

    public Board GetBoard()
    {
        return logicBoard;
    }

    public GridView GetGridView()
    {
        return gridView;
    }

    public bool AllShipsSunk()
    {
        return logicBoard.AllShipsSunk();
    }

    public void ResetGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GetCell(x, y)?.ResetCell();
            }
        }

        foreach (var ship in ships)
        {
            if (ship != null)
            {
                Destroy(ship.gameObject);
            }
        }

        ships.Clear();
    }

    public void ResetGridOnly()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GetCell(x, y)?.ResetCell();
            }
        }

        ships.Clear();
    }
}