using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject chứa danh sách tất cả vũ khí có sẵn trong game
/// </summary>
[CreateAssetMenu(fileName = "Weapon List", menuName = "BattleShip/Weapon List")]
public class WeaponListData : ScriptableObject
{
    [Header("Available Weapons")]
    public List<WeaponData> weapons = new List<WeaponData>();

    /// <summary>
    /// Lấy weapon theo loại vũ khí
    /// </summary>
    public WeaponData GetWeaponByType (WeaponType type)
    {
        return weapons.Find(w => w.type == type);
    }

    /// <summary>
    /// Lấy weapon theo tên
    /// </summary>
    public WeaponData GetWeaponByName (string name)
    {
        return weapons.Find(w => w.weaponName == name);
    }
}
