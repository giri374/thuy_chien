using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject chứa danh sách tất cả các loại tàu trong game
/// </summary>
[CreateAssetMenu(fileName = "Ship List", menuName = "Battleship/Ship List")]
public class ShipListData : ScriptableObject
{
    [Header("Available Ships")]
    public List<ShipData> ships = new List<ShipData>();
    
    /// <summary>
    /// Lấy ship data theo ID
    /// </summary>
    public ShipData GetShipByID(int id)
    {
        return ships.Find(ship => ship.shipID == id);
    }
    
    /// <summary>
    /// Lấy ship data theo tên
    /// </summary>
    public ShipData GetShipByName(string name)
    {
        return ships.Find(ship => ship.shipName == name);
    }
}
