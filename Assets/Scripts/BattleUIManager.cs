using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý tất cả UI trong BattleScene.
/// Tách riêng khỏi logic game.
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    private const string TurnYour = "Your turn";
    private const string TurnBot = "Bot is thinking...";
    private const string TurnPlayer1 = "Player 1's turn";
    private const string TurnPlayer2 = "Player 2's turn";
    private const string PassAndPlayPrompt = "Give the device to {0}.\nAre you ready?";
    private const string WinnerYou = "You win! 🎉";
    private const string WinnerBot = "Bot wins! 🤖";
    private const string WinnerPlayer1 = "Player 1 wins! 🎉";
    private const string WinnerPlayer2 = "Player 2 wins! 🎉";

    [Header("Turn UI")]
    public TextMeshProUGUI turnText;

    [Header("Pass & Play Panel")]
    public GameObject passAndPlayPanel;
    public TextMeshProUGUI passAndPlayText;
    public Button passAndPlayReadyButton;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button returnMenuButton;

    private void Awake ()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start ()
    {
        // Ẩn tất cả panels ban đầu
        if (passAndPlayPanel != null)
        {
            passAndPlayPanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Subscribe to BattleSceneLogic events
        if (BattleSceneLogic.Instance != null)
        {
            BattleSceneLogic.Instance.onTurnChanged += UpdateTurnUI;
            BattleSceneLogic.Instance.onPassAndPlayNeeded += ShowPassAndPlayScreen;
            BattleSceneLogic.Instance.onGameOver += ShowGameOverPanel;
        }

        SetupButtons();
    }

    private void OnDestroy ()
    {
        if (BattleSceneLogic.Instance != null)
        {
            BattleSceneLogic.Instance.onTurnChanged -= UpdateTurnUI;
            BattleSceneLogic.Instance.onPassAndPlayNeeded -= ShowPassAndPlayScreen;
            BattleSceneLogic.Instance.onGameOver -= ShowGameOverPanel;
        }
    }

    private void SetupButtons ()
    {
        if (passAndPlayReadyButton != null)
        {
            passAndPlayReadyButton.onClick.RemoveAllListeners();
            passAndPlayReadyButton.onClick.AddListener(OnPassAndPlayReady);
        }

        if (returnMenuButton != null)
        {
            returnMenuButton.onClick.RemoveAllListeners();
            returnMenuButton.onClick.AddListener(OnReturnMenuClicked);
        }
    }

    // ── Turn UI ───────────────────────────────────────────────

    public void UpdateTurnUI (Turn currentTurn, GameMode gameMode)
    {
        if (turnText == null)
        {
            return;
        }

        if (gameMode == GameMode.PlayWithBot)
        {
            turnText.text = currentTurn == Turn.Player1 ? TurnYour : TurnBot;
        }
        else
        {
            turnText.text = currentTurn == Turn.Player1 ? TurnPlayer1 : TurnPlayer2;
        }
    }

    // ── Pass & Play UI ────────────────────────────────────────

    public void ShowPassAndPlayScreen (Turn nextTurn)
    {
        if (passAndPlayPanel == null)
        {
            return;
        }

        passAndPlayPanel.SetActive(true);

        var nextPlayer = nextTurn == Turn.Player2 ? "Player 2" : "Player 1";
        if (passAndPlayText != null)
        {
            passAndPlayText.text = string.Format(PassAndPlayPrompt, nextPlayer);
        }
    }

    public void HidePassAndPlayScreen ()
    {
        if (passAndPlayPanel != null)
        {
            passAndPlayPanel.SetActive(false);
        }
    }

    private void OnPassAndPlayReady ()
    {
        HidePassAndPlayScreen();
        BattleSceneLogic.Instance?.OnPassAndPlayReady();
    }

    // ── Game Over UI ──────────────────────────────────────────

    public void ShowGameOverPanel (bool player1Won, GameMode gameMode)
    {
        var winnerText = gameMode == GameMode.PlayWithBot
            ? player1Won ? WinnerYou : WinnerBot
            : player1Won ? WinnerPlayer1 : WinnerPlayer2;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = winnerText;
        }
    }

    private void OnReturnMenuClicked ()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
