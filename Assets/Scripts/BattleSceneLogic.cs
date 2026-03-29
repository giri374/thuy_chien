using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;
using UnityEngine;

public enum Turn
{
    Player1,
    Player2
}

public enum GameState
{
    Setup,
    Playing,
    GameOver
}

public enum WeaponType
{
    NormalShot,
    NuclearBomb,
    Bomber,
    Torpedoes,
    Radar,
    AntiAircraft
}

public class BattleSceneLogic : MonoBehaviour
{
    public static BattleSceneLogic Instance { get; private set; }

    [Header("Grid References")]
    public GridManager player1Grid;
    public GridManager player2Grid;

    [Header("Bot (chỉ dùng khi PlayWithBot)")]
    public BotController botController;

    [Header("Command System")]
    private AttackCommandHistory commandHistory;
    private AttackCommandExecutor commandExecutor;
    private int commandCounter = 0;  // Counter for assigning sequential indices to commands
    private int lastExecutedIndex = -1;  // Index of the last successfully executed command

    public GameState currentState = GameState.Playing;
    public Turn currentTurn = Turn.Player1;

    private GameMode gameMode => GameManager.Instance != null
        ? GameManager.Instance.gameMode
        : GameMode.PlayWithBot;

    // ── Weapon System ──────────────────────────────────────────
    private WeaponType lastUsedWeapon = WeaponType.NormalShot;
    private int lastCommandIndex = -1;  // Index of the current command being executed

    public delegate void TurnChangedHandler (Turn currentTurn, GameMode gameMode);
    public delegate void PassAndPlayNeededHandler (Turn nextTurn);
    public delegate void GameOverHandler (bool player1Won, GameMode gameMode);

    public event TurnChangedHandler onTurnChanged;
    public event PassAndPlayNeededHandler onPassAndPlayNeeded;
    public event GameOverHandler onGameOver;

    private void Awake ()
    {
        Instance = this;

        // Initialize command system
        commandHistory = new AttackCommandHistory();

        // Create and initialize the executor
        if (commandExecutor == null)
        {
            commandExecutor = gameObject.AddComponent<AttackCommandExecutor>();
        }
    }

    private void Start ()
    {
        SetupBotIfNeeded();
        LoadAllShips();

        // Initialize command executor with grid references
        if (commandExecutor != null)
        {
            commandExecutor.Initialize(player1Grid, player2Grid);
        }

        currentTurn = Turn.Player1;
        onTurnChanged?.Invoke(currentTurn, gameMode);

        Debug.Log($"[BattleSceneManager] Game started | Mode: {gameMode}");
    }

    private void SetupBotIfNeeded ()
    {
        if (gameMode != GameMode.PlayWithBot)
        {
            return;
        }

        if (botController == null)
        {
            botController = GetComponent<BotController>()
                         ?? gameObject.AddComponent<BotController>();
        }

        botController.myGrid = player2Grid;
        botController.targetGrid = player1Grid;

        if (GameManager.Instance != null)
        {
            botController.shipListData = GameManager.Instance.shipListData;
        }
    }

    private void LoadAllShips ()
    {
        // Load Player 1 luôn từ GameManager
        LoadShipsFromData(GameManager.Instance?.GetPlacements(1), player1Grid);

        if (gameMode == GameMode.PlayWithBot)
        {
            // Bot tự đặt tàu ngẫu nhiên
            botController?.PlaceShipsRandomly();
        }
        else
        {
            LoadShipsFromData(GameManager.Instance?.GetPlacements(2), player2Grid);
        }
    }

    private void LoadShipsFromData (List<GameManager.ShipPlacementData> placements, GridManager grid)
    {
        if (placements == null || grid == null)
        {
            return;
        }

        var shipListData = GameManager.Instance?.shipListData;
        if (shipListData == null)
        {
            Debug.LogWarning("[BattleSceneManager] shipListData is null!");
            return;
        }

        foreach (var data in placements)
        {
            var shipData = shipListData.GetShipByID(data.shipID);
            if (shipData?.shipPrefab == null)
            {
                continue;
            }

            var obj = Instantiate(shipData.shipPrefab, grid.transform);
            var ship = obj.GetComponent<Ship>();
            if (ship == null)
            {
                continue;
            }

            ship.Initialize(shipData);

            if (!data.isHorizontal)
            {
                ship.Rotate();
            }

            grid.PlaceShip(ship, data.position);
        }
    }

    /// <summary>
    /// Creates an attack command and executes it asynchronously.
    /// This is the main entry point for all attack actions (player and bot).
    /// The result is handled via the callback, which then invokes the normal game flow logic.
    /// Routes through the weapon system for special weapons.
    /// </summary>
    private async void CreateAndExecuteAttackCommandAsync (WeaponType weaponType, Vector2Int position, Turn attacker)
    {
        // Store weapon type for result handling
        lastUsedWeapon = weaponType;

        // Create the command (immutable input data) with sequential index
        int currentCommandIndex = commandCounter++;
        lastCommandIndex = currentCommandIndex;  // Store for result handling
        IAttackCommand command = new AttackCommand(weaponType, position, attacker, currentCommandIndex);

        // Record the command in history for replays and network sync
        commandHistory.RecordCommand(command);

        // Execute the command asynchronously
        // Use the weapon executor which handles both single-cell and multi-cell attacks
        if (weaponType != WeaponType.NormalShot)
        {
            await commandExecutor.ExecuteWeaponAttackAsync(command, HandleAttackResult);
        }
        else
        {
            await commandExecutor.ExecuteAsync(command, HandleAttackResult);
        }
    }

