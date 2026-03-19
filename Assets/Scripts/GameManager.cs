using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameMode gameMode { get; private set; } = GameMode.PlayWithBot;
    public GameMap gameMap { get; private set; } = GameMap.NormalMap;
    public int currentSetupPlayer { get; private set; } = 1;

    [Header("Ship Data")]
    public ShipListData shipListData;

    public static List<ShipPlacementData> player1Placements = new List<ShipPlacementData>();
    public static List<ShipPlacementData> player2Placements = new List<ShipPlacementData>();

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

    public void SetGameMode(GameMode mode)
    {
        gameMode = mode;
        currentSetupPlayer = 1;
        Debug.Log($"[GameManager] GameMode = {mode}");
    }

    public void SetGameMap(GameMap map)
    {
        gameMap = map;
        Debug.Log($"[GameManager] GameMap = {map}");
    }

    public void SetCurrentSetupPlayer(int playerIndex)
    {
        currentSetupPlayer = playerIndex;
        Debug.Log($"[GameManager] currentSetupPlayer = {playerIndex}");
    }

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

    [System.Serializable]
    public class ShipPlacementData
    {
        public int shipID;
        public Vector2Int position;
        public bool isHorizontal;
    }
}
