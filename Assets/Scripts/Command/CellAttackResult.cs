using UnityEngine;

/// <summary>
/// Result data from attacking a single cell.
/// Contains information about what happened when the cell was attacked.
/// </summary>
public class CellAttackResult
{
    /// <summary>
    /// The position that was attacked.
    /// </summary>
    public Vector2Int Position { get; set; }

    /// <summary>
    /// Whether the attack hit a ship or missed.
    /// </summary>
    public bool WasHit { get; set; }

    /// <summary>
    /// The ship that was hit (if any). Null if miss.
    /// </summary>
    public Ship HitShip { get; set; }

    /// <summary>
    /// Whether a ship was sunk as a result of this attack.
    /// </summary>
    public bool ShipSunk { get; set; }

    public CellAttackResult()
    {
        Position = Vector2Int.zero;
        WasHit = false;
        HitShip = null;
        ShipSunk = false;
    }

    public CellAttackResult(Vector2Int position, bool wasHit, Ship hitShip = null, bool shipSunk = false)
    {
        Position = position;
        WasHit = wasHit;
        HitShip = hitShip;
        ShipSunk = shipSunk;
    }
}