    /// <summary>
    /// Public method for BotController to execute an attack command.
    /// This is called from BotController.ProcessTurn() after selecting a target position.
    /// The bot-specific logic (adding neighbors on hit) is handled here before executing the command.
    /// </summary>
    public async void ExecuteBotAttackCommand (WeaponType weaponType, Vector2Int position)
    {
        // Store weapon type for result handling
        lastUsedWeapon = weaponType;

        // Create the command with bot (Player 2) as attacker and sequential index
        int currentCommandIndex = commandCounter++;
        lastCommandIndex = currentCommandIndex;  // Store for result handling
        IAttackCommand command = new AttackCommand(weaponType, position, Turn.Player2, currentCommandIndex);

        // Record the command in history
        commandHistory.RecordCommand(command);

        // Execute the command with a special callback for bot result handling
        await commandExecutor.ExecuteAsync(command, HandleBotAttackResult);
    }

    /// <summary>
    /// Callback handler for attack command results.
    /// This is called after a command executes and handles the game logic response
    /// (checking for sinks, switching turns, etc.)
    /// </summary>
    private void HandleAttackResult (CellAttackResult result)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        // Successfully executed the command - update lastExecutedIndex
        lastExecutedIndex = lastCommandIndex;

        // Determine which player attacked based on the current turn
        bool isPlayer1Attacking = currentTurn == Turn.Player1;
        GridManager targetGrid = isPlayer1Attacking ? player2Grid : player1Grid;
        GridManager attackerGrid = isPlayer1Attacking ? player1Grid : player2Grid;
        int attackerIndex = isPlayer1Attacking ? 1 : 2;

        // Handle Radar attack (doesn't consume turn, costs CP)
        if (lastUsedWeapon == WeaponType.Radar)
        {
            int cpCost = GetWeaponCPCost(lastUsedWeapon);
            if (cpCost > 0)
            {
                GameManager.Instance.SubtractCP(attackerIndex, cpCost);
                Debug.Log($"[BattleSceneLogic] Player {attackerIndex} used Radar, cost: {cpCost} CP");
            }
            // Radar doesn't switch turns - same player goes again
            if (BattleWeaponManager.Instance != null)
            {
                BattleWeaponManager.Instance.SelectWeapon(WeaponType.NormalShot);
            }
            return;
        }

        // Handle Anti-Aircraft attack (defensive, doesn't consume opponent's turn but costs CP)
        if (lastUsedWeapon == WeaponType.AntiAircraft)
        {
            int cpCost = GetWeaponCPCost(lastUsedWeapon);
            if (cpCost > 0)
            {
                GameManager.Instance.SubtractCP(attackerIndex, cpCost);
                Debug.Log($"[BattleSceneLogic] Player {attackerIndex} set Anti-Aircraft defense, cost: {cpCost} CP");
            }
            // Anti-Aircraft doesn't switch turns - same player goes again
            if (BattleWeaponManager.Instance != null)
            {
                BattleWeaponManager.Instance.SelectWeapon(WeaponType.NormalShot);
            }
            return;
        }

        // Normal attacks and offensive weapons (NuclearBomb, Bomber, Torpedoes)
        // Add/Subtract CP based on weapon used
        if (lastUsedWeapon == WeaponType.NormalShot)
        {
            // NormalShot gives +1 CP on each attack
            GameManager.Instance.AddCP(attackerIndex, 1);
        }
        else
        {
            // Subtract CP cost for special weapons
            int cpCost = GetWeaponCPCost(lastUsedWeapon);
            if (cpCost > 0)
            {
                GameManager.Instance.SubtractCP(attackerIndex, cpCost);
                Debug.Log($"[BattleSceneLogic] Player {attackerIndex} used {lastUsedWeapon}, cost: {cpCost} CP");
            }
        }

        // Check if any ships were sunk
        CheckSunkShips(targetGrid);

        // Check if all ships in target grid are sunk - if so, game over
        if (targetGrid.AllShipsSunk())
        {
            EndGame(player1Won: isPlayer1Attacking);
            return;
        }

        // Handle turn logic: miss = switch turn, hit = bonus turn
        if (!result.WasHit)
        {
            SwitchTurn();
        }
        else
        {
            Debug.Log($"[BattleSceneLogic] Bonus turn for {currentTurn}!");
        }

