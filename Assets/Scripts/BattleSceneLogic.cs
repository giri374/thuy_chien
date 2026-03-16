using System.Collections.Generic;
using UnityEngine;
public enum Turn
{
    Player1,    // Lượt người chơi 1 (hoặc người chơi duy nhất khi vs Bot)
    Player2   // Lượt người chơi 2 (hoặc Bot)
}

public enum GameState
{
    Setup,      // SetupScene - đang sắp xếp tàu
    Playing,    // BattleScene - đang chơi
    GameOver    // Kết thúc
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

/// <summary>
/// Quản lý toàn bộ logic game trong BattleScene.
/// Hỗ trợ cả PlayWithBot (Player vs AI) và PlayWithFriend (Player 1 vs Player 2).
/// UI được tách riêng thành BattleUIManager.
/// </summary>
public class BattleSceneLogic : MonoBehaviour
{
    public static BattleSceneLogic Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────

    [Header("Grid References")]
    public GridManager player1Grid;   // Lưới của Player 1
    public GridManager player2Grid;   // Lưới của Player 2 (hoặc Bot)

    [Header("Bot (chỉ dùng khi PlayWithBot)")]
    public BotController botController;

    // ── State ─────────────────────────────────────────────────

    public GameState currentState = GameState.Playing;
    public Turn currentTurn = Turn.Player1;

    private GameMode gameMode => GameManager.Instance != null
        ? GameManager.Instance.gameMode
        : GameMode.PlayWithBot;

    // ── Events ────────────────────────────────────────────────

    public delegate void TurnChangedHandler (Turn currentTurn, GameMode gameMode);
    public delegate void PassAndPlayNeededHandler (Turn nextTurn);
    public delegate void GameOverHandler (bool player1Won, GameMode gameMode);

    public event TurnChangedHandler onTurnChanged;
    public event PassAndPlayNeededHandler onPassAndPlayNeeded;
    public event GameOverHandler onGameOver;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake ()
    {
        Instance = this;
    }

    private void Start ()
    {
        SetupBotIfNeeded();
        LoadAllShips();

        currentTurn = Turn.Player1;
        onTurnChanged?.Invoke(currentTurn, gameMode);

        Debug.Log($"[BattleSceneManager] Game started | Mode: {gameMode}");
    }

    // ── Setup ─────────────────────────────────────────────────

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
            // PlayWithFriend: load Player 2 từ GameManager
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

    // ── Cell Click Callbacks (gọi từ GridManager) ────────────

    public void OnPlayer1GridCellClicked (Cell cell)
    {
        // PlayWithFriend: Player 2 tấn công lưới Player 1
        if (gameMode == GameMode.PlayWithFriend && currentTurn == Turn.Player2)
        {
            HandleAttack(cell, player1Grid, isPlayer1Attacking: false);
        }
        // PlayWithBot: Bot tự động, không cho click vào lưới Player 1
    }

    public void OnPlayer2GridCellClicked (Cell cell)
    {
        // Player 1 luôn tấn công lưới Player 2 (cả 2 mode)
        if (currentTurn == Turn.Player1)
        {
            HandleAttack(cell, player2Grid, isPlayer1Attacking: true);
        }
    }

    // ── Attack Logic ──────────────────────────────────────────

    private void HandleAttack (Cell cell, GridManager targetGrid, bool isPlayer1Attacking)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        if (cell.cellState != CellState.Unknown)
        {
            return;
        }

        var hit = targetGrid.AttackCell(cell.gridPosition);
        var attacker = isPlayer1Attacking ? "Player 1" : "Player 2";
        Debug.Log($"[BattleSceneManager] {attacker} attacks {cell.gridPosition}: {(hit ? "HIT" : "MISS")}");

        CheckSunkShips(targetGrid);

        if (targetGrid.AllShipsSunk())
        {
            EndGame(player1Won: isPlayer1Attacking);
            return;
        }

        if (!hit)
        {
            SwitchTurn();
        }
        else
        {
            Debug.Log("Bonus turn!");
        }
    }

    // ── Bot Callback ──────────────────────────────────────────

    /// <summary>
    /// BotController gọi hàm này sau khi bot bắn xong
    /// </summary>
    public void OnBotFinishedTurn (bool hit)
    {
        CheckSunkShips(player1Grid);

        if (player1Grid.AllShipsSunk())
        {
            EndGame(player1Won: false);
            return;
        }

        if (hit)
        {
            botController.MakeTurn(); // Bonus turn cho bot
        }
        else
        {
            SwitchTurn();
        }
    }

    // ── Turn Management ───────────────────────────────────────

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
        else // PlayWithFriend
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

    // ── Pass & Play Callback (PlayWithFriend) ────────────────

    public void OnPassAndPlayReady ()
    {
        onTurnChanged?.Invoke(currentTurn, gameMode);
    }

    // ── Helpers ───────────────────────────────────────────────

    private void CheckSunkShips (GridManager grid)
    {
        foreach (var ship in grid.ships)
        {
            if (ship != null && ship.IsSunk())
            {
                grid.MarkAdjacentCellsEmpty(ship);
            }
        }
    }

    // ── Game Over Handler ────────────────────────────────────

    private async void EndGame (bool player1Won)
    {
        currentState = GameState.GameOver;

        Debug.Log($"[BattleSceneManager] Game Over — {(player1Won ? "Player 1 won" : "Player 2/Bot won")}");

        // ── Reward EXP & Gold ──────────────────────────────────────
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

        // ── Notify UI ──────────────────────────────────────────────
        onGameOver?.Invoke(player1Won, gameMode);
    }
}