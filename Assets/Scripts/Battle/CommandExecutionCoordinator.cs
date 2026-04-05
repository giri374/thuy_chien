using System;
using UnityEngine;

public class CommandExecutionCoordinator
{
    private readonly AttackCommandHistory _commandHistory;
    private readonly AttackCommandExecutor _commandExecutor;
    private int _commandCounter;
    private int LastExecutedIndexInternal { get; set; } = -1;

    public int LastExecutedIndex => LastExecutedIndexInternal;

    public event Action<CellAttackResult, int, WeaponType, Turn, bool> OnCommandExecuted;

    public CommandExecutionCoordinator (MonoBehaviour owner, GridManager player1Grid, GridManager player2Grid)
    {
        _commandHistory = new AttackCommandHistory();
        _commandExecutor = owner.GetComponent<AttackCommandExecutor>();
        if (_commandExecutor == null)
        {
            _commandExecutor = owner.gameObject.AddComponent<AttackCommandExecutor>();
        }

        _commandExecutor.Initialize(player1Grid, player2Grid);
    }

    public bool HasAlreadyExecuted (int commandIndex)
    {
        return commandIndex <= LastExecutedIndexInternal;
    }

    public void ExecutePlayerAttack (WeaponType weaponType, Vector2Int position, Turn attacker)
    {
        ExecuteCommandInternal(weaponType, position, attacker, isBotAttack: false, forcedCommandIndex: null);
    }

    public void ExecuteBotAttack (WeaponType weaponType, Vector2Int position)
    {
        ExecuteCommandInternal(weaponType, position, Turn.Player2, isBotAttack: true, forcedCommandIndex: null);
    }

    public void ExecuteNetworkCommand (WeaponType weaponType, Vector2Int position, Turn attacker, int commandIndex)
    {
        ExecuteCommandInternal(weaponType, position, attacker, isBotAttack: false, forcedCommandIndex: commandIndex);
    }

    private async void ExecuteCommandInternal (WeaponType weaponType, Vector2Int position, Turn attacker, bool isBotAttack, int? forcedCommandIndex)
    {
        var currentCommandIndex = forcedCommandIndex ?? _commandCounter++;
        IAttackCommand command = new AttackCommand(weaponType, position, attacker, currentCommandIndex);

        _commandHistory.RecordCommand(command);

        void HandleResult (CellAttackResult result)
        {
            LastExecutedIndexInternal = currentCommandIndex;
            OnCommandExecuted?.Invoke(result, currentCommandIndex, weaponType, attacker, isBotAttack);
        }

        if (weaponType != WeaponType.NormalShot)
        {
            await _commandExecutor.ExecuteWeaponAttackAsync(command, HandleResult);
        }
        else
        {
            await _commandExecutor.ExecuteAsync(command, HandleResult);
        }
    }
}
