using System.Collections.Generic;
using UnityEngine;

public class SetupSceneLogic : MonoBehaviour
{
    public static SetupSceneLogic Instance { get; private set; }

    private const int RandomPlacementMaxAttempts = 400;
    private const int RandomPlacementPasses = 2;

    [Header("Grid")]
    public GridManager playerGrid;

    [Header("Ships")]
    public ShipPlacement[] shipPlacements;

    private int currentPlayer = 1;

    private void Awake ()
    {
        Instance = this;
    }

    public void Initialize (int player)
    {
        if (player != 1 && player != 2)
        {
            Debug.LogError($"[SetupSceneLogic] Invalid player index: {player}");
            return;
        }

        currentPlayer = player;

        // Xóa data placement cũ
        GameManager.Instance?.ClearPlacements(currentPlayer);

        // Khởi tạo Grid
        if (playerGrid != null)
        {
            playerGrid.Initialize(null);
        }
    }

    // ── Ship Placement ────────────────────────────────────────

    public void SaveShipPlacement (Ship ship)
    {
        GameManager.Instance?.SavePlacement(currentPlayer, ship);
    }

    // ── Validation ────────────────────────────────────────────

    public bool CanConfirm ()
    {
        var placements = GameManager.Instance?.GetPlacements(currentPlayer);
        return placements != null && placements.Count == shipPlacements.Length;
    }

    // ── Flow Control ──────────────────────────────────────────

    public GameFlowAction GetNextAction ()
    {
        var mode = GameManager.Instance?.gameMode ?? GameMode.PlayWithBot;

        if (mode == GameMode.PlayWithFriend)
        {
            if (currentPlayer == 1)
            {
                return GameFlowAction.ShowPassDevice;  // Player 1 xong → PassDevice
            }
            else
            {
                return GameFlowAction.GoToBattle;      // Player 2 xong → Battle
            }
        }
        if (mode == GameMode.PlayOnline)
        {
            return GameFlowAction.GoToOnlineConnection;
        }
        else
        {
            return GameFlowAction.GoToBattle;
        }
    }

    public int GetCurrentPlayer () => currentPlayer;

    // ── Reset Logic ────────────────────────────────────────────

    public void ResetAllPlacements ()
    {
        GameManager.Instance?.ClearPlacements(currentPlayer);
        playerGrid?.ResetGridOnly();

        if (shipPlacements != null)
        {
            foreach (var sp in shipPlacements)
            {
                sp?.ResetToOrigin();
            }
        }

        Debug.Log($"[SetupSceneLogic] Player {currentPlayer} reset.");
    }

    // ── Random Placement ──────────────────────────────────────

    public bool RandomPlaceAllShips ()
    {
        if (shipPlacements == null || shipPlacements.Length == 0 || playerGrid == null)
        {
            Debug.LogWarning("[SetupSceneLogic] Missing shipPlacements or playerGrid.");
            return false;
        }

        ResetAllPlacements();

        var placedAll = false;
        for (var pass = 0; pass < RandomPlacementPasses && !placedAll; pass++)
        {
            placedAll = TryRandomPlaceAllShips();
            if (!placedAll)
            {
                ResetAllPlacements();
            }
        }

        if (!placedAll)
        {
            Debug.LogWarning("[SetupSceneLogic] Could not random place all ships.");
        }

        return placedAll;
    }

    private bool TryRandomPlaceAllShips ()
    {
        var toPlace = new List<ShipPlacement>();
        foreach (var sp in shipPlacements)
        {
            if (sp != null && sp.ship != null)
            {
                toPlace.Add(sp);
            }
        }

        Shuffle(toPlace);

        foreach (var sp in toPlace)
        {
            if (!TryRandomPlaceShip(sp))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryRandomPlaceShip (ShipPlacement shipPlacement)
    {
        for (var attempt = 0; attempt < RandomPlacementMaxAttempts; attempt++)
        {
            var horizontal = Random.value > 0.5f;
            var x = Random.Range(0, playerGrid.gridWidth);
            var y = Random.Range(0, playerGrid.gridHeight);
            var origin = new Vector2Int(x, y);

            if (shipPlacement.TryPlaceAt(origin, horizontal))
            {
                return true;
            }
        }

        return false;
    }

    private static void Shuffle<T> (IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

}

// Enum để UI biết cần làm gì
public enum GameFlowAction
{
    ShowPassDevice,
    GoToBattle,
    GoToOnlineConnection
}