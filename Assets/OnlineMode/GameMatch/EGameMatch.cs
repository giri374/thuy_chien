namespace Assets.OnlineMode.GameMatch
{
    using Assets.Commons.SceneHandlers;
    using Assets.OnlineMode.ConnectionMenu;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    // TO DO: nên có các class handle client request, handle server command tách biệt?
    public class EGameMatch : NetworkBehaviour
    {
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

        [Rpc(SendTo.Server)]
        public void MarkCell_ServerRPC (int rowIndex, int columnIndex, RpcParams rpcParams = default)
        {
            if (InvalidToExecute())
            {
                return;
            }

            MarkCell_ClientsAndHostRPC(rowIndex, columnIndex);

            bool InvalidToExecute ()
            {
                return !IsPlayerTurn(rpcParams);
            }
        }

        private void Awake ()
        {
            if (InvalidToExecute())
            {
                Destroy(gameObject);
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
            AllPlayerClientIds = new ulong[MatchHostConnectionMenu.TotalPlayers];

            static bool InvalidToExecute ()
            {
                return Singleton != null;
            }
        }

        //NetworkManager.Singleton  might be null before NetworkManager game object finishes its Awake(),
        //NetworkObject == null before Start() finishes .
        public override void OnNetworkSpawn ()
        {
            if (InvalidToExecute())
            {
                return;
            }

            NetworkManager.Singleton.OnServerStarted += InitializeGameMatch;

            #region local functions
            static bool InvalidToExecute ()
            {
                return !NetworkManager.Singleton.IsServer;
            }

            void InitializeGameMatch ()
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnNewClientConnected;
            }
            void OnNewClientConnected (ulong clientId)
            {
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
                GetInMatch_ClientsAndHostRPC();
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
                PlayerOfCurrentTurnClientId = AllPlayerClientIds[Random.Range(minInclusive: 0, maxExclusive: AllPlayerClientIds.Length)];
            }
            #endregion
        }

        //private NetworkVariable<CellState> LastMarkedMark_NetworkVariable
        //{
        //    get => _lastMarkedMark_NetworkVariable;
        //    set
        //    {
        //        if (_lastMarkedMark_NetworkVariable != null)
        //        {
        //            return;
        //        }

        //        _lastMarkedMark_NetworkVariable = value;
        //        _lastMarkedMark_NetworkVariable.OnValueChanged = (previousValue, newValue) => GameLogicController.LastMarkedMark = newValue;
        //    }
        //}

        //private NetworkVariable<GameState> CurrentGameState_NetworkVariable
        //{
        //    get => _currentGameState_NetworkVariable;
        //    set
        //    {
        //        if (_currentGameState_NetworkVariable != value)
        //        {
        //            return;
        //        }

        //        _currentGameState_NetworkVariable = value;
        //        _currentGameState_NetworkVariable.OnValueChanged = (previousValue, newValue) => GameLogicController.CurrentGameState = newValue;
        //    }
        //}

        //private NetworkVariable<CellState> _lastMarkedMark_NetworkVariable;
        //private NetworkVariable<GameState> _currentGameState_NetworkVariable;

        private ulong[] AllPlayerClientIds
        {
            get => _allPlayersClientId;
            set
            {
                if (_allPlayersClientId != null)
                {
                    return;
                }

                _allPlayersClientId = value;
            }
        }
        private ulong PlayerOfCurrentTurnClientId { get; set; }

        private static EGameMatch _singleton;

        private ulong[] _allPlayersClientId;

        private bool IsPlayerTurn (RpcParams rpcParams)
        {
            return rpcParams.Receive.SenderClientId == PlayerOfCurrentTurnClientId;
        }

        private bool AllPlayersAreStillConnected ()
        {
            return NetworkManager.Singleton.ConnectedClients.Count == MatchHostConnectionMenu.TotalPlayers;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void GetInMatch_ClientsAndHostRPC ()
        {
            SceneManager.LoadScene(SceneInGame.SetupScene.ToString());
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MarkCell_ClientsAndHostRPC (int rowIndex, int columnIndex)
        {
        }
    }
}
