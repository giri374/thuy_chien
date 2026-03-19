using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Concrete implementation of an attack command.
/// Stores the immutable input data for an attack: weapon type, position, and attacker.
/// This is the command data that can be serialized and sent over the network.
/// </summary>
public class AttackCommand : IAttackCommand
{
    /// <summary>
    /// The type of weapon being used for this attack. Defaults to NormalShot.
    /// </summary>
    public WeaponType weaponType { get; private set; }

    /// <summary>
    /// The target grid position for this attack.
    /// </summary>
    public Vector2Int position { get; private set; }

    /// <summary>
    /// Which player is performing this attack.
    /// </summary>
    public Turn attacker { get; private set; }

    /// <summary>
    /// Timestamp when this command was created (for history tracking).
    /// </summary>
    public long createdAtTick { get; private set; }

    /// <summary>
    /// Constructor for creating an attack command.
    /// </summary>
    /// <param name="weaponType">The weapon type to use. Defaults to NormalShot.</param>
    /// <param name="position">The target position on the grid.</param>
    /// <param name="attacker">Which player is attacking (Player1 or Player2).</param>
    public AttackCommand(WeaponType weaponType, Vector2Int position, Turn attacker)
    {
        this.weaponType = weaponType;
        this.position = position;
        this.attacker = attacker;
        this.createdAtTick = System.DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Constructor with default NormalShot weapon.
    /// </summary>
    public AttackCommand(Vector2Int position, Turn attacker)
        : this(WeaponType.NormalShot, position, attacker)
    {
    }

    public WeaponType GetWeaponType()
    {
        return weaponType;
    }

    public Vector2Int GetPosition()
    {
        return position;
    }

    public Turn GetAttacker()
    {
        return attacker;
    }

    public async Task ExecuteAsync()
    {
        // The actual execution is handled by AttackCommandExecutor.
        // This just ensures the Task completes immediately.
        // In a networked scenario, this could be where we send the command to the server.
        await Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"AttackCommand{{weapon:{weaponType}, pos:{position}, attacker:{attacker}}}";
    }
}
