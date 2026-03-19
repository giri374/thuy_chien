using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for weapon attack strategies.
/// Each weapon type implements this to define how it affects the grid.
/// </summary>
public interface IWeaponStrategy
{
    /// <summary>
    /// Gets the list of cells affected by this weapon when fired at the given center position.
    /// This does NOT execute the attack, just determines which cells are affected.
    /// </summary>
    /// <param name="centerPosition">The clicked cell position (center of effect).</param>
    /// <param name="targetGrid">The grid being attacked (for bounds checking).</param>
    /// <returns>List of affected cell positions. Empty list if none affected.</returns>
    List<Vector2Int> GetAffectedCells (Vector2Int centerPosition, GridManager targetGrid);

    /// <summary>
    /// Gets the weapon type this strategy implements.
    /// </summary>
    WeaponType GetWeaponType ();

    /// <summary>
    /// Gets the weapon name for logging/debugging.
    /// </summary>
    string GetWeaponName ();
}
