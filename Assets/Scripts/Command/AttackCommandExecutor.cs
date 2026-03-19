using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Models;

/// <summary>
/// Executes attack commands and bridges them to the actual game logic (GridManager).
/// Handles the conversion from command input data to GridManager.AttackCell() calls
/// and ensures proper result callbacks.
/// Supports both single-cell (NormalShot) and multi-cell weapon attacks.
/// </summary>
public class AttackCommandExecutor : MonoBehaviour
{
    private GridManager _player1Grid;
    private GridManager _player2Grid;

    /// <summary>
    /// Initializes the executor with references to both player grids.
    /// Should be called from BattleSceneLogic during setup.
    /// </summary>
    public void Initialize (GridManager player1Grid, GridManager player2Grid)
    {
        _player1Grid = player1Grid;
        _player2Grid = player2Grid;
    }

    /// <summary>
    /// Executes an attack command asynchronously using the weapon system.
    /// For offensive weapons: targets opponent's grid
    /// For defensive weapons (Anti-Aircraft): targets attacker's own grid
    /// For utility weapons (Radar): targets opponent's grid but doesn't damage
    /// Invokes the callback with aggregated results.
    /// </summary>
    /// <param name="command">The attack command to execute.</param>
    /// <param name="resultCallback">Callback to handle the attack result.</param>
    public async Task ExecuteWeaponAttackAsync (IAttackCommand command, Action<CellAttackResult> resultCallback)
    {
        if (command == null)
        {
            Debug.LogError("[AttackCommandExecutor] Attempted to execute null command!");
            return;
        }

        if (_player1Grid == null || _player2Grid == null)
        {
            Debug.LogError("[AttackCommandExecutor] Grid references not initialized!");
            return;
        }

        Vector2Int targetPos = command.GetPosition();
        WeaponType weaponType = command.GetWeaponType();
        Turn attacker = command.GetAttacker();

        // Determine which grid is affected based on weapon type
        GridManager targetGrid;
        if (weaponType == WeaponType.AntiAircraft)
        {
            // Anti-Aircraft is placed on attacker's OWN grid (defensive)
            targetGrid = attacker == Turn.Player1 ? _player1Grid : _player2Grid;
        }
        else
        {
            // Offensive and utility weapons target opponent's grid
            targetGrid = attacker == Turn.Player1 ? _player2Grid : _player1Grid;
        }

        // Get the target cell to verify it's valid before attacking
        Cell targetCell = targetGrid.GetCell(targetPos);
        if (targetCell == null)
        {
            Debug.LogWarning($"[AttackCommandExecutor] Invalid target position: {targetPos}");
            return;
        }

        Board targetBoard = targetGrid.GetBoard();
        if (targetBoard == null)
        {
            Debug.LogError("[AttackCommandExecutor] Target board is null!");
            return;
        }

        // Clear Radar hints from previous attacks before starting new attack
        targetBoard.ClearRadarHints();

        // Update visuals to remove hint sprites
        for (int x = 0; x < targetGrid.gridWidth; x++)
        {
            for (int y = 0; y < targetGrid.gridHeight; y++)
            {
                Cell cell = targetGrid.GetCell(x, y);
                if (cell != null)
                {
                    cell.UpdateVisual();
                }
            }
        }

        // Create weapon from factory
        Weapon weapon = WeaponFactory.CreateWeapon(weaponType);
        if (weapon == null)
        {
            Debug.LogError($"[AttackCommandExecutor] Failed to create weapon: {weaponType}");
            return;
        }

        // Execute the weapon to get affected cells
        List<Vector2Int> affectedCells = weapon.ExecuteWeapon(targetPos, targetGrid, targetBoard);

        // Create aggregated result
        var result = new CellAttackResult();
        result.Position = targetPos;
        result.WasHit = false;

        // Determine if any cell was a hit
        if (affectedCells.Count > 0)
        {
            foreach (var cellPos in affectedCells)
            {
                Cell cell = targetGrid.GetCell(cellPos);
                if (cell != null && cell.occupyingShip != null)
                {
                    result.WasHit = true;
                    result.HitShip = cell.occupyingShip;
                    break;
                }
            }
        }

        // For Radar attack, return special result indicating no actual damage
        if (weaponType == WeaponType.Radar)
        {
            result.WasHit = false; // Radar doesn't "hit" in the damage sense
            Debug.Log($"[AttackCommandExecutor] {attacker} used Radar at {targetPos}, scanned {affectedCells.Count} cells");
        }
        else if (weaponType == WeaponType.AntiAircraft)
        {
            // Anti-Aircraft is placed on own grid, not an attack
            result.WasHit = false;
            Debug.Log($"[AttackCommandExecutor] {attacker} set Anti-Aircraft defense at {targetPos}");
        }
        else
        {
            Debug.Log($"[AttackCommandExecutor] {attacker} attacks with {weaponType} at {targetPos}: {affectedCells.Count} cells affected, {(result.WasHit ? "HIT" : "MISS")}");
        }

        // Cleanup temporary weapon GameObject
        if (weapon != null)
        {
            Destroy(weapon.gameObject);
        }

        // Invoke the callback with the result
        resultCallback?.Invoke(result);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes an attack command asynchronously.
    /// Determines the target grid based on the attacker and executes the attack.
    /// This is the legacy method for single-cell attacks (NormalShot).
    /// For special weapons, use ExecuteWeaponAttackAsync() instead.
    /// Invokes the callback with the result when complete.
    /// </summary>
    /// <param name="command">The attack command to execute.</param>
    /// <param name="resultCallback">Callback to handle the attack result.</param>
    public async Task ExecuteAsync (IAttackCommand command, Action<CellAttackResult> resultCallback)
    {
        // For weapon-based attacks, use the weapon system
        if (command.GetWeaponType() != WeaponType.NormalShot)
        {
            await ExecuteWeaponAttackAsync(command, resultCallback);
            return;
        }

        if (command == null)
        {
            Debug.LogError("[AttackCommandExecutor] Attempted to execute null command!");
            return;
        }

        if (_player1Grid == null || _player2Grid == null)
        {
            Debug.LogError("[AttackCommandExecutor] Grid references not initialized!");
            return;
        }

        // Determine which grid is the target based on who is attacking
        GridManager targetGrid = command.GetAttacker() == Turn.Player1 ? _player2Grid : _player1Grid;
        Vector2Int targetPos = command.GetPosition();

        // Get the target cell to verify it's valid before attacking
        Cell targetCell = targetGrid.GetCell(targetPos);
        if (targetCell == null)
        {
            Debug.LogWarning($"[AttackCommandExecutor] Invalid target position: {targetPos}");
            return;
        }

        // Check if the cell has already been attacked
        if (targetCell.cellState != CellState.Unknown)
        {
            Debug.LogWarning($"[AttackCommandExecutor] Cell {targetPos} has already been attacked (state: {targetCell.cellState})");
            return;
        }

        // Execute the attack on the target grid
        // This returns whether the attack was a hit
        bool wasHit = targetGrid.AttackCell(targetPos);

        // Create a result object containing the attack outcome
        var result = new CellAttackResult(targetPos, wasHit);

        // If the attack hit a ship, get reference to it
        if (wasHit && targetCell.occupyingShip != null)
        {
            result.HitShip = targetCell.occupyingShip;
        }

        Debug.Log($"[AttackCommandExecutor] {command.GetAttacker()} attacks {targetPos}: {(wasHit ? "HIT" : "MISS")}");

        // Invoke the callback with the result (this will be handled in BattleSceneLogic)
        resultCallback?.Invoke(result);

        // Complete the async task
        await Task.CompletedTask;
    }
}
