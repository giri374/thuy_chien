using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Interface for all attack commands in the game.
/// Represents an immutable attack action with weapon type, position, and attacker info.
/// </summary>
public interface IAttackCommand
{
    /// <summary>
    /// Gets the weapon type used for this attack.
    /// </summary>
    WeaponType GetWeaponType ();

    /// <summary>
    /// Gets the target position for this attack.
    /// </summary>
    Vector2Int GetPosition ();

    /// <summary>
    /// Gets which player is attacking.
    /// </summary>
    Turn GetAttacker ();

    /// <summary>
    /// Executes the attack command asynchronously.
    /// Returns a Task that completes when the attack is resolved.
    /// The actual result handling is done via callback in the executor.
    /// </summary>
    Task ExecuteAsync ();
}
