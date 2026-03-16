using UnityEngine;

/// <summary>
/// ScriptableObject chứa thông tin của tàu
/// </summary>
[CreateAssetMenu(fileName = "New Ship", menuName = "Battleship/Ship Data")]
public class ShipData : ScriptableObject
{
    [Header("Ship Information")]
    public string shipName;
    public int shipID;
    
    [Header("Ship Prefab")]
    public GameObject shipPrefab;
    
    [Header("Ship Size")]
    public Vector2Int size = Vector2Int.one;
    
    /// <summary>
    /// Tổng số ô mà tàu chiếm (dài x rộng)
    /// </summary>
    public int GetTotalCells()
    {
        return size.x * size.y;
    }
}
