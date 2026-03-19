using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a history of all attack commands executed during a game session.
/// This enables replay, undo, and network synchronization features.
/// </summary>
public class AttackCommandHistory
{
    private List<IAttackCommand> _commands = new List<IAttackCommand>();

    /// <summary>
    /// Records an executed command in the history.
    /// </summary>
    public void RecordCommand(IAttackCommand command)
    {
        if (command == null)
        {
            Debug.LogWarning("[AttackCommandHistory] Attempted to record null command!");
            return;
        }

        _commands.Add(command);
        Debug.Log($"[AttackCommandHistory] Recorded: {command}");
    }

    /// <summary>
    /// Gets the entire command history as a read-only list.
    /// </summary>
    public IReadOnlyList<IAttackCommand> GetHistory()
    {
        return _commands.AsReadOnly();
    }

    /// <summary>
    /// Gets a command at a specific index in the history.
    /// </summary>
    public IAttackCommand GetCommandAt(int index)
    {
        if (index < 0 || index >= _commands.Count)
        {
            Debug.LogWarning($"[AttackCommandHistory] Index {index} out of range. History size: {_commands.Count}");
            return null;
        }

        return _commands[index];
    }

    /// <summary>
    /// Gets the most recent command (last executed).
    /// Returns null if no commands have been recorded yet.
    /// </summary>
    public IAttackCommand GetLastCommand()
    {
        if (_commands.Count == 0)
        {
            return null;
        }

        return _commands[_commands.Count - 1];
    }

    /// <summary>
    /// Gets the total number of commands in history.
    /// </summary>
    public int GetCommandCount()
    {
        return _commands.Count;
    }

    /// <summary>
    /// Clears all recorded commands. Use this when starting a new game.
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
        Debug.Log("[AttackCommandHistory] History cleared.");
    }

    /// <summary>
    /// Gets a summary of the command history for debugging.
    /// </summary>
    public string GetDebugSummary()
    {
        var summary = $"Command History ({_commands.Count} total):\n";
        for (int i = 0; i < _commands.Count; i++)
        {
            var cmd = _commands[i];
            summary += $"  [{i}] {cmd}\n";
        }
        return summary;
    }
}