        // Reset weapon to NormalShot after attack
        if (BattleWeaponManager.Instance != null)
        {
            BattleWeaponManager.Instance.SelectWeapon(WeaponType.NormalShot);
        }
    }

    /// <summary>
    /// Special callback handler for bot attack results.
    /// Handles bot-specific logic like adding neighbors to target list on hit.
    /// This replaces the old OnBotFinishedTurn method.
    /// </summary>
    private void HandleBotAttackResult (CellAttackResult result)
    {
        // Successfully executed the command - update lastExecutedIndex
        lastExecutedIndex = lastCommandIndex;

        // Add/Subtract CP based on weapon used
        if (lastUsedWeapon == WeaponType.NormalShot)
        {
            // NormalShot gives +1 CP on each attack
            GameManager.Instance.AddCP(2, 1);
        }
        else
        {
            // Subtract CP cost for special weapons
            int cpCost = GetWeaponCPCost(lastUsedWeapon);
            if (cpCost > 0)
            {
                GameManager.Instance.SubtractCP(2, cpCost);
                Debug.Log($"[BattleSceneLogic] Bot used {lastUsedWeapon}, cost: {cpCost} CP");
            }
        }

        // Check if player 1's ships are sunk
        CheckSunkShips(player1Grid);

        if (player1Grid.AllShipsSunk())
        {
            EndGame(player1Won: false);
            return;
        }

        if (result.WasHit)
        {
            // Add neighboring cells to bot's target list for next attack
            botController?.AddNeighborsToTargets(result.Position);

            // Bot gets a bonus turn on hit
            botController?.MakeTurn();
        }
        else
        {
            // Miss means switch turn
            SwitchTurn();
        }
    }

    public void OnPlayer1GridCellClicked (Cell cell)
    {
        if (gameMode == GameMode.PlayWithFriend && currentTurn == Turn.Player2)
        {
            HandleAttack(cell, isPlayer1Attacking: false);
        }
    }

    public void OnPlayer2GridCellClicked (Cell cell)
    {
        if (currentTurn == Turn.Player1)
        {
            HandleAttack(cell, isPlayer1Attacking: true);
        }
    }

    private void HandleAttack (Cell cell, bool isPlayer1Attacking)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        if (cell.cellState != CellState.Unknown)
        {
            return;
        }

        // Determine the attacker based on the current turn
        Turn attacker = isPlayer1Attacking ? Turn.Player1 : Turn.Player2;

        // Get the selected weapon from BattleWeaponManager
        WeaponType weaponToUse = WeaponType.NormalShot;
        if (BattleWeaponManager.Instance != null)
        {
            weaponToUse = BattleWeaponManager.Instance.GetCurrentWeapon();
        }

        // Create and execute the attack command with the selected weapon
        CreateAndExecuteAttackCommandAsync(weaponToUse, cell.gridPosition, attacker);
    }

    private void SwitchTurn ()
    {
        if (gameMode == GameMode.PlayWithBot)
        {
            currentTurn = (currentTurn == Turn.Player1) ? Turn.Player2 : Turn.Player1;
            onTurnChanged?.Invoke(currentTurn, gameMode);

            if (currentTurn == Turn.Player2)
            {
                botController?.MakeTurn();
            }
        }
        else
        {
            if (currentTurn == Turn.Player1)
            {
                currentTurn = Turn.Player2;
            }
            else
            {
                currentTurn = Turn.Player1;
            }

            onPassAndPlayNeeded?.Invoke(currentTurn);
        }
    }

    public void OnPassAndPlayReady ()
    {
        onTurnChanged?.Invoke(currentTurn, gameMode);
    }

    /// <summary>
    /// Get CP cost of a weapon from the weapon list data
    /// </summary>
    private int GetWeaponCPCost (WeaponType weaponType)
    {
        if (GameManager.Instance?.weaponListData == null)
        {
            return 0;
        }

        var weaponData = GameManager.Instance.weaponListData.GetWeaponByType(weaponType);
        return weaponData?.cpCost ?? 0;
    }

    private void CheckSunkShips (GridManager grid)
    {
        var board = grid.GetBoard();
        if (board == null)
        {
            return;
        }

        var allShips = board.GetAllShips();
        foreach (var shipInstance in allShips)
        {
            if (shipInstance.IsSunk)
            {
                foreach (var cellPos in shipInstance.occupiedCells)
                {
                    board.MarkAdjacentEmpty(cellPos);
                }

                var legacyShip = grid.ships.Find(s => s != null && s.shipData != null && s.shipData.shipID == shipInstance.shipId);
                if (legacyShip != null)
                {
                    legacyShip.SetVisible(true);
                }

                grid.GetGridView().SyncFromBoard();
            }
        }
    }

    private async void EndGame (bool player1Won)
    {
        currentState = GameState.GameOver;

        Debug.Log($"[BattleSceneManager] Game Over — {(player1Won ? "Player 1 won" : "Player 2/Bot won")}");

        if (ProgressManager.Instance != null && ProgressManager.Instance.IsReady)
        {
            if (player1Won)
            {
                ProgressManager.Instance.AddGold(10);
                ProgressManager.Instance.AddExperience(30);
                ProgressManager.Instance.Data.totalWins++;
            }
            else
            {
                ProgressManager.Instance.AddGold(0);
                ProgressManager.Instance.AddExperience(10);
                ProgressManager.Instance.Data.totalLosses++;
            }

            await ProgressManager.Instance.SaveProgress();
        }

        onGameOver?.Invoke(player1Won, gameMode);
    }
}