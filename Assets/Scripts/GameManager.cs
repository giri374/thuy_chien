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

    [Header("Weapon Data")]
    public WeaponListData weaponListData;

    public static List<ShipPlacementData> player1Placements = new List<ShipPlacementData>();
    public static List<ShipPlacementData> player2Placements = new List<ShipPlacementData>();

    // ── Weapon & Gold Data ─────────────────────────────────────
    public static List<WeaponType> player1SelectedWeapons = new List<WeaponType>();
    public static List<WeaponType> player2SelectedWeapons = new List<WeaponType>();

    public static int player1Gold = 0;
    public static int player2Gold = 0;

    private void Awake ()
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

    public void SetGameMode (GameMode mode)
    {
        gameMode = mode;
        currentSetupPlayer = 1;
        Debug.Log($"[GameManager] GameMode = {mode}");
    }

    public void SetGameMap (GameMap map)
    {
        gameMap = map;
        Debug.Log($"[GameManager] GameMap = {map}");
    }

    public void SetCurrentSetupPlayer (int playerIndex)
    {
        currentSetupPlayer = playerIndex;
        Debug.Log($"[GameManager] currentSetupPlayer = {playerIndex}");
    }

    public void SavePlacement (int playerIndex, Ship ship)
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

    public void ClearPlacements (int playerIndex)
    {
        GetPlacements(playerIndex).Clear();
    }

    public void ClearAllPlacements ()
    {
        player1Placements.Clear();
        player2Placements.Clear();
    }

    public List<ShipPlacementData> GetPlacements (int playerIndex)
    {
        return playerIndex == 1 ? player1Placements : player2Placements;
    }

    // ── Weapon Management ──────────────────────────────────────

    /// <summary>
    /// Đặt gold hiện tại cho người chơi
    /// </summary>
    public void SetPlayerGold (int playerIndex, int goldAmount)
    {
        if (playerIndex == 1)
        {
            player1Gold = goldAmount;
        }
        else
        {
            player2Gold = goldAmount;
        }
        Debug.Log($"[GameManager] Player {playerIndex} gold set to {goldAmount}");
    }

    /// <summary>
    /// Lấy gold hiện tại của người chơi
    /// </summary>
    public int GetPlayerGold (int playerIndex)
    {
        return playerIndex == 1 ? player1Gold : player2Gold;
    }

    /// <summary>
    /// Thêm vũ khí vào danh sách của người chơi
    /// </summary>
    public void AddWeapon (int playerIndex, WeaponType weaponType)
    {
        var weaponList = GetSelectedWeapons(playerIndex);
        if (!weaponList.Contains(weaponType))
        {
            weaponList.Add(weaponType);
            Debug.Log($"[GameManager] Player {playerIndex} added weapon: {weaponType}");
        }
    }

    /// <summary>
    /// Xóa vũ khí khỏi danh sách của người chơi
    /// </summary>
    public void RemoveWeapon (int playerIndex, WeaponType weaponType)
    {
        var weaponList = GetSelectedWeapons(playerIndex);
        if (weaponList.Contains(weaponType))
        {
            weaponList.Remove(weaponType);
            Debug.Log($"[GameManager] Player {playerIndex} removed weapon: {weaponType}");
        }
    }

    /// <summary>
    /// Lấy danh sách vũ khí được chọn của người chơi
    /// </summary>
    public List<WeaponType> GetSelectedWeapons (int playerIndex)
    {
        return playerIndex == 1 ? player1SelectedWeapons : player2SelectedWeapons;
    }

    /// <summary>
    /// Xóa tất cả vũ khí đã chọn
    /// </summary>
    public void ClearAllWeapons ()
    {
        player1SelectedWeapons.Clear();
        player2SelectedWeapons.Clear();
    }

    /// <summary>
    /// Xóa vũ khí của người chơi cụ thể
    /// </summary>
    public void ClearWeapons (int playerIndex)
    {
        GetSelectedWeapons(playerIndex).Clear();
    }

    [System.Serializable]
    public class ShipPlacementData
    {
        public int shipID;
        public Vector2Int position;
        public bool isHorizontal;
    }
}
