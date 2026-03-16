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
            turnText.text = currentTurn == Turn.Player1 ? "Your turn" : "Bot is thinking...";
        }
        else
        {
            turnText.text = currentTurn == Turn.Player1 ? "Player 1's turn" : "Player 2's turn";
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
            passAndPlayText.text = $"Give the device to {nextPlayer}.\nAre you ready?";
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
            ? player1Won ? "You win! 🎉" : "Bot wins! 🤖"
            : player1Won ? "Player 1 wins! 🎉" : "Player 2 wins! 🎉";
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
