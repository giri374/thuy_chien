using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton tồn tại xuyên suốt toàn bộ game (DontDestroyOnLoad).
/// Lưu trữ GameMode, currentSetupPlayer, và ship placement data của cả hai người chơi.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Game Mode ─────────────────────────────────────────────

    public GameMode gameMode { get; private set; } = GameMode.PlayWithBot;

    /// <summary>
    /// Player đang trong quá trình Setup (1 hoặc 2).
    /// SetupSceneManager đọc giá trị này để biết đang setup cho ai.
    /// </summary>
    public int currentSetupPlayer { get; private set; } = 1;

    // ── Ship Data ─────────────────────────────────────────────

    [Header("Ship Data")]
    public ShipListData shipListData;

    // ── Placement Data ────────────────────────────────────────

    public static List<ShipPlacementData> player1Placements = new List<ShipPlacementData>();
    public static List<ShipPlacementData> player2Placements = new List<ShipPlacementData>();

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Mode API ──────────────────────────────────────────────

    public void SetGameMode(GameMode mode)
    {
        gameMode = mode;
        currentSetupPlayer = 1;
        Debug.Log($"[GameManager] GameMode = {mode}");
    }

    public void SetCurrentSetupPlayer(int playerIndex)
    {
        currentSetupPlayer = playerIndex;
        Debug.Log($"[GameManager] currentSetupPlayer = {playerIndex}");
    }

    // ── Placement API ─────────────────────────────────────────

    public void SavePlacement(int playerIndex, Ship ship)
    {
        var list = GetPlacements(playerIndex);
        list.RemoveAll(d => d.shipID == ship.shipData.shipID);
        list.Add(new ShipPlacementData
        {
            shipID = ship.shipData.shipID,
            position = ship.gridPosition,
            isHorizontal = ship.isHorizontal
        });
        Debug.Log($"[GameManager] Player {playerIndex} saved ship {ship.shipData.shipName} at {ship.gridPosition}");
    }

    public void ClearPlacements(int playerIndex)
    {
        GetPlacements(playerIndex).Clear();
    }

    public void ClearAllPlacements()
    {
        player1Placements.Clear();
        player2Placements.Clear();
    }

    public List<ShipPlacementData> GetPlacements(int playerIndex)
    {
        return playerIndex == 1 ? player1Placements : player2Placements;
    }

    // ── Nested Data Class ─────────────────────────────────────

    [System.Serializable]
    public class ShipPlacementData
    {
        public int shipID;
        public Vector2Int position;
        public bool isHorizontal;
    }
}
