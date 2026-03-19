using UnityEngine;

/// <summary>
/// Factory for creating weapon instances.
/// Creates the correct weapon type with its corresponding strategy.
/// This centralizes weapon instantiation and initialization.
/// </summary>
public static class WeaponFactory
{
    /// <summary>
    /// Creates a weapon instance of the given type.
    /// Returns a configured weapon ready to execute an attack.
    /// </summary>
    /// <param name="weaponType">The type of weapon to create.</param>
    /// <returns>A fully initialized Weapon instance, or null if type is not recognized.</returns>
    public static Weapon CreateWeapon (WeaponType weaponType)
    {
        // Get weapon data from GameManager
        if (GameManager.Instance == null || GameManager.Instance.weaponListData == null)
        {
            Debug.LogError("[WeaponFactory] GameManager or weaponListData is null!");
            return null;
        }

        WeaponData weaponData = GameManager.Instance.weaponListData.GetWeaponByType(weaponType);
        if (weaponData == null)
        {
            Debug.LogError($"[WeaponFactory] No weapon data found for type: {weaponType}");
            return null;
        }

        // Create temporary GameObject to hold the weapon component
        GameObject weaponObj = new GameObject($"Weapon_{weaponType}");
        Weapon weapon = null;
        IWeaponStrategy strategy = null;

        // Instantiate the correct weapon and strategy based on type
        switch (weaponType)
        {
            case WeaponType.NormalShot:
                // NormalShot uses default weapon behavior (single cell attack)
                weapon = weaponObj.AddComponent<DefaultWeapon>();
                strategy = new SingleCellStrategy();
                break;

            case WeaponType.NuclearBomb:
                weapon = weaponObj.AddComponent<DefaultWeapon>();
                strategy = new NuclearBombStrategy();
                break;

            case WeaponType.Bomber:
                weapon = weaponObj.AddComponent<DefaultWeapon>();
                strategy = new BomberStrategy();
                break;

            case WeaponType.Torpedoes:
                weapon = weaponObj.AddComponent<DefaultWeapon>();
                strategy = new TorpedoesStrategy();
                break;

            case WeaponType.Radar:
                weapon = weaponObj.AddComponent<RadarWeapon>();
                strategy = new RadarStrategy();
                break;

            case WeaponType.AntiAircraft:
                weapon = weaponObj.AddComponent<AntiAircraftWeapon>();
                strategy = new AntiAircraftStrategy();
                break;

            default:
                Debug.LogError($"[WeaponFactory] Unknown weapon type: {weaponType}");
                Object.Destroy(weaponObj);
                return null;
        }

        // Initialize the weapon with its data and strategy
        if (weapon != null && strategy != null)
        {
            weapon.Initialize(weaponData, strategy);
            Debug.Log($"[WeaponFactory] Created weapon: {weaponType}");
            return weapon;
        }

        Object.Destroy(weaponObj);
        return null;
    }
}
