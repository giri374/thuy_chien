namespace Assets.OnlineMode.GameMatch
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Assets.OnlineMode.ConnectionMenu;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    // TO DO: nên có các class handle client request, handle server command tách biệt?
    public class EGameMatch : NetworkBehaviour
    {
        public event Action OnSetupSynced;

        public bool IsSetupSynced { get; private set; }

        private readonly NetworkVariable<bool> _opponentDisconnected = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
        public event Action<bool> OnOpponentDisconnectedChanged;

        private ulong _disconnectedClientId = ulong.MaxValue;
        private Coroutine _disconnectTimeoutCoroutine;
        private const float DisconnectTimeoutSeconds = 60f;
        private int _localLastExecutedIndex = -1;

        private struct PlayerSetupPayload
        {
            public ShipPlacementNet[] Placements;
            public WeaponType[] Weapons;
        }

        private struct ShipPlacementNet : INetworkSerializable
        {
            public int ShipId;
            public int X;
            public int Y;
            public bool IsHorizontal;

            public ShipPlacementNet (int shipId, int x, int y, bool isHorizontal)
            {
                ShipId = shipId;
                X = x;
                Y = y;
                IsHorizontal = isHorizontal;
            }

            public void NetworkSerialize<T> (BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ShipId);
                serializer.SerializeValue(ref X);
                serializer.SerializeValue(ref Y);
                serializer.SerializeValue(ref IsHorizontal);
            }
        }

        public static EGameMatch Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton != null)
                {
                    return;
                }

                _singleton = value;
            }
        }

        private void Awake ()
        {
            if (Singleton != null && Singleton != this)
            {
                enabled = false;
                // The way netcode communicates is by sending messages that have the networkid of the network object and the index of the network behavior.
                //  This means that the network behaviors cannot be different between clients. 
                // This means that you cannot delete a network behavior on one client but not another.
                return;
            }

            Singleton = this;

            ////TO DO: NetworkVariable/NetworkList BẮT BUỘC là non-static field, muốn dùng property thì cũng phải có 1 explicit field.
            ////Lý do tồn tại NetworkVariable trong khi đã có RPC vì RPC là fire then forget ko lưu state,
            ////nếu client connect muộn, reconnect giữa trận thì ko đc auto sync mà phải tự viết hàm rpc sync nào đó để
            ////gọi trong OnClientConnected để sync đủ thứ, NetworkVariable/NetworkList thì tự động sync mỗi khi property Value của nó thay đổi.
            ////Bên dưới còn đoạn nữa khai báo các property này đã comment .
            //LastMarkedMark_NetworkVariable = new();
            //CurrentGameState_NetworkVariable = new();
            EnsurePlayerIdArray();
            Array.Clear(_allPlayersClientId, 0, _allPlayersClientId.Length);
            _setupByClientId = new Dictionary<ulong, PlayerSetupPayload>();

            _opponentDisconnected.OnValueChanged += (_, newValue) =>
            {
                OnOpponentDisconnectedChanged?.Invoke(newValue);
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState ()
        {
            _singleton = null;
        }

        public override void OnDestroy ()
        {
            if (_singleton == this)
            {
                _singleton = null;
            }

            base.OnDestroy();
        }

        // In EGameMatch — field declarations (alongside _singleton, _allPlayersClientId, etc.)

        // The persistent battle record. Auto-synced to any client that (re)connects.
        // MUST be initialized as a field, not in Awake — Netcode requirement.
        private readonly NetworkList<CommandData> _battleCommands = new NetworkList<CommandData>(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Tracks the highest CommandIndex that has been broadcast and acknowledged by the server.
        // Reconnecting clients use this to know how many commands they missed.
        private readonly NetworkVariable<int> _serverCommandCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        //NetworkManager.Singleton  might be null before NetworkManager game object finishes its Awake(),
        //NetworkObject might be null before Start() finishes .
        public override void OnNetworkSpawn ()
        {
            Debug.Log($"[EGameMatch] OnNetworkSpawn | IsServer={IsServer} IsClient={IsClient} IsHost={IsHost}");

            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[EGameMatch] NetworkManager not ready in OnNetworkSpawn.");
                return;
            }

            EnsurePlayerIdArray();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnServerStarted += InitializeGameMatch;
            }

            if (IsClient)
            {
                SubmitLocalSetupToHost();
            }

            #region local functions
            void InitializeGameMatch ()
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnNewClientConnected;
            }
            void OnNewClientConnected (ulong clientId)
            {
                EnsurePlayerIdArray();

                // NEW: check if this is the disconnected player returning
                if (_opponentDisconnected.Value && clientId == _disconnectedClientId)
                {
                    Debug.Log($"[EGameMatch] Client {clientId} reconnected.");

                    // Cancel the timeout
                    if (_disconnectTimeoutCoroutine != null)
                    {
                        StopCoroutine(_disconnectTimeoutCoroutine);
                        _disconnectTimeoutCoroutine = null;
                    }

                    _opponentDisconnected.Value = false;
                    _disconnectedClientId = ulong.MaxValue;

                    // Tell the returning client to replay from where it left off.
                    // We pass the client's last known index via a TargetClientId RPC.
                    // Since we don't store the client's lastExecutedIndex server-side,
                    // we simply tell it the current server count — the client will figure
                    // out its own gap by comparing against its local lastExecutedIndex.
                    SyncOnReconnect_ClientRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    return;
                }

                if (MatchStartedOnce())
                {
                    return;
                }

                if (IsHostConnectedAsClient(clientId))
                {
                    //AllPlayersClientId[0] = 0;
                    return;
                }

                AllPlayerClientIds[1] = clientId;
                PickFirstTurnPlayerRandomly();
                // GetInMatch_ClientsAndHostRPC();
            }

            bool MatchStartedOnce ()
            {
                return AllPlayerClientIds[1] != 0;
            }

            static bool IsHostConnectedAsClient (ulong clientId)
            {
                return clientId == 0;
            }

            void PickFirstTurnPlayerRandomly ()
            {
                // Sử dụng UnityEngine.Random để dùng được .Range
                PlayerOfCurrentTurnClientId = AllPlayerClientIds[UnityEngine.Random.Range(0, AllPlayerClientIds.Length)];
            }
            #endregion
        }

        private void EnsurePlayerIdArray ()
        {
            if (_allPlayersClientId == null || _allPlayersClientId.Length != MatchHostConnectionMenu.TotalPlayers)
            {
                _allPlayersClientId = new ulong[MatchHostConnectionMenu.TotalPlayers];
            }
        }

        private ulong[] AllPlayerClientIds => _allPlayersClientId;
        private ulong PlayerOfCurrentTurnClientId { get; set; }

        private static EGameMatch _singleton;

        private ulong[] _allPlayersClientId;
        private Dictionary<ulong, PlayerSetupPayload> _setupByClientId;
        private bool _localSetupSubmitted;
        private bool _battleSceneLoaded;
        private bool _setupSyncTimeoutScheduled;
        private const float SetupSyncTimeoutSeconds = 120f;

        private bool AllPlayersAreStillConnected ()
        {
            var manager = NetworkManager.Singleton;
            return manager != null && manager.ConnectedClients.Count == MatchHostConnectionMenu.TotalPlayers;
        }

        private void SubmitLocalSetupToHost ()
        {
            if (_localSetupSubmitted)
            {
                Debug.Log("[EGameMatch] Local setup already submitted.");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[EGameMatch] GameManager not found, cannot submit setup.");
                return;
            }

            var placements = BuildLocalPlacementArray();
            var weapons = BuildLocalWeaponArray();

            _localSetupSubmitted = true;
            Debug.Log($"[EGameMatch] SubmitLocalSetupToHost: placements={placements.Length}, weapons={weapons.Length} | IsServer={IsServer}");

            if (IsServer)
            {
                var clientId = NetworkManager.Singleton.LocalClientId;
                if (_setupByClientId == null)
                {
                    _setupByClientId = new Dictionary<ulong, PlayerSetupPayload>();
                }
                _setupByClientId[clientId] = new PlayerSetupPayload
                {
                    Placements = placements,
                    Weapons = weapons
                };
                Debug.Log($"[EGameMatch] Host added own setup to dict: clientId={clientId}");
                StartSetupSyncTimeoutIfServer();
            }
            else
            {
                SubmitLocalSetup_ServerRpc(placements, weapons);
            }
        }

        private ShipPlacementNet[] BuildLocalPlacementArray ()
        {
            var list = GameManager.Instance.GetPlacements(1);
            if (list == null || list.Count == 0)
            {
                return Array.Empty<ShipPlacementNet>();
            }

            var result = new ShipPlacementNet[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var data = list[i];
                result[i] = new ShipPlacementNet(data.shipID, data.position.x, data.position.y, data.isHorizontal);
            }

            return result;
        }

        private WeaponType[] BuildLocalWeaponArray ()
        {
            var list = GameManager.Instance.GetSelectedWeapons(1);
            if (list == null || list.Count == 0)
            {
                return Array.Empty<WeaponType>();
            }

            return list.ToArray();
        }

        [Rpc(SendTo.Server)]
        private void SubmitLocalSetup_ServerRpc (ShipPlacementNet[] placements, WeaponType[] weapons, RpcParams rpcParams = default)
        {
            Debug.Log($"[EGameMatch] ServerRpc received setup from clientId={rpcParams.Receive.SenderClientId} placements={placements?.Length ?? 0} weapons={weapons?.Length ?? 0}");
            if (_setupByClientId == null)
            {
                _setupByClientId = new Dictionary<ulong, PlayerSetupPayload>();
            }

            var senderClientId = rpcParams.Receive.SenderClientId;
            _setupByClientId[senderClientId] = new PlayerSetupPayload
            {
                Placements = placements,
                Weapons = weapons
            };

            StartSetupSyncTimeoutIfServer();

            // If sender is not host (clientId != 0), it's the guest. Set AllPlayerClientIds[1].
            EnsurePlayerIdArray();
            if (senderClientId != 0)
            {
                AllPlayerClientIds[1] = senderClientId;
                Debug.Log($"[EGameMatch] Set AllPlayerClientIds[1]={senderClientId}");
            }

            if (!AllPlayersAreStillConnected())
            {
                Debug.Log("[EGameMatch] Waiting for all players to connect before sync.");
                return;
            }

            if (!TryGetPayloadForPlayerIndex(1, out var player1Payload) ||
                !TryGetPayloadForPlayerIndex(2, out var player2Payload))
            {
                Debug.Log("[EGameMatch] Missing setup payload(s); waiting for both.");
                return;
            }

            SyncSetup_ClientsAndHostRpc(
                player1Payload.Placements,
                player1Payload.Weapons,
                player2Payload.Placements,
                player2Payload.Weapons);
        }

        private bool TryGetPayloadForPlayerIndex (int playerIndex, out PlayerSetupPayload payload)
        {
            payload = default;

            var clientId = playerIndex == 1 ? 0UL : AllPlayerClientIds[1];
            if (clientId == 0UL && playerIndex == 2)
            {
                Debug.Log($"[EGameMatch] TryGetPayloadForPlayerIndex({playerIndex}): AllPlayerClientIds[1] not set yet (is 0).");
                return false;
            }

            bool found = _setupByClientId != null && _setupByClientId.TryGetValue(clientId, out payload);
            Debug.Log($"[EGameMatch] TryGetPayloadForPlayerIndex({playerIndex}): clientId={clientId}, found={found}");
            if (_setupByClientId != null)
            {
                Debug.Log($"[EGameMatch] Current _setupByClientId keys: {string.Join(", ", _setupByClientId.Keys)}");
            }
            return found;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncSetup_ClientsAndHostRpc (
            ShipPlacementNet[] player1Placements,
            WeaponType[] player1Weapons,
            ShipPlacementNet[] player2Placements,
            WeaponType[] player2Weapons)
        {
            Debug.Log("[EGameMatch] SyncSetup_ClientsAndHostRpc received. Applying setup.");
            ApplySyncedSetup(player1Placements, player1Weapons, player2Placements, player2Weapons);

            IsSetupSynced = true;
            OnSetupSynced?.Invoke();

            LoadBattleSceneOnce();
        }

        private void LoadBattleSceneOnce ()
        {
            if (_battleSceneLoaded)
            {
                return;
            }

            _battleSceneLoaded = true;
            Debug.Log("[EGameMatch] Loading BattleScene.");

            // Only server loads the scene. Clients will be notified via Netcode and transition automatically.
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsServer && networkManager.SceneManager != null)
            {
                networkManager.SceneManager.LoadScene(SceneNames.Battle, LoadSceneMode.Single);
            }
        }

        private void StartSetupSyncTimeoutIfServer ()
        {
            if (!IsServer || _setupSyncTimeoutScheduled)
            {
                return;
            }

            _setupSyncTimeoutScheduled = true;
            StartCoroutine(SetupSyncTimeoutCoroutine());
        }

        private IEnumerator SetupSyncTimeoutCoroutine ()
        {
            yield return new WaitForSeconds(SetupSyncTimeoutSeconds);

            if (IsSetupSynced)
            {
                yield break;
            }

            Debug.LogWarning("[EGameMatch] Setup sync timed out. Returning to main menu.");
            AbortSetupSync_ClientsAndHostRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbortSetupSync_ClientsAndHostRpc ()
        {
            Debug.LogWarning("[EGameMatch] Setup sync aborted.");
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void ApplySyncedSetup (
            ShipPlacementNet[] player1Placements,
            WeaponType[] player1Weapons,
            ShipPlacementNet[] player2Placements,
            WeaponType[] player2Weapons)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[EGameMatch] GameManager not found, cannot apply setup.");
                return;
            }

            GameManager.Instance.ClearAllPlacements();
            GameManager.Instance.ClearAllWeapons();

            ApplyPlacementsToPlayer(1, player1Placements);
            ApplyPlacementsToPlayer(2, player2Placements);

            ApplyWeaponsToPlayer(1, player1Weapons);
            ApplyWeaponsToPlayer(2, player2Weapons);
        }

        private void ApplyPlacementsToPlayer (int playerIndex, ShipPlacementNet[] placements)
        {
            if (placements == null)
            {
                return;
            }

            var targetList = GameManager.Instance.GetPlacements(playerIndex);
            targetList.Clear();

            var seenShipIds = new HashSet<int>();

            foreach (var placement in placements)
            {
                if (!IsValidPlacement(placement, seenShipIds))
                {
                    continue;
                }

                targetList.Add(new GameManager.ShipPlacementData
                {
                    shipID = placement.ShipId,
                    position = new Vector2Int(placement.X, placement.Y),
                    isHorizontal = placement.IsHorizontal
                });
            }
        }

        private bool IsValidPlacement (ShipPlacementNet placement, HashSet<int> seenShipIds)
        {
            if (placement.ShipId < 0)
            {
                Debug.LogWarning("[EGameMatch] Invalid placement: negative ship id.");
                return false;
            }

            if (placement.X < 0 || placement.Y < 0)
            {
                Debug.LogWarning("[EGameMatch] Invalid placement: negative coordinates.");
                return false;
            }

            if (!seenShipIds.Add(placement.ShipId))
            {
                Debug.LogWarning($"[EGameMatch] Duplicate placement for ship id {placement.ShipId}.");
                return false;
            }

            return true;
        }

        private void ApplyWeaponsToPlayer (int playerIndex, WeaponType[] weapons)
        {
            if (weapons == null)
            {
                return;
            }

            var targetList = GameManager.Instance.GetSelectedWeapons(playerIndex);
            targetList.Clear();

            foreach (var weapon in weapons)
            {
                if (!targetList.Contains(weapon))
                {
                    targetList.Add(weapon);
                }
            }
        }

        // ── Battle Command Relay ────────────────────────────────────────

        public event System.Action<CommandData> OnCommandReceived;

        [Rpc(SendTo.Server)]
        public void SubmitAttack_ServerRpc (CommandData data, RpcParams rpcParams = default)
        {
            Debug.Log($"[EGameMatch] SubmitAttack_ServerRpc from clientId={rpcParams.Receive.SenderClientId} cmd={data.CommandIndex}");

            // Append to the persistent list BEFORE broadcasting.
            // This ensures the record exists even if broadcast delivery fails.
            data.CommandIndex = _battleCommands.Count; // 0-based, before Add

            Debug.Log($"[EGameMatch] SubmitAttack_ServerRpc from clientId={rpcParams.Receive.SenderClientId}, assigned index={data.CommandIndex}");

            _battleCommands.Add(data);
            _serverCommandCount.Value = _battleCommands.Count;

            BroadcastAttack_ClientsAndHostRpc(data);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastAttack_ClientsAndHostRpc (CommandData data)
        {
            Debug.Log($"[EGameMatch] BroadcastAttack received cmd={data.CommandIndex} weapon={data.WeaponType} pos=({data.X},{data.Y}) attacker={data.Attacker}");
            OnCommandReceived?.Invoke(data);
        }

        // Disconnection handling: if a client disconnects mid-match, set a flag and start a timeout.

        // Call this in OnNetworkSpawn, inside the server-only InitializeGameMatch local function,
        // alongside the existing OnClientConnectedCallback subscription:
        // NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        private void OnClientDisconnected (ulong clientId)
        {
            // Only care about the guest (clientId != 0) disconnecting mid-battle
            if (clientId == 0) return; // host disconnect = game over, handle separately
            if (!IsServer) return;

            Debug.LogWarning($"[EGameMatch] Client {clientId} disconnected.");
            _disconnectedClientId = clientId;
            _opponentDisconnected.Value = true;

            _disconnectTimeoutCoroutine = StartCoroutine(DisconnectTimeoutCoroutine());
        }

        // In EGameMatch:
        private IEnumerator DisconnectTimeoutCoroutine ()
        {
            Debug.Log($"[EGameMatch] Disconnect timeout started ({DisconnectTimeoutSeconds}s).");
            yield return new WaitForSeconds(DisconnectTimeoutSeconds);

            // If opponent is still gone after timeout, abort the match
            if (_opponentDisconnected.Value)
            {
                Debug.LogWarning("[EGameMatch] Disconnect timeout expired — aborting match.");
                AbortMatch_ClientsAndHostRpc("Opponent left the game.");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbortMatch_ClientsAndHostRpc (string reason)
        {
            Debug.LogWarning($"[EGameMatch] Match aborted: {reason}");
            // Disconnect cleanly then go to main menu
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SyncOnReconnect_ClientRpc (RpcParams rpcParams = default)
        {
            Debug.Log($"[EGameMatch] SyncOnReconnect received. Server has {_serverCommandCount.Value} commands. Local lastExecutedIndex={_localLastExecutedIndex}.");
            StartCoroutine(ReplayMissingCommands());
        }

        // Called by BattleSceneLogic after each ExecuteReceivedCommand:
        public void NotifyCommandExecuted (int commandIndex)
        {
            _localLastExecutedIndex = commandIndex;
        }

        // The replay coroutine:
        private IEnumerator ReplayMissingCommands ()
        {
            // Block input on BattleSceneLogic while replaying
            OnOpponentDisconnectedChanged?.Invoke(true); // reuse the block flag temporarily

            // Wait one frame to ensure _battleCommands is fully synced
            yield return null;

            int replayFrom = _localLastExecutedIndex + 1;
            int replayTo = _battleCommands.Count;

            Debug.Log($"[EGameMatch] Replaying commands [{replayFrom}..{replayTo - 1}]");

            for (int i = replayFrom; i < replayTo; i++)
            {
                var cmd = _battleCommands[i];
                Debug.Log($"[EGameMatch] Replaying command index={cmd.CommandIndex}");
                OnCommandReceived?.Invoke(cmd);

                // Wait for the command to execute before firing the next one.
                // ExecuteReceivedCommand is async — yield one frame between each.
                yield return new WaitForSeconds(0.15f); // small delay for visual clarity
            }

            // Unblock input
            OnOpponentDisconnectedChanged?.Invoke(false);
            Debug.Log("[EGameMatch] Replay complete.");

        }


    }

}
