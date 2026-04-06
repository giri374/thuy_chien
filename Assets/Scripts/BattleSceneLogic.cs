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

    [Header("Extracted Services")]
    private CommandExecutionCoordinator commandCoordinator;
    private AttackResolutionService attackResolutionService;
    private OnlineBattleRelay onlineBattleRelay;

    public GameState currentState = GameState.Playing;
    public Turn currentTurn = Turn.Player1;

    private GameMode gameMode => GameManager.Instance != null
        ? GameManager.Instance.gameMode
        : GameMode.PlayWithBot;

    public delegate void TurnChangedHandler (Turn currentTurn, GameMode gameMode);
    public delegate void PassAndPlayNeededHandler (Turn nextTurn);
    public delegate void GameOverHandler (bool player1Won, GameMode gameMode);

    public event TurnChangedHandler onTurnChanged;
    public event PassAndPlayNeededHandler onPassAndPlayNeeded;
    public event GameOverHandler onGameOver;

    private void Awake ()
    {
        Instance = this;
        attackResolutionService = new AttackResolutionService();
    }

    private void Start ()
    {
        SetupBotIfNeeded();
        LoadAllShips();

        commandCoordinator = new CommandExecutionCoordinator(this, player1Grid, player2Grid);
        commandCoordinator.OnCommandExecuted += HandleCommandExecuted;

        onlineBattleRelay = new OnlineBattleRelay();
        onlineBattleRelay.OnCommandReceived += ExecuteReceivedCommand;

        currentTurn = Turn.Player1;
        onTurnChanged?.Invoke(currentTurn, gameMode);

        if (gameMode == GameMode.PlayOnline)
        {
            onlineBattleRelay.Subscribe();
        }

        Debug.Log($"[BattleSceneManager] Game started | Mode: {gameMode}");
    }

    private void OnDestroy ()
    {
        if (commandCoordinator != null)
        {
            commandCoordinator.OnCommandExecuted -= HandleCommandExecuted;
        }

        if (onlineBattleRelay != null)
        {
            onlineBattleRelay.OnCommandReceived -= ExecuteReceivedCommand;
            onlineBattleRelay.Unsubscribe();
        }
    }

    private void ExecuteReceivedCommand (Assets.OnlineMode.GameMatch.CommandData data)
    {
        Debug.Log($"[BattleSceneLogic] ExecuteReceivedCommand: index={data.CommandIndex} weapon={data.WeaponType} pos=({data.X},{data.Y}) attacker={data.Attacker}");
        if (commandCoordinator == null)
        {
            Debug.LogWarning("[BattleSceneLogic] Command coordinator is null — cannot execute received command.");
            return;
        }

        if (commandCoordinator.HasAlreadyExecuted(data.CommandIndex))
        {
            Debug.LogWarning($"[BattleSceneLogic] Skipping duplicate command index={data.CommandIndex}");
            return;
        }

        var weapon = (WeaponType) data.WeaponType;
        var pos = new Vector2Int(data.X, data.Y);
        var attacker = (Turn) data.Attacker;

        commandCoordinator.ExecuteNetworkCommand(weapon, pos, attacker, data.CommandIndex);

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
            if (onlineBattleRelay == null || !onlineBattleRelay.IsLocalPlayer2())
            {
                return;
            }

            onlineBattleRelay.TrySendAttack(cell, isPlayer1Attacking: false, currentState, GetSelectedWeapon());
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
            if (onlineBattleRelay == null || !onlineBattleRelay.IsLocalPlayer1())
            {
                return;
            }

            onlineBattleRelay.TrySendAttack(cell, isPlayer1Attacking: true, currentState, GetSelectedWeapon());
            return;
        }

        HandleAttack(cell, isPlayer1Attacking: true);
    }

    private void HandleAttack (Cell cell, bool isPlayer1Attacking)
    {
        if (cell == null)
        {
            return;
        }

        if (currentState != GameState.Playing || cell.cellState != CellState.Unknown)
        {
            return;
        }

        if (commandCoordinator == null)
        {
            Debug.LogWarning("[BattleSceneLogic] Command coordinator is null — cannot execute attack.");
            return;
        }

        Turn attacker = isPlayer1Attacking ? Turn.Player1 : Turn.Player2;
        commandCoordinator.ExecutePlayerAttack(GetSelectedWeapon(), cell.gridPosition, attacker);
    }

    private WeaponType GetSelectedWeapon ()
    {
        var weaponManager = BattleWeaponManager.Instance;
        return weaponManager != null ? weaponManager.GetCurrentWeapon() : WeaponType.NormalShot;
    }

    private void HandleCommandExecuted (CellAttackResult result, int commandIndex, WeaponType weaponType, Turn attacker, bool isBotAttack)
    {
        if (isBotAttack)
        {
            HandleBotAttackResult(result, weaponType);
            return;
        }

        HandleAttackResult(result, weaponType);
    }

    /// <summary>
    /// Public method for BotController to execute an attack command.
    /// This is called from BotController.ProcessTurn() after selecting a target position.
    /// The bot-specific logic (adding neighbors on hit) is handled here before executing the command.
    /// </summary>
    public void ExecuteBotAttackCommand (WeaponType weaponType, Vector2Int position)
    {
        if (commandCoordinator == null)
        {
            Debug.LogWarning("[BattleSceneLogic] Command coordinator is null — cannot execute bot attack.");
            return;
        }

        commandCoordinator.ExecuteBotAttack(weaponType, position);
    }

    /// <summary>
    /// Callback handler for attack command results.
    /// This is called after a command executes and handles the game logic response
    /// (checking for sinks, switching turns, etc.)
    /// </summary>
    private void HandleAttackResult (CellAttackResult result, WeaponType weaponType)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        if (attackResolutionService == null)
        {
            Debug.LogWarning("[BattleSceneLogic] Attack resolution service is null.");
            return;
        }

        attackResolutionService.HandlePlayerAttackResult(
            result,
            currentTurn,
            player1Grid,
            player2Grid,
            weaponType,
            gameMode,
            botController,
            SwitchTurn,
            EndGame);
    }

    /// <summary>
    /// Special callback handler for bot attack results.
    /// Handles bot-specific logic like adding neighbors to target list on hit.
    /// This replaces the old OnBotFinishedTurn method.
    /// </summary>
    private void HandleBotAttackResult (CellAttackResult result, WeaponType weaponType)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        if (attackResolutionService == null)
        {
            Debug.LogWarning("[BattleSceneLogic] Attack resolution service is null.");
            return;
        }

        attackResolutionService.HandleBotAttackResult(
            result,
            player1Grid,
            weaponType,
            gameMode,
            botController,
            SwitchTurn,
            EndGame);
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
        else if (gameMode == GameMode.PlayOnline)
        {
            currentTurn = (currentTurn == Turn.Player1) ? Turn.Player2 : Turn.Player1;
            onTurnChanged?.Invoke(currentTurn, gameMode);
        }
        else
        {
            currentTurn = (currentTurn == Turn.Player1) ? Turn.Player2 : Turn.Player1;

            onPassAndPlayNeeded?.Invoke(currentTurn);
        }

        // Remove empty cells if enabled in GameManager
        if (GameManager.Instance != null && GameManager.Instance.removeEmptyCells)
        {
            player1Grid?.RemoveEmptyCells();
            player2Grid?.RemoveEmptyCells();
        }
    }

    public void OnPassAndPlayReady ()
    {
        onTurnChanged?.Invoke(currentTurn, gameMode);
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