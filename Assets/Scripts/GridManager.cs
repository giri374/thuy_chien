using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý lưới ô 10x10.
/// Được dùng cho cả lưới Player 1 và Player 2.
/// isPlayer1Grid = true → đây là lưới Player 1.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    [Header("Grid Component")]
    public Grid grid;

    [Header("Prefabs")]
    public GameObject cellPrefab;

    [Header("Grid Type")]
    public bool isPlayer1Grid = true; // True: lưới Player 1 | False: lưới Player 2 / Bot

    [Header("Cell Storage")]
    private Cell[,] cells;

    [Header("Ship Storage")]
    public List<Ship> ships = new List<Ship>();

    private SetupSceneManager setupSceneManager;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        if (grid == null)
            grid = GetComponent<Grid>();

        CreateGrid();
    }

    /// <summary>
    /// Gán SetupSceneManager sau khi grid đã tạo.
    /// </summary>
    public void Initialize(SetupSceneManager manager)
    {
        setupSceneManager = manager;
    }

    // ── Grid Creation ─────────────────────────────────────────

    private void CreateGrid()
    {
        cells = new Cell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                CreateCell(x, y);
    }

    private void CreateCell(int x, int y)
    {
        Vector3Int cellPosition = new Vector3Int(x, y, 0);
        Vector3 worldPosition = grid.CellToWorld(cellPosition);

        GameObject cellObject = Instantiate(cellPrefab, worldPosition, Quaternion.identity, transform);
        cellObject.name = $"Cell_{x}_{y}";

        Cell cell = cellObject.GetComponent<Cell>();
        if (cell != null)
        {
            cell.Initialize(new Vector2Int(x, y), this);
            cells[x, y] = cell;
        }
    }

    // ── Cell Access ───────────────────────────────────────────

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
        return cells[x, y];
    }

    public Cell GetCell(Vector2Int position) => GetCell(position.x, position.y);

    // ── Click Routing ─────────────────────────────────────────

    public void OnCellClicked(Cell cell)
    {
        Debug.Log($"[GridManager] OnCellClicked — {cell.gridPosition} | isPlayer1Grid={isPlayer1Grid}");

        // BattleScene
        if (BattleSceneManager.Instance != null)
        {
            if (isPlayer1Grid)
                BattleSceneManager.Instance.OnPlayer1GridCellClicked(cell);
            else
                BattleSceneManager.Instance.OnPlayer2GridCellClicked(cell);
            return;
        }

        // SetupScene
        if (setupSceneManager != null)
        {
            setupSceneManager.OnPlayerGridCellClicked(cell);
        }
        else
        {
            Debug.LogWarning("[GridManager] Cả BattleSceneManager lẫn SetupSceneManager đều null!");
        }
    }

    // ── Ship Placement ────────────────────────────────────────

    public bool CanPlaceShip(Ship ship, Vector2Int origin)
    {
        List<Vector2Int> offsets = ship.GetOccupiedPositions();

        var absolutePositions = new List<Vector2Int>();
        foreach (var offset in offsets)
            absolutePositions.Add(origin + offset);

        foreach (var pos in absolutePositions)
        {
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
                return false;

            Cell cell = GetCell(pos);
            if (cell == null || !cell.IsAvailable()) return false;
        }

        var shipCells = new HashSet<Vector2Int>(absolutePositions);

        foreach (var pos in absolutePositions)
        {
            foreach (var dir in _adjacentDirs)
            {
                Vector2Int neighbor = pos + dir;
                if (shipCells.Contains(neighbor)) continue;

                Cell cell = GetCell(neighbor);
                if (cell != null && !cell.IsAvailable()) return false;
            }
        }

        return true;
    }

    private static readonly Vector2Int[] _adjacentDirs =
    {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1,  0),                         new Vector2Int(1,  0),
        new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
    };

    public bool PlaceShip(Ship ship, Vector2Int origin)
    {
        if (!CanPlaceShip(ship, origin)) return false;

        ship.SetGridPosition(origin);

        var occupiedCells = new List<Cell>();
        foreach (var offset in ship.GetOccupiedPositions())
        {
            Cell cell = GetCell(origin + offset);
            if (cell != null) occupiedCells.Add(cell);
        }

        ship.SetOccupiedCells(occupiedCells);
        ships.Add(ship);

        Vector3 worldPosition = grid.CellToWorld(new Vector3Int(origin.x, origin.y, 0));
        worldPosition.z = 0f;

        if (ship != null && !ship.isHorizontal)
            worldPosition.x += grid.cellSize.x;

        ship.transform.position = worldPosition;

        return true;
    }

    // ── Attack ────────────────────────────────────────────────

    public bool AttackCell(Vector2Int position)
    {
        Cell cell = GetCell(position);
        if (cell == null) return false;
        return cell.Attack();
    }

    // ── Sunk Ship Marking ─────────────────────────────────────

    public void MarkAdjacentCellsEmpty(Ship sunkShip)
    {
        foreach (Cell occupiedCell in sunkShip.occupiedCells)
            MarkSurroundingCellsEmpty(occupiedCell.gridPosition);
    }

    private void MarkSurroundingCellsEmpty(Vector2Int position)
    {
        foreach (var dir in _adjacentDirs)
        {
            Vector2Int checkPos = position + dir;
            Cell cell = GetCell(checkPos);

            if (cell != null && cell.cellState == CellState.Unknown)
            {
                cell.cellState = CellState.Empty;
                cell.UpdateVisual();
            }
        }
    }

    // ── Win Condition ─────────────────────────────────────────

    public bool AllShipsSunk()
    {
        foreach (Ship ship in ships)
            if (ship != null && !ship.IsSunk()) return false;

        return ships.Count > 0;
    }

    // ── Reset ─────────────────────────────────────────────────

    /// <summary>
    /// Reset toàn bộ lưới VÀ Destroy tất cả ship.
    /// Dùng khi chuyển scene hoặc bot reset.
    /// </summary>
    public void ResetGrid()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                GetCell(x, y)?.ResetCell();

        foreach (Ship ship in ships)
            if (ship != null) Destroy(ship.gameObject);

        ships.Clear();
    }

    /// <summary>
    /// Chỉ reset trạng thái các ô (Unknown) và xóa danh sách ship,
    /// KHÔNG Destroy ship GameObject — dùng cho SetupScene khi người chơi nhấn Reset.
    /// Ship sẽ được trả về vị trí gốc bởi ShipPlacement.ResetToOrigin().
    /// </summary>
    public void ResetGridOnly()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                GetCell(x, y)?.ResetCell();

        ships.Clear();
    }
}