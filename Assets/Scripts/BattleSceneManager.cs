using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý toàn bộ logic BattleScene.
/// Hỗ trợ cả PlayWithBot (Player vs AI) và PlayWithFriend (Player 1 vs Player 2).
/// </summary>
public class BattleSceneManager : MonoBehaviour
{
    public static BattleSceneManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────

    [Header("Grid References")]
    public GridManager player1Grid;   // Lưới của Player 1
    public GridManager player2Grid;   // Lưới của Player 2 (hoặc Bot)

    [Header("Bot (chỉ dùng khi PlayWithBot)")]
    public BotController botController;

    [Header("UI")]
    public TextMeshProUGUI turnText;          // "Lượt của Player 1 / Player 2 / Bot"
    public GameObject passAndPlayPanel;       // Panel "Pass & Play" khi PlayWithFriend
    public TextMeshProUGUI passAndPlayText;   // "Player 2, đã sẵn sàng chưa?"
    public Button passAndPlayReadyButton;     // Nút "Sẵn sàng" trên panel
    public GameObject gameOverPanel;          // Panel game over
    public TextMeshProUGUI gameOverText;      // "Player 1 Thắng!" / "Bot Thắng!"
    public Button returnMenuButton;

    // ── State ─────────────────────────────────────────────────

    public GameState currentState = GameState.Playing;
    public Turn currentTurn = Turn.Player1;

    private GameMode gameMode => GameManager.Instance != null
        ? GameManager.Instance.gameMode
        : GameMode.PlayWithBot;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Ẩn panels
        if (passAndPlayPanel != null) passAndPlayPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        SetupBotIfNeeded();
        LoadAllShips();

        currentTurn = Turn.Player1;
        UpdateTurnUI();

