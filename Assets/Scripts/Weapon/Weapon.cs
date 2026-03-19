using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for all weapons.
/// Handles the execution of a weapon attack using a strategy pattern.
/// Coordinates between the strategy (which cells to affect) and the grid/board (actual attack).
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    protected IWeaponStrategy strategy;
    protected WeaponData weaponData;
    protected bool isInitialized = false;

    /// <summary>
    /// Initializes the weapon with its data and strategy.
    /// Must be called before ExecuteWeapon().
    /// </summary>
    public virtual void Initialize (WeaponData data, IWeaponStrategy weaponStrategy)
    {
        weaponData = data;
        strategy = weaponStrategy;
        isInitialized = true;

        Debug.Log($"[Weapon] Initialized {GetWeaponName()} with strategy {strategy.GetWeaponName()}");
    }

    /// <summary>
    /// Executes the weapon attack at the given position on the target grid.
    /// This is the main execution method called by the command executor.
    /// </summary>
    /// <param name="centerPosition">The center/primary target position.</param>
    /// <param name="targetGrid">The grid being attacked.</param>
    /// <param name="targetBoard">The logic board for the target grid.</param>
    /// <returns>List of positions that were attacked.</returns>
    public virtual List<Vector2Int> ExecuteWeapon (Vector2Int centerPosition, GridManager targetGrid, Board targetBoard)
    {
        if (!isInitialized)
        {
            Debug.LogError($"[Weapon] Cannot execute {GetWeaponName()} - not initialized!");
            return new List<Vector2Int>();
        }

        if (targetGrid == null || targetBoard == null)
        {
            Debug.LogError($"[Weapon] Cannot execute {GetWeaponName()} - null grid or board!");
            return new List<Vector2Int>();
        }

        // Get the cells affected by this weapon strategy
        List<Vector2Int> affectedCells = strategy.GetAffectedCells(centerPosition, targetGrid);

        if (affectedCells.Count == 0)
        {
            Debug.LogWarning($"[Weapon] {GetWeaponName()} affected no cells at position {centerPosition}");
            return affectedCells;
        }

        // Execute the attack on all affected cells
        ApplyWeaponEffects(affectedCells, targetGrid, targetBoard);

        Debug.Log($"[Weapon] {GetWeaponName()} executed at {centerPosition}, affected {affectedCells.Count} cells");

        return affectedCells;
    }

    /// <summary>
    /// Applies the weapon effects to the affected cells.
    /// Override this in subclasses for special behavior (Radar, Anti-Aircraft, etc.).
    /// Default behavior is to attack all cells via Board.AttackMultiple().
    /// </summary>
    protected virtual void ApplyWeaponEffects (List<Vector2Int> affectedCells, GridManager targetGrid, Board targetBoard)
    {
        // Default: attack all affected cells
        targetBoard.AttackMultiple(affectedCells);

        // Animate attacks in the grid view
        foreach (var position in affectedCells)
        {
            targetGrid.AttackCell(position);
        }
    }

    /// <summary>
    /// Gets the weapon name from the strategy.
    /// </summary>
    public string GetWeaponName ()
    {
        return strategy != null ? strategy.GetWeaponName() : "UnknownWeapon";
    }

    /// <summary>
    /// Gets the weapon type from the strategy.
    /// </summary>
    public WeaponType GetWeaponType ()
    {
        return strategy != null ? strategy.GetWeaponType() : WeaponType.NormalShot;
    }

    /// <summary>
    /// Gets the weapon data.
    /// </summary>
    public WeaponData GetWeaponData ()
    {
        return weaponData;
    }

    /// <summary>
    /// Gets the weapon strategy for use in preview and analysis.
    /// </summary>
    public IWeaponStrategy GetStrategy ()
    {
        return strategy;
    }
}
