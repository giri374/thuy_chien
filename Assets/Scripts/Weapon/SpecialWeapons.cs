using System.Collections.Generic;
using UnityEngine;
using Core.Models;

/// <summary>
/// Default weapon implementation for standard attacking weapons.
/// Uses the standard attack flow: get affected cells and attack them all.
/// </summary>
public class DefaultWeapon : Weapon
{
    // No additional behavior needed - uses base class defaults
}

/// <summary>
/// Special weapon for Radar that reveals ships without attacking.
/// Overrides ApplyWeaponEffects to show ship locations instead of attacking.
/// </summary>
public class RadarWeapon : Weapon
{
    /// <summary>
    /// Radar reveals ships without attacking them.
    /// Shows hint sprites on cells containing ships.
    /// </summary>
    protected override void ApplyWeaponEffects (List<Vector2Int> affectedCells, GridManager targetGrid, Board targetBoard)
    {
        foreach (var position in affectedCells)
        {
            Cell cell = targetGrid.GetCell(position);
            if (cell == null) continue;

            // Check if there's a ship at this position
            if (cell.occupyingShip != null)
            {
                // Mark as RadarHinted to show hint sprite
                cell.SetCellState(CellState.RadarHinted);
                cell.UpdateVisual();

                Debug.Log($"[RadarWeapon] Ship detected at {position}");
            }
            else
            {
                // No ship here, but still mark as checked
                Debug.Log($"[RadarWeapon] No ship at {position}");
            }
        }
    }
}

/// <summary>
/// Special weapon for Anti-Aircraft that marks defensive areas on the player's own grid.
/// Overrides ApplyWeaponEffects to mark cells as protected instead of attacking.
/// </summary>
public class AntiAircraftWeapon : Weapon
{
    /// <summary>
    /// Anti-Aircraft marks cells as protected on the player's own grid.
    /// These marked cells will block incoming attacks.
    /// </summary>
    protected override void ApplyWeaponEffects (List<Vector2Int> affectedCells, GridManager targetGrid, Board targetBoard)
    {
        foreach (var position in affectedCells)
        {
            // Mark the cell as anti-aircraft protected
            targetBoard.MarkAntiAircraft(position);

            Cell cell = targetGrid.GetCell(position);
            if (cell != null)
            {
                cell.SetCellState(CellState.AntiAircraftMarked);
                cell.UpdateVisual();
                Debug.Log($"[AntiAircraftWeapon] Protected {position}");
            }
        }
    }
}
