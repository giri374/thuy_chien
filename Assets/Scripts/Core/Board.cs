using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Models;

public class Board
{
    private const int GridWidth = 10;
    private const int GridHeight = 10;

    private CellData[,] cells = new CellData[GridWidth, GridHeight];
    private readonly Dictionary<int, ShipInstanceData> ships = new Dictionary<int, ShipInstanceData>();

    public Board()
    {
        InitializeEmptyCells();
    }

    private void InitializeEmptyCells()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                cells[x, y] = new CellData(new Vector2Int(x, y), CellState.Unknown, -1);
            }
        }
    }

    public bool CanPlaceShip(int shipId, Vector2Int position, bool isHorizontal, ShipData shipData)
    {
        if (shipData == null)
        {
            Debug.LogError($"Board.CanPlaceShip: ShipData is null for shipId {shipId}");
            return false;
        }

        var shipSize = isHorizontal ? shipData.size : new Vector2Int(shipData.size.y, shipData.size.x);
        var occupiedCells = GetOccupiedCells(position, shipSize);

        if (!IsWithinBounds(position, shipSize))
        {
            return false;
        }

        if (occupiedCells.Any(cellPos => cells[cellPos.x, cellPos.y].HasShip))
        {
            return false;
        }

        var occupiedSet = new HashSet<Vector2Int>(occupiedCells);

        foreach (var cellPos in occupiedCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var adjacentPos = cellPos + new Vector2Int(dx, dy);

                    if (occupiedSet.Contains(adjacentPos))
                    {
                        continue;
                    }

                    if (IsWithinBounds(adjacentPos) && cells[adjacentPos.x, adjacentPos.y].HasShip)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool PlaceShip(int shipId, Vector2Int position, bool isHorizontal, ShipData shipData)
    {
        if (!CanPlaceShip(shipId, position, isHorizontal, shipData))
        {
            return false;
        }

        var shipSize = isHorizontal ? shipData.size : new Vector2Int(shipData.size.y, shipData.size.x);
        var occupiedCells = GetOccupiedCells(position, shipSize);

        var shipInstance = new ShipInstanceData(shipId, position, isHorizontal)
        {
            occupiedCells = occupiedCells
        };

        foreach (var cellPos in occupiedCells)
        {
            var cellData = cells[cellPos.x, cellPos.y];
            cellData.shipInstanceId = shipId;
            cells[cellPos.x, cellPos.y] = cellData;
        }

        ships[shipId] = shipInstance;
        return true;
    }

    public bool RemoveShip(int shipId)
    {
        if (!ships.TryGetValue(shipId, out var shipInstance))
        {
            return false;
        }

        foreach (var cellPos in shipInstance.occupiedCells)
        {
            var cellData = cells[cellPos.x, cellPos.y];
            cellData.shipInstanceId = -1;
            cells[cellPos.x, cellPos.y] = cellData;
        }

        ships.Remove(shipId);
        return true;
    }

    public bool Attack(Vector2Int position)
    {
        if (!IsWithinBounds(position))
        {
            return false;
        }

        var cellData = cells[position.x, position.y];

        if (cellData.state != CellState.Unknown)
        {
            return false;
        }

        if (cellData.HasShip)
        {
            cellData.state = CellState.Hit;
            cells[position.x, position.y] = cellData;

            if (ships.TryGetValue(cellData.shipInstanceId, out var shipInstance))
            {
                shipInstance.hitCount++;
                ships[cellData.shipInstanceId] = shipInstance;
            }

            return true;
        }

        cellData.state = CellState.Empty;
        cells[position.x, position.y] = cellData;
        return false;
    }

    public List<Vector2Int> AttackMultiple(List<Vector2Int> positions)
    {
        var hitPositions = new List<Vector2Int>();

        foreach (var pos in positions)
        {
            if (Attack(pos))
            {
                hitPositions.Add(pos);
            }
        }

        return hitPositions;
    }

    public CellData GetCell(Vector2Int position)
    {
        if (!IsWithinBounds(position))
        {
            return new CellData(position, Core.Models.CellState.Unknown, -1);
        }

        return cells[position.x, position.y];
    }

    public CellData[,] GetAllCells()
    {
        return (CellData[,])cells.Clone();
    }

    public ShipInstanceData? GetShipAt(Vector2Int position)
    {
        var cell = GetCell(position);
        if (cell.HasShip && ships.TryGetValue(cell.shipInstanceId, out var shipInstance))
        {
            return shipInstance;
        }

        return null;
    }

    public ShipInstanceData? GetShip(int shipId)
    {
        return ships.TryGetValue(shipId, out var shipInstance) ? shipInstance : null;
    }

    public List<ShipInstanceData> GetAllShips()
    {
        return ships.Values.ToList();
    }

    public bool AllShipsSunk()
    {
        return ships.Count > 0 && ships.Values.All(ship => ship.IsSunk);
    }

    public void MarkAdjacentEmpty(Vector2Int position)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var adjacentPos = position + new Vector2Int(dx, dy);

                if (IsWithinBounds(adjacentPos))
                {
                    var cellData = cells[adjacentPos.x, adjacentPos.y];
                    if (cellData.state == Core.Models.CellState.Unknown && !cellData.HasShip)
                    {
                        cellData.state = Core.Models.CellState.Empty;
                        cells[adjacentPos.x, adjacentPos.y] = cellData;
                    }
                }
            }
        }
    }

    public int GetShipCount()
    {
        return ships.Count;
    }

    public List<Vector2Int> GetCellsWithShips()
    {
        var result = new List<Vector2Int>();

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (cells[x, y].HasShip)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        return result;
    }

    private bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < GridWidth &&
               position.y >= 0 && position.y < GridHeight;
    }

    private bool IsWithinBounds(Vector2Int position, Vector2Int size)
    {
        return position.x >= 0 && position.x + size.x <= GridWidth &&
               position.y >= 0 && position.y + size.y <= GridHeight;
    }

    private List<Vector2Int> GetOccupiedCells(Vector2Int position, Vector2Int size)
    {
        var result = new List<Vector2Int>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                result.Add(position + new Vector2Int(x, y));
            }
        }

        return result;
    }

    public string ToJson()
    {
        var boardJson = new BoardJson
        {
            cells = cells,
            ships = ships.Values.ToList()
        };

        return JsonUtility.ToJson(boardJson);
    }

    public static Board FromJson(string json)
    {
        var boardJson = JsonUtility.FromJson<BoardJson>(json);
        var board = new Board();

        board.cells = boardJson.cells;

        foreach (var ship in boardJson.ships)
        {
            board.ships[ship.shipId] = ship;
        }

        return board;
    }

    [System.Serializable]
    private class BoardJson
    {
        public CellData[,] cells;
        public List<ShipInstanceData> ships;
    }
}
