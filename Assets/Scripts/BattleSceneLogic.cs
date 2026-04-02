using System.Collections.Generic;
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

    private bool _inputBlocked;

    public GameState currentState = GameState.Playing;
    public Turn currentTurn = Turn.Player1;

    private GameMode gameMode => GameManager.Instance != null
        ? GameManager.Instance.gameMode
        : GameMode.PlayWithBot;

    // ── Weapon System ──────────────────────────────────────────
    private WeaponType lastUsedWeapon = WeaponType.NormalShot;

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

        // NEW: subscribe to online command relay
        if (gameMode == GameMode.PlayOnline)
        {
            var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
            if (match != null)
            {
                match.OnCommandReceived += ExecuteReceivedCommand;
                match.OnOpponentDisconnectedChanged += HandleOpponentDisconnectedChanged;
            }
            else
            {
                Debug.LogWarning("[BattleSceneLogic] EGameMatch.Singleton null at Start — online commands won't execute.");
            }
        }

        Debug.Log($"[BattleSceneManager] Game started | Mode: {gameMode}");
    }

    private void HandleOpponentDisconnectedChanged (bool isDisconnected)
    {
        if (isDisconnected)
        {
            Debug.Log("[BattleSceneLogic] Opponent disconnected — blocking input.");
            // TODO: show "Waiting for opponent..." overlay via your UI manager
            // For now, currentState blocks all attacks because we add a check in HandleOnlineAttack:
            _inputBlocked = true;
        }
        else
        {
            Debug.Log("[BattleSceneLogic] Opponent reconnected — unblocking input.");
            _inputBlocked = false;
            // TODO: hide overlay
        }
    }

    private void OnDestroy ()
    {
        if (gameMode == GameMode.PlayOnline)
        {
            var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
            if (match != null)
            {
                match.OnCommandReceived -= ExecuteReceivedCommand;
            }
        }
    }

    private void ExecuteReceivedCommand (Assets.OnlineMode.GameMatch.CommandData data)
    {
        Debug.Log($"[BattleSceneLogic] ExecuteReceivedCommand: index={data.CommandIndex} weapon={data.WeaponType} pos=({data.X},{data.Y}) attacker={data.Attacker}");
        // Guard: skip if already executed (duplicate protection — Step 8)
        if (data.CommandIndex <= lastExecutedIndex)
        {
            Debug.LogWarning($"[BattleSceneLogic] Skipping duplicate command index={data.CommandIndex}");
            return;
        }

        var weapon = (WeaponType) data.WeaponType;
        var pos = new UnityEngine.Vector2Int(data.X, data.Y);
        var attacker = (Turn) data.Attacker;

        // CreateAndExecuteAttackCommandAsync(weapon, pos, attacker);
        ExecuteCommandDirectly(weapon, pos, attacker, data.CommandIndex);

        // Notify EGameMatch that this client has executed up to this index
        Assets.OnlineMode.GameMatch.EGameMatch.Singleton?.NotifyCommandExecuted(data.CommandIndex);
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

    public void OnPlayer1GridCellClicked (Cell cell)
    {
        if (gameMode == GameMode.PlayWithFriend && currentTurn == Turn.Player2)
        {
            HandleAttack(cell, isPlayer1Attacking: false);
            return;
        }

        if (gameMode == GameMode.PlayOnline && currentTurn == Turn.Player2)
        {
            // Only the guest (Player2) can click here during their turn
            if (!IsLocalPlayer2())
            {
                return;
            }

            HandleOnlineAttack(cell, isPlayer1Attacking: false);
        }
    }

    public void OnPlayer2GridCellClicked (Cell cell)
    {
        if (currentTurn != Turn.Player1)
        {
            return;
        }

        if (gameMode == GameMode.PlayOnline)
        {
            // Only the host (Player1) can click here during their turn
            if (!IsLocalPlayer1())
            {
                return;
            }

            HandleOnlineAttack(cell, isPlayer1Attacking: true);
            return;
        }

        HandleAttack(cell, isPlayer1Attacking: true);
    }

    private void HandleAttack (Cell cell, bool isPlayer1Attacking)
    {
        if (!CanAttackCell(cell))
        {
            return;
        }

        Turn attacker = isPlayer1Attacking ? Turn.Player1 : Turn.Player2;
        WeaponType weaponToUse = GetSelectedWeapon();

        CreateAndExecuteAttackCommandAsync(weaponToUse, cell.gridPosition, attacker);

        bool CanAttackCell (Cell selectedCell)
        {
            if (currentState != GameState.Playing)
            {
                return false;
            }

            return selectedCell.cellState == CellState.Unknown;
        }

        WeaponType GetSelectedWeapon ()
        {
            var weaponManager = BattleWeaponManager.Instance;
            return weaponManager != null ? weaponManager.GetCurrentWeapon() : WeaponType.NormalShot;
        }
    }

    private bool IsLocalPlayer1 ()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null && nm.IsHost;
    }

    private bool IsLocalPlayer2 ()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null && nm.IsClient && !nm.IsHost;
    }

    private void HandleOnlineAttack (Cell cell, bool isPlayer1Attacking)
    {
        if (_inputBlocked)
        {
            return;
        }

        if (currentState != GameState.Playing)
        {
            return;
        }

        if (cell.cellState != CellState.Unknown)
        {
            return;
        }

        var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
        if (match == null)
        {
            Debug.LogWarning("[BattleSceneLogic] EGameMatch.Singleton is null — cannot send attack.");
            return;
        }

        WeaponType weapon = BattleWeaponManager.Instance != null
            ? BattleWeaponManager.Instance.GetCurrentWeapon()
            : WeaponType.NormalShot;

        Turn attacker = isPlayer1Attacking ? Turn.Player1 : Turn.Player2;

        var data = new Assets.OnlineMode.GameMatch.CommandData
        {
            WeaponType = (int) weapon,
            X = cell.gridPosition.x,
            Y = cell.gridPosition.y,
            Attacker = (int) attacker,
            CommandIndex = -1  // placeholder — server overwrites this
        };

        Debug.Log($"[BattleSceneLogic] Sending online attack: {data.CommandIndex} weapon={weapon} pos={cell.gridPosition} attacker={attacker}");
        match.SubmitAttack_ServerRpc(data);
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
        IAttackCommand command = new AttackCommand(weaponType, position, attacker, currentCommandIndex);

        // Record the command in history for replays and network sync
        commandHistory.RecordCommand(command);

        // Execute the command asynchronously
        // Use the weapon executor which handles both single-cell and multi-cell attacks
        if (weaponType != WeaponType.NormalShot)
        {
            await commandExecutor.ExecuteWeaponAttackAsync(command, result => HandleAttackResult(result, currentCommandIndex));
        }
        else
        {
            await commandExecutor.ExecuteAsync(command, result => HandleAttackResult(result, currentCommandIndex));
        }
    }

    // New internal method — used ONLY by ExecuteReceivedCommand
    private async void ExecuteCommandDirectly (WeaponType weaponType, Vector2Int position, Turn attacker, int commandIndex)
    {
        lastUsedWeapon = weaponType;

        IAttackCommand command = new AttackCommand(weaponType, position, attacker, commandIndex);
        commandHistory.RecordCommand(command);

        if (weaponType != WeaponType.NormalShot)
            await commandExecutor.ExecuteWeaponAttackAsync(command, result => HandleAttackResult(result, commandIndex));
        else
            await commandExecutor.ExecuteAsync(command, result => HandleAttackResult(result, commandIndex));
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
        IAttackCommand command = new AttackCommand(weaponType, position, Turn.Player2, currentCommandIndex);

        // Record the command in history
        commandHistory.RecordCommand(command);

        // Execute the command with a special callback for bot result handling
        await commandExecutor.ExecuteAsync(command, result => HandleBotAttackResult(result, currentCommandIndex));
    }

    /// <summary>
    /// Callback handler for attack command results.
    /// This is called after a command executes and handles the game logic response
    /// (checking for sinks, switching turns, etc.)
    /// </summary>
    private void HandleAttackResult (CellAttackResult result, int commandIndex)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        lastExecutedIndex = commandIndex;

        bool isPlayer1Attacking = currentTurn == Turn.Player1;
        GridManager targetGrid = isPlayer1Attacking ? player2Grid : player1Grid;
        int attackerIndex = isPlayer1Attacking ? 1 : 2;

        if (TryHandleDefensiveWeapon(attackerIndex))
        {
            return;
        }

        ApplyWeaponCostOrReward(attackerIndex);

        if (CheckGameOverAfterSinks(targetGrid, isPlayer1Attacking))
        {
            return;
        }

        HandleTurnAfterAttack(result);
        ResetSelectedWeapon();

        bool TryHandleDefensiveWeapon (int index)
        {
            if (lastUsedWeapon == WeaponType.Radar)
            {
                ApplyWeaponCost(index, lastUsedWeapon, "used Radar");
                ResetSelectedWeapon();
                return true;
            }

            if (lastUsedWeapon == WeaponType.AntiAircraft)
            {
                ApplyWeaponCost(index, lastUsedWeapon, "set Anti-Aircraft defense");
                ResetSelectedWeapon();
                return true;
            }

            return false;
        }

        void ApplyWeaponCostOrReward (int index)
        {
            if (lastUsedWeapon == WeaponType.NormalShot)
            {
                ApplyNormalShotReward(index);
                return;
            }

            ApplyWeaponCost(index, lastUsedWeapon, $"used {lastUsedWeapon}");
        }

        bool CheckGameOverAfterSinks (GridManager grid, bool player1Attacking)
        {
            CheckSunkShips(grid);
            if (!grid.AllShipsSunk())
            {
                return false;
            }

            EndGame(player1Won: player1Attacking);
            return true;
        }

        void HandleTurnAfterAttack (CellAttackResult attackResult)
        {
            if (!attackResult.WasHit)
            {
                SwitchTurn();
                return;
            }

            Debug.Log($"[BattleSceneLogic] Bonus turn for {currentTurn}!");
        }
    }

    /// <summary>
    /// Special callback handler for bot attack results.
    /// Handles bot-specific logic like adding neighbors to target list on hit.
    /// This replaces the old OnBotFinishedTurn method.
    /// </summary>
    private void HandleBotAttackResult (CellAttackResult result, int commandIndex)
    {
        lastExecutedIndex = commandIndex;
        ApplyBotCpCostOrReward();

        if (CheckBotGameOver())
        {
            return;
        }

        HandleBotTurnAfterAttack(result);

        void ApplyBotCpCostOrReward ()
        {
            if (lastUsedWeapon == WeaponType.NormalShot)
            {
                ApplyNormalShotReward(2);
                return;
            }

            ApplyWeaponCost(2, lastUsedWeapon, $"Bot used {lastUsedWeapon}");
        }

        bool CheckBotGameOver ()
        {
            CheckSunkShips(player1Grid);
            if (!player1Grid.AllShipsSunk())
            {
                return false;
            }

            EndGame(player1Won: false);
            return true;
        }

        void HandleBotTurnAfterAttack (CellAttackResult attackResult)
        {
            if (attackResult.WasHit)
            {
                botController?.AddNeighborsToTargets(attackResult.Position);
                botController?.MakeTurn();
                return;
            }

            SwitchTurn();
        }
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

                // Notify bot to remove this ship from TrackShip tracking if it was being hunted
                if (gameMode == GameMode.PlayWithBot && botController != null)
                {
                    botController.RemoveTrackedShip(shipInstance.shipId);
                    botController.RemoveTargetPositions(shipInstance.occupiedCells);
                }

                grid.GetGridView().SyncFromBoard();
            }
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
    }

    private void ApplyWeaponCost (int playerIndex, WeaponType weaponType, string actionLabel)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[BattleSceneLogic] GameManager not found; cannot apply CP cost.");
            return;
        }

        int cpCost = GetWeaponCPCost(weaponType);
        if (cpCost <= 0)
        {
            return;
        }

        gameManager.SubtractCP(playerIndex, cpCost);
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