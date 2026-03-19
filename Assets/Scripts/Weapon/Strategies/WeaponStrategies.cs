using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strategy for single-cell attacks (NormalShot).
/// Attacks only the center cell.
/// </summary>
public class SingleCellStrategy : IWeaponStrategy
{
    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();

        if (targetGrid.GetCell(centerPosition) != null)
        {
            affectedCells.Add(centerPosition);
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.NormalShot;
    }

    public string GetWeaponName ()
    {
        return "Normal Shot";
    }
}

/// <summary>
/// Strategy for Nuclear Bomb: attacks a 5x5 area centered at the clicked position.
/// Clamps to grid bounds to prevent out-of-bounds attacks.
/// </summary>
public class NuclearBombStrategy : IWeaponStrategy
{
    private const int RADIUS = 2; // 5x5 = radius of 2 from center

    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();

        int minX = Mathf.Max(0, centerPosition.x - RADIUS);
        int maxX = Mathf.Min(targetGrid.gridWidth - 1, centerPosition.x + RADIUS);
        int minY = Mathf.Max(0, centerPosition.y - RADIUS);
        int maxY = Mathf.Min(targetGrid.gridHeight - 1, centerPosition.y + RADIUS);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var pos = new Vector2Int(x, y);
                if (targetGrid.GetCell(pos) != null)
                {
                    affectedCells.Add(pos);
                }
            }
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.NuclearBomb;
    }

    public string GetWeaponName ()
    {
        return "Nuclear Bomb";
    }
}

/// <summary>
/// Strategy for Bomber: attacks an entire horizontal row at the clicked Y position.
/// All cells in the row from x=0 to x=gridWidth are affected.
/// </summary>
public class BomberStrategy : IWeaponStrategy
{
    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();

        // Attack all cells in the same row (Y == centerPosition.y)
        for (int x = 0; x < targetGrid.gridWidth; x++)
        {
            var pos = new Vector2Int(x, centerPosition.y);
            if (targetGrid.GetCell(pos) != null)
            {
                affectedCells.Add(pos);
            }
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.Bomber;
    }

    public string GetWeaponName ()
    {
        return "Bomber";
    }
}

/// <summary>
/// Strategy for Torpedoes: scans a horizontal row and attacks ONLY the first ship cell found.
/// Intelligent targeting: searches from left to right along the row.
/// </summary>
public class TorpedoesStrategy : IWeaponStrategy
{
    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();
        Board board = targetGrid.GetBoard();

        if (board == null)
        {
            Debug.LogError("[TorpedoesStrategy] Cannot access board!");
            return affectedCells;
        }

        // Scan from left to right along the row at centerPosition.y
        for (int x = 0; x < targetGrid.gridWidth; x++)
        {
            var pos = new Vector2Int(x, centerPosition.y);
            var cellData = board.GetCell(pos);

            // Check if this cell has a ship
            if (cellData.HasShip)
            {
                affectedCells.Add(pos);
                Debug.Log($"[TorpedoesStrategy] Found ship at {pos}");
                break; // Only attack the first ship found
            }
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.Torpedoes;
    }

    public string GetWeaponName ()
    {
        return "Torpedoes";
    }
}

/// <summary>
/// Strategy for Radar: reveals a 3x3 area centered at the clicked position.
/// Shows which cells contain ships without attacking them.
/// </summary>
public class RadarStrategy : IWeaponStrategy
{
    private const int RADIUS = 1; // 3x3 = radius of 1 from center

    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();

        int minX = Mathf.Max(0, centerPosition.x - RADIUS);
        int maxX = Mathf.Min(targetGrid.gridWidth - 1, centerPosition.x + RADIUS);
        int minY = Mathf.Max(0, centerPosition.y - RADIUS);
        int maxY = Mathf.Min(targetGrid.gridHeight - 1, centerPosition.y + RADIUS);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var pos = new Vector2Int(x, y);
                if (targetGrid.GetCell(pos) != null)
                {
                    affectedCells.Add(pos);
                }
            }
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.Radar;
    }

    public string GetWeaponName ()
    {
        return "Radar";
    }
}

/// <summary>
/// Strategy for Anti-Aircraft: marks a 3x3 area on the player's own grid.
/// Blocks incoming attacks (NuclearBomb, Bomber, Torpedoes) from hitting marked cells.
/// </summary>
public class AntiAircraftStrategy : IWeaponStrategy
{
    private const int RADIUS = 1; // 3x3 = radius of 1 from center

    public List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid)
    {
        var affectedCells = new List<Vector2Int>();

        int minX = Mathf.Max(0, centerPosition.x - RADIUS);
        int maxX = Mathf.Min(targetGrid.gridWidth - 1, centerPosition.x + RADIUS);
        int minY = Mathf.Max(0, centerPosition.y - RADIUS);
        int maxY = Mathf.Min(targetGrid.gridHeight - 1, centerPosition.y + RADIUS);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var pos = new Vector2Int(x, y);
                if (targetGrid.GetCell(pos) != null)
                {
                    affectedCells.Add(pos);
                }
            }
        }

        return affectedCells;
    }

    public WeaponType GetWeaponType ()
    {
        return WeaponType.AntiAircraft;
    }

    public string GetWeaponName ()
    {
        return "Anti-Aircraft Missile";
    }
}
