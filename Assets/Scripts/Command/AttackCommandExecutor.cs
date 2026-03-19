using System;
using System.Threading.Tasks;
using UnityEngine;
using Core.Models;

/// <summary>
/// Executes attack commands and bridges them to the actual game logic (GridManager).
/// Handles the conversion from command input data to GridManager.AttackCell() calls
/// and ensures proper result callbacks.
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
    /// Executes an attack command asynchronously.
    /// Determines the target grid based on the attacker and executes the attack.
    /// Invokes the callback with the result when complete.
    /// </summary>
    /// <param name="command">The attack command to execute.</param>
    /// <param name="resultCallback">Callback to handle the attack result.</param>
    public async Task ExecuteAsync (IAttackCommand command, Action<CellAttackResult> resultCallback)
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

        // TODO: Check if ship was sunk (for future enhancement)
        // For now, this will be handled in BattleSceneLogic.CheckSunkShips()

        Debug.Log($"[AttackCommandExecutor] {command.GetAttacker()} attacks {targetPos}: {(wasHit ? "HIT" : "MISS")}");

        // Invoke the callback with the result (this will be handled in BattleSceneLogic)
        resultCallback?.Invoke(result);

        // Complete the async task
        await Task.CompletedTask;
    }
}