        Debug.Log($"[BattleSceneManager] Game started | Mode: {gameMode}");
    }

    // ── Setup ─────────────────────────────────────────────────

    private void SetupBotIfNeeded()
    {
        if (gameMode != GameMode.PlayWithBot) return;

        if (botController == null)
        {
            botController = GetComponent<BotController>()
                         ?? gameObject.AddComponent<BotController>();
        }

        botController.myGrid = player2Grid;
        botController.targetGrid = player1Grid;

        if (GameManager.Instance != null)
            botController.shipListData = GameManager.Instance.shipListData;
    }

    private void LoadAllShips()
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

    private void LoadShipsFromData(List<GameManager.ShipPlacementData> placements, GridManager grid)
    {
        if (placements == null || grid == null) return;

        var shipListData = GameManager.Instance?.shipListData;
        if (shipListData == null)
        {
            Debug.LogWarning("[BattleSceneManager] shipListData is null!");
            return;
        }

        foreach (var data in placements)
        {
            ShipData shipData = shipListData.GetShipByID(data.shipID);
            if (shipData?.shipPrefab == null) continue;

            GameObject obj = Instantiate(shipData.shipPrefab, grid.transform);
            Ship ship = obj.GetComponent<Ship>();
            if (ship == null) continue;

            ship.Initialize(shipData);

            if (!data.isHorizontal) ship.Rotate();

            grid.PlaceShip(ship, data.position);
        }
    }

    // ── Cell Click Callbacks (gọi từ GridManager) ────────────

    public void OnPlayer1GridCellClicked(Cell cell)
    {
        // PlayWithFriend: Player 2 tấn công lưới Player 1
        if (gameMode == GameMode.PlayWithFriend && currentTurn == Turn.Player2)
        {
            HandleAttack(cell, player1Grid, isPlayer1Attacking: false);
        }
        // PlayWithBot: Bot tự động, không cho click vào lưới Player 1
    }

    public void OnPlayer2GridCellClicked(Cell cell)
    {
        // Player 1 luôn tấn công lưới Player 2 (cả 2 mode)
        if (currentTurn == Turn.Player1)
        {
            HandleAttack(cell, player2Grid, isPlayer1Attacking: true);
        }
    }

    // ── Attack Logic ──────────────────────────────────────────

    private void HandleAttack(Cell cell, GridManager targetGrid, bool isPlayer1Attacking)
    {
        if (currentState != GameState.Playing) return;
        if (cell.cellState != CellState.Unknown) return;

        bool hit = targetGrid.AttackCell(cell.gridPosition);
        string attacker = isPlayer1Attacking ? "Player 1" : "Player 2";
        Debug.Log($"[BattleSceneManager] {attacker} attacks {cell.gridPosition}: {(hit ? "HIT" : "MISS")}");

        CheckSunkShips(targetGrid);

        if (targetGrid.AllShipsSunk())
        {
            EndGame(player1Won: isPlayer1Attacking);
            return;
        }

        if (!hit)
            SwitchTurn();
        else
            Debug.Log("Bonus turn!");
    }

    // ── Bot Callback ──────────────────────────────────────────

    /// <summary>
    /// BotController gọi hàm này sau khi bot bắn xong
    /// </summary>
    public void OnBotFinishedTurn(bool hit)
    {
        CheckSunkShips(player1Grid);

        if (player1Grid.AllShipsSunk())
        {
            EndGame(player1Won: false);
            return;
        }

        if (hit)
            botController.MakeTurn(); // Bonus turn cho bot
        else
            SwitchTurn();
    }

    // ── Turn Management ───────────────────────────────────────

    private void SwitchTurn()
    {
        if (gameMode == GameMode.PlayWithBot)
        {
            currentTurn = (currentTurn == Turn.Player1) ? Turn.Player2 : Turn.Player1;

            if (currentTurn == Turn.Player2)
            {
                UpdateTurnUI();
                botController?.MakeTurn();
            }
            else
            {
                UpdateTurnUI();
            }
        }
        else // PlayWithFriend
        {
            if (currentTurn == Turn.Player1)
            {
                // Hiện Pass & Play screen trước khi Player 2 hành động
                currentTurn = Turn.Player2;
                ShowPassAndPlayScreen();
            }
            else
            {
                currentTurn = Turn.Player1;
                ShowPassAndPlayScreen();
            }
        }
    }

    // ── Pass & Play (PlayWithFriend) ──────────────────────────

    private void ShowPassAndPlayScreen()
    {
        if (passAndPlayPanel == null)
        {
            // Không có UI → chuyển thẳng
            UpdateTurnUI();
            return;
        }

        passAndPlayPanel.SetActive(true);

        string nextPlayer = currentTurn == Turn.Player2 ? "Player 2" : "Player 1";
        if (passAndPlayText != null)
            passAndPlayText.text = $"Give the device to {nextPlayer}.\nAre you ready?";

        if (passAndPlayReadyButton != null)
        {
            passAndPlayReadyButton.onClick.RemoveAllListeners();
            passAndPlayReadyButton.onClick.AddListener(OnPassAndPlayReady);
        }
    }

    public void OnPassAndPlayReady()
    {
        if (passAndPlayPanel != null) passAndPlayPanel.SetActive(false);
        UpdateTurnUI();
    }

    // ── UI ────────────────────────────────────────────────────

    private void UpdateTurnUI()
    {
        if (turnText == null) return;

        if (gameMode == GameMode.PlayWithBot)
        {
            turnText.text = currentTurn == Turn.Player1 ? "Your turn" : "Bot is thinking...";
        }
        else
        {
            turnText.text = currentTurn == Turn.Player1 ? "Player 1's turn" : "Player 2's turn";
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private void CheckSunkShips(GridManager grid)
    {
        foreach (Ship ship in grid.ships)
        {
            if (ship != null && ship.IsSunk())
                grid.MarkAdjacentCellsEmpty(ship);
        }
    }

    private void EndGame(bool player1Won)
    {
        currentState = GameState.GameOver;

        string winnerText;
        if (gameMode == GameMode.PlayWithBot)
            winnerText = player1Won ? "You win! 🎉" : "Bot wins! 🤖";
        else
            winnerText = player1Won ? "Player 1 wins! 🎉" : "Player 2 wins! 🎉";

        Debug.Log($"[BattleSceneManager] Game Over — {winnerText}");

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null) gameOverText.text = winnerText;

        if (returnMenuButton != null)
        {
            returnMenuButton.onClick.RemoveAllListeners();
            returnMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MenuScene"));
        }
    }
}