using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupSceneUIManager : MonoBehaviour
{
    public static SetupSceneUIManager Instance { get; private set; }

    [Header("UI")]
    public Button confirmButton;
    public Button resetButton;
    public Button randomButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("Pass & Play UI")]
    public GameObject passDevicePanel;
    public Button passDeviceReadyButton;

    private SetupSceneLogic logic;

    private void Awake ()
    {
        Instance = this;
    }

    private void Start ()
    {
        logic = SetupSceneLogic.Instance;

        var currentPlayer = GameManager.Instance?.currentSetupPlayer ?? 1;
        logic.Initialize(currentPlayer);

        UpdateUI();
        RegisterButtonListeners();
    }

    // ── UI Updates ────────────────────────────────────────────

    private void UpdateUI ()
    {
        var player = logic.GetCurrentPlayer();

        if (titleText != null)
        {
            titleText.text = $"Player {player}";
        }

        if (instructionText != null)
        {
            instructionText.text = "Drag and rotate your ships to place them on the grid. The ship will turn green if the position is valid.";
        }
    }

    private void RegisterButtonListeners ()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }

        if (randomButton != null)
        {
            randomButton.onClick.AddListener(OnRandomClicked);
        }

        if (passDeviceReadyButton != null)
        {
            passDeviceReadyButton.onClick.AddListener(OnPassDeviceReady);
        }
    }

    // ── Button Handlers ────────────────────────────────────────

    private void OnConfirmClicked ()
    {
        if (!logic.CanConfirm())
        {
            Debug.LogWarning("[SetupSceneManager] Chưa đặt tàu nào!");
            // TODO: Hiển thị thông báo UI
            return;
        }

        var action = logic.GetNextAction();

        if (action == GameFlowAction.ShowPassDevice)
        {
            ShowPassDeviceScreen();
        }
        else if (action == GameFlowAction.GoToOnlineConnection)
        {
            GoToOnlineConnection();
        }
        else
        {
            GoToBattle();
        }
    }

    private void OnResetClicked ()
    {
        logic.ResetAllPlacements();
    }

    private void OnRandomClicked ()
    {
        var success = logic.RandomPlaceAllShips();
        if (!success)
        {
            // TODO: Hiển thị thông báo lỗi
        }
    }

    // ── Pass & Play UI ────────────────────────────────────────

    private void ShowPassDeviceScreen ()
    {
        if (passDevicePanel != null)
        {
            passDevicePanel.SetActive(true);
        }
        else
        {
            OnPassDeviceReady();
        }
    }

    public void OnPassDeviceReady ()
    {
        GameManager.Instance?.SetCurrentSetupPlayer(2);
        SceneManager.LoadScene(SceneNames.Setup);
    }

    // ── Scene Transition ──────────────────────────────────────

    private void GoToBattle ()
    {
        SceneManager.LoadScene(SceneNames.Battle);
    }

    private void GoToOnlineConnection ()
    {
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsServer && networkManager.SceneManager != null)
        {
            networkManager.SceneManager.LoadScene(SceneNames.OnlineConnection, LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(SceneNames.OnlineConnection);
    }

}