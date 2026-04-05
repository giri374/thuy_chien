using System;
using Core.Models;
using UnityEngine;

public class OnlineBattleRelay
{
    private bool _inputBlocked;

    public event Action<Assets.OnlineMode.GameMatch.CommandData> OnCommandReceived;

    public void Subscribe ()
    {
        var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
        if (match == null)
        {
            Debug.LogWarning("[BattleSceneLogic] EGameMatch.Singleton null at Start - online commands won't execute.");
            return;
        }

        match.OnCommandReceived += HandleCommandReceived;
        match.OnOpponentDisconnectedChanged += HandleOpponentDisconnectedChanged;
    }

    public void Unsubscribe ()
    {
        var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
        if (match == null)
        {
            return;
        }

        match.OnCommandReceived -= HandleCommandReceived;
        match.OnOpponentDisconnectedChanged -= HandleOpponentDisconnectedChanged;
    }

    public bool IsLocalPlayer1 ()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null && nm.IsHost;
    }

    public bool IsLocalPlayer2 ()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null && nm.IsClient && !nm.IsHost;
    }

    public void TrySendAttack (Cell cell, bool isPlayer1Attacking, GameState currentState, WeaponType selectedWeapon)
    {
        if (_inputBlocked || currentState != GameState.Playing || cell == null || cell.cellState != CellState.Unknown)
        {
            return;
        }

        var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
        if (match == null)
        {
            Debug.LogWarning("[BattleSceneLogic] EGameMatch.Singleton is null - cannot send attack.");
            return;
        }

        var attacker = isPlayer1Attacking ? Turn.Player1 : Turn.Player2;

        var data = new Assets.OnlineMode.GameMatch.CommandData
        {
            WeaponType = (int) selectedWeapon,
            X = cell.gridPosition.x,
            Y = cell.gridPosition.y,
            Attacker = (int) attacker,
            CommandIndex = -1
        };

        Debug.Log($"[BattleSceneLogic] Sending online attack: {data.CommandIndex} weapon={selectedWeapon} pos={cell.gridPosition} attacker={attacker}");
        match.SubmitAttack_ServerRpc(data);
    }

    private void HandleCommandReceived (Assets.OnlineMode.GameMatch.CommandData data)
    {
        OnCommandReceived?.Invoke(data);
    }

    private void HandleOpponentDisconnectedChanged (bool isDisconnected)
    {
        if (isDisconnected)
        {
            Debug.Log("[BattleSceneLogic] Opponent disconnected - blocking input.");
            _inputBlocked = true;
            return;
        }

        Debug.Log("[BattleSceneLogic] Opponent reconnected - unblocking input.");
        _inputBlocked = false;
    }
}
