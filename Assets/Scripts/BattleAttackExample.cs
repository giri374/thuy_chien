using UnityEngine;

/// <summary>
/// SAMPLE: Cách sử dụng BattleWeaponManager trong attack logic
/// 
/// Thêm code này vào BattleSceneLogic hoặc UI handler khi xử lý attack
/// </summary>
public class BattleAttackExample : MonoBehaviour
{
    private BattleWeaponManager weaponManager;

    private void Start()
    {
        // Tìm BattleWeaponManager trong scene
        weaponManager = GetComponent<BattleWeaponManager>();
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<BattleWeaponManager>();
        }
    }

    // ── EXAMPLE 1: Lấy current weapon khi player click cell ──

    /// <summary>
    /// Ví dụ: Gọi khi player click vào lưới để tấn công
    /// </summary>
    public void OnCellClickedForAttack(Vector2Int targetPosition)
    {
        // Lấy vũ khí được chọn hiện tại
        WeaponType currentWeapon = weaponManager.GetCurrentWeapon();
        WeaponData weaponData = weaponManager.GetCurrentWeaponData();

        Debug.Log($"Attacking with {weaponData.weaponName} at {targetPosition}");

        // Tính toán hiệu ứng dựa trên loại vũ khí
        switch (currentWeapon)
        {
            case WeaponType.NormalShot:
                ExecuteNormalAttack(targetPosition);
                break;
            case WeaponType.NuclearBomb:
                ExecuteNuclearAttack(targetPosition);
                break;
            case WeaponType.Bomber:
                ExecuteBomberAttack(targetPosition);
                break;
            case WeaponType.Torpedoes:
                ExecuteTorpedoAttack(targetPosition);
                break;
            case WeaponType.Radar:
                ExecuteRadarAttack(targetPosition);
                break;
            case WeaponType.AntiAircraft:
                ExecuteAntiAircraftAttack(targetPosition);
                break;
        }
    }

    // ── EXAMPLE 2: Trigger attack qua UI button ──

    /// <summary>
    /// Ví dụ: Khi player click "Attack" button sau khi chọn cell + weapon
    /// </summary>
    public void OnAttackButtonClicked(Vector2Int targetPosition)
    {
        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found!");
            return;
        }

        WeaponType selectedWeapon = weaponManager.GetCurrentWeapon();
        WeaponData weaponData = weaponManager.GetCurrentWeaponData();

        if (weaponData == null)
        {
            Debug.LogError($"Weapon data not found for {selectedWeapon}");
            return;
        }

        // Perform attack
        PerformAttack(selectedWeapon, targetPosition, weaponData);
    }

    // ── EXAMPLE 3: Integrate with BattleSceneLogic ──

    /// <summary>
    /// Ví dụ: Integrare BattleWeaponManager vào existing CreateAndExecuteAttackCommandAsync
    /// 
    /// Thay đổi trong BattleSceneLogic:
    /// 
    /// private async void CreateAndExecuteAttackCommandAsync(Vector2Int position, Turn attacker)
    /// {
    ///     // OLD CODE (hard-coded weapon):
    ///     // IAttackCommand command = new AttackCommand(WeaponType.NormalShot, position, attacker);
    ///     
    ///     // NEW CODE (get from BattleWeaponManager):
    ///     BattleWeaponManager weaponManager = GetComponent<BattleWeaponManager>();
    ///     WeaponType currentWeapon = weaponManager.GetCurrentWeapon();
    ///     IAttackCommand command = new AttackCommand(currentWeapon, position, attacker);
    ///     
    ///     // ... rest of method
    /// }
    /// </summary>
    public void ExampleIntegrationWithBattleSceneLogic()
    {
        // Pseudo-code để integrate vào BattleSceneLogic
        /*
        public class BattleSceneLogic : MonoBehaviour
        {
            private BattleWeaponManager weaponManager;
            
            private void Awake()
            {
                // ... existing code ...
                weaponManager = GetComponent<BattleWeaponManager>();
            }
            
            private async void CreateAndExecuteAttackCommandAsync(Vector2Int position, Turn attacker)
            {
                // Get current weapon from BattleWeaponManager instead of hard-coding
                WeaponType currentWeapon = weaponManager.GetCurrentWeapon();
                
                IAttackCommand command = new AttackCommand(currentWeapon, position, attacker);
                // ... rest of method
            }
        }
        */
    }

    // ── Helper Methods ──

    private void ExecuteNormalAttack(Vector2Int position)
    {
        Debug.Log($"Normal attack at {position}");
        // Implementation
    }

    private void ExecuteNuclearAttack(Vector2Int position)
    {
        Debug.Log($"Nuclear attack at {position}");
        // Implementation - affects larger area
    }

    private void ExecuteBomberAttack(Vector2Int position)
    {
        Debug.Log($"Bomber attack at {position}");
        // Implementation - area damage
    }

    private void ExecuteTorpedoAttack(Vector2Int position)
    {
        Debug.Log($"Torpedo attack at {position}");
        // Implementation
    }

    private void ExecuteRadarAttack(Vector2Int position)
    {
        Debug.Log($"Radar attack at {position}");
        // Implementation - reveals ships
    }

    private void ExecuteAntiAircraftAttack(Vector2Int position)
    {
        Debug.Log($"Anti-aircraft attack at {position}");
        // Implementation
    }

    private void PerformAttack(WeaponType weaponType, Vector2Int position, WeaponData weaponData)
    {
        Debug.Log($"Performing {weaponData.weaponName} attack at {position}");
        // Unified attack handler
    }
}
