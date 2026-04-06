using UnityEngine;

public class AttackResolutionService
{
    public void HandlePlayerAttackResult (
        CellAttackResult result,
        Turn currentTurn,
        GridManager player1Grid,
        GridManager player2Grid,
        WeaponType usedWeapon,
        GameMode gameMode,
        BotController botController,
        System.Action switchTurn,
        System.Action<bool> endGame)
    {
        var isPlayer1Attacking = currentTurn == Turn.Player1;
        var targetGrid = isPlayer1Attacking ? player2Grid : player1Grid;
        var attackerIndex = isPlayer1Attacking ? 1 : 2;

        if (TryHandleDefensiveWeapon(attackerIndex, usedWeapon))
        {
            return;
        }

        ApplyWeaponCostOrReward(attackerIndex, usedWeapon);

        CheckSunkShips(targetGrid, gameMode, botController);
        if (targetGrid.AllShipsSunk())
        {
            endGame?.Invoke(isPlayer1Attacking);
            return;
        }

        if (!result.WasHit)
        {
            switchTurn?.Invoke();
        }
        else
        {
            Debug.Log($"[BattleSceneLogic] Bonus turn for {currentTurn}!");
        }

        ResetSelectedWeapon();
    }

    public void HandleBotAttackResult (
        CellAttackResult result,
        GridManager player1Grid,
        WeaponType usedWeapon,
        GameMode gameMode,
        BotController botController,
        System.Action switchTurn,
        System.Action<bool> endGame)
    {
        if (usedWeapon == WeaponType.NormalShot)
        {
            ApplyNormalShotReward(2);
        }
        else
        {
            ApplyWeaponCost(2, usedWeapon, $"Bot used {usedWeapon}");
        }

        CheckSunkShips(player1Grid, gameMode, botController);
        if (player1Grid.AllShipsSunk())
        {
            endGame?.Invoke(false);
            return;
        }

        if (result.WasHit)
        {
            if (botController != null)
            {
                botController.AddNeighborsToTargets(result.Position);
                botController.MakeTurn();
            }

            return;
        }

        switchTurn?.Invoke();
    }

    private bool TryHandleDefensiveWeapon (int playerIndex, WeaponType usedWeapon)
    {
        if (usedWeapon == WeaponType.Radar)
        {
            ApplyWeaponCost(playerIndex, usedWeapon, "used Radar");
            ResetSelectedWeapon();
            return true;
        }

        if (usedWeapon == WeaponType.AntiAircraft)
        {
            ApplyWeaponCost(playerIndex, usedWeapon, "set Anti-Aircraft defense");
            ResetSelectedWeapon();
            return true;
        }

        return false;
    }

    private void ApplyWeaponCostOrReward (int playerIndex, WeaponType usedWeapon)
    {
        if (usedWeapon == WeaponType.NormalShot)
        {
            ApplyNormalShotReward(playerIndex);
            return;
        }

        ApplyWeaponCost(playerIndex, usedWeapon, $"used {usedWeapon}");
    }

    private int GetWeaponCPCost (WeaponType weaponType)
    {
        if (GameManager.Instance == null || GameManager.Instance.weaponListData == null)
        {
            return 0;
        }

        var weaponData = GameManager.Instance.weaponListData.GetWeaponByType(weaponType);
        return weaponData != null ? weaponData.cpCost : 0;
    }

    private void CheckSunkShips (GridManager grid, GameMode gameMode, BotController botController)
    {
        var board = grid.GetBoard();
        if (board == null)
        {
            return;
        }

        var allShips = board.GetAllShips();
        foreach (var shipInstance in allShips)
        {
            if (!shipInstance.IsSunk)
            {
                continue;
            }

            foreach (var cellPos in shipInstance.occupiedCells)
            {
                board.MarkAdjacentEmpty(cellPos);
            }

            var legacyShip = grid.ships.Find(s => s != null && s.shipData != null && s.shipData.shipID == shipInstance.shipId);
            if (legacyShip != null)
            {
                legacyShip.SetVisible(true);
            }

            if (gameMode == GameMode.PlayWithBot && botController != null)
            {
                botController.RemoveTrackedShip(shipInstance.shipId);
                botController.RemoveTargetPositions(shipInstance.occupiedCells);
            }

            grid.GetGridView().SyncFromBoard();
        }
    }

    private void ApplyNormalShotReward (int playerIndex)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[BattleSceneLogic] GameManager not found; cannot add CP reward.");
            return;
        }

        gameManager.AddCP(playerIndex, 1);
        BattleWeaponManager.Instance?.RefreshCPDisplay();
    }

    private void ApplyWeaponCost (int playerIndex, WeaponType weaponType, string actionLabel)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[BattleSceneLogic] GameManager not found; cannot apply CP cost.");
            return;
        }

        var cpCost = GetWeaponCPCost(weaponType);
        if (cpCost <= 0)
        {
            return;
        }

        gameManager.SubtractCP(playerIndex, cpCost);
        BattleWeaponManager.Instance?.RefreshCPDisplay();
        Debug.Log($"[BattleSceneLogic] Player {playerIndex} {actionLabel}, cost: {cpCost} CP");
    }

    private void ResetSelectedWeapon ()
    {
        var weaponManager = BattleWeaponManager.Instance;
        if (weaponManager != null)
        {
            weaponManager.SelectWeapon(WeaponType.NormalShot);
        }
    }
}
