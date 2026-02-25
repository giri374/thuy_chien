using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quản lý SetupScene cho từng người chơi.
/// Scene "SetupScene" được dùng chung cho cả Player 1 và Player 2.
/// GameManager.currentSetupPlayer cho biết đang setup cho ai.
/// </summary>
public class SetupSceneManager : MonoBehaviour
{
    public static SetupSceneManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────

    [Header("Grid")]
    public GridManager playerGrid;

    [Header("UI")]
    public Button confirmButton;       // Nút "Xác nhận / Sẵn sàng"
    public Button resetButton;         // Nút "Đặt lại"
    public TextMeshProUGUI titleText;  // "Player 1 - Đặt tàu" / "Player 2 - Đặt tàu"

    [Header("Pass & Play UI (chỉ hiện khi PlayWithFriend)")]
    public GameObject passDevicePanel;    // Panel "Hãy đưa màn hình cho Player 2"
    public Button passDeviceReadyButton;  // Nút "Sẵn sàng" trên panel đó

    [Header("Ships (kéo tất cả ShipPlacement vào đây)")]
    public ShipPlacement[] shipPlacements;

    private int currentPlayer = 1; // Player đang setup (1 hoặc 2)

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Xác định đang setup cho player nào
        currentPlayer = GameManager.Instance != null
            ? GameManager.Instance.currentSetupPlayer
            : 1;

        // Xóa data placement cũ của player này (cho phép đặt lại)
        GameManager.Instance?.ClearPlacements(currentPlayer);

        // Khởi tạo Grid
        if (playerGrid != null)
            playerGrid.Initialize(this);

        UpdateUI();

        // Gán nút
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);

        if (passDevicePanel != null) passDevicePanel.SetActive(false);
        if (passDeviceReadyButton != null) passDeviceReadyButton.onClick.AddListener(OnPassDeviceReady);
    }

    // ── UI ────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (titleText != null)
            titleText.text = $"Player {currentPlayer}";

    }

    // ── Ship Placement Callback (gọi từ ShipPlacement.cs) ────

    /// <summary>
    /// Lưu vị trí tàu của player hiện tại
    /// </summary>
    public void SaveShipPlacement(Ship ship)
    {
        GameManager.Instance?.SavePlacement(currentPlayer, ship);
    }

    // ── Cell Click (SetupScene không cần xử lý nhiều) ────────

    public void OnPlayerGridCellClicked(Cell cell)
    {
        Debug.Log($"[SetupSceneManager] Cell clicked: {cell.gridPosition}");
    }

    // ── Buttons ───────────────────────────────────────────────

    /// <summary>
    /// Nút "Xác nhận" - kiểm tra đã đặt đủ tàu chưa rồi chuyển scene
    /// </summary>
    public void OnConfirm()
    {
        var placements = GameManager.Instance?.GetPlacements(currentPlayer);

        if (placements == null || placements.Count == 0)
        {
            Debug.LogWarning("[SetupSceneManager] Chưa đặt tàu nào!");
            // TODO: Hiển thị thông báo UI
            return;
        }

        // Kiểm tra số lượng tàu (tuỳ chọn - so với shipListData)
        // int requiredCount = GameManager.Instance.shipListData?.ships.Count ?? 0;
        // if (placements.Count < requiredCount) { ... return; }

        ProceedToNextStep();
    }

    /// <summary>
    /// Nút "Đặt lại" — reset ô lưới và trả tất cả tàu về vị trí ban đầu.
    /// KHÔNG Destroy prefab tàu.
    /// </summary>
    public void OnReset()
    {
        // 1. Xóa placement data
        GameManager.Instance?.ClearPlacements(currentPlayer);

        // 2. Reset trạng thái các ô lưới (không Destroy ship)
        playerGrid?.ResetGridOnly();

        // 3. Trả từng tàu về vị trí gốc
        if (shipPlacements != null)
        {
            foreach (var sp in shipPlacements)
                sp?.ResetToOrigin();
        }

        Debug.Log($"[SetupSceneManager] Player {currentPlayer} reset.");
    }

    // ── Flow Control ──────────────────────────────────────────

    private void ProceedToNextStep()
    {
        bool isPlayWithFriend = GameManager.Instance?.gameMode == GameMode.PlayWithFriend;

        if (isPlayWithFriend && currentPlayer == 1)
        {
            // Hiện màn hình "Pass & Play" để che màn hình cho Player 2
            ShowPassDeviceScreen();
        }
        else
        {
            // PlayWithBot (player 1 xong) hoặc PlayWithFriend player 2 xong
            GoToBattle();
        }
    }

    /// <summary>
    /// Hiện panel "Đưa thiết bị cho Player 2" để tránh Player 2 thấy vị trí tàu Player 1
    /// </summary>
    private void ShowPassDeviceScreen()
    {
        if (passDevicePanel != null)
        {
            passDevicePanel.SetActive(true);
        }
        else
        {
            // Nếu không có panel, chuyển thẳng
            OnPassDeviceReady();
        }
    }

    /// <summary>
    /// Player 2 nhấn "Sẵn sàng" sau khi nhận thiết bị → Load SetupScene lần 2
    /// </summary>
    public void OnPassDeviceReady()
    {
        GameManager.Instance?.SetCurrentSetupPlayer(2);
        SceneManager.LoadScene("SetupScene"); // Load lại cùng scene cho Player 2
    }

    private void GoToBattle()
    {
        SceneManager.LoadScene("BattleScene");
    }
}