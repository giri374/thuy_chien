using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "BattleShip/Weapon")]
public class WeaponData : ScriptableObject
{
    public WeaponType type;
    public string weaponName;
    public int goldCost;
    public int cpCost;
    public GameObject effectPrefab;
    public Sprite icon;
    public string description;
}