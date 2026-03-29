namespace Assets.OnlineMode.ConnectionMenu
{
    using System.Collections;
    using Assets.OnlineMode.Connection;
    using UnityEngine;
    using UnityEngine.UI;
    using Unity.Netcode;
    using UnityEngine.SceneManagement;

    public class MatchHostConnectionMenuViewController : MonoBehaviour
    {
        [SerializeField]
        private Text _matchIdTextController;

        [SerializeField]
        private Text _statusTextController;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private Button _backButton;

        private Coroutine _waitForOpponentRoutine;
        private Coroutine _connectionWatchRoutine;
        private bool _wasConnected;

        private void OnEnable ()
        {
            UpdateStatus("Creating match...");
            RegisterButtonHandlers();
            CreateMatch_Async();
            RegisterMatchSyncEvents();
            StartConnectionWatch();
        }

        private void OnDisable ()
        {
            if (_waitForOpponentRoutine != null)
            {
                StopCoroutine(_waitForOpponentRoutine);
                _waitForOpponentRoutine = null;
            }

            if (_connectionWatchRoutine != null)
            {
                StopCoroutine(_connectionWatchRoutine);
                _connectionWatchRoutine = null;
            }

            UnregisterMatchSyncEvents();
        }

        private async void CreateMatch_Async ()
        {
            SetRetryInteractable(false);
            await MatchHostConnectionMenu.CreateMatch_Async();

            if (!EConnection.ReadyToConnect())
            {
                UpdateStatus("Failed to start host. Please retry.");
                SetRetryInteractable(true);
                return;
            }

            JoinString = MatchHostConnectionMenu.MatchId;
            UpdateStatus("Waiting for opponent...");
            StartWaitingForOpponent();
        }

        private string JoinString
        {
            set => _matchIdTextController.text = value;
        }

        private void StartWaitingForOpponent ()
        {
            if (_waitForOpponentRoutine != null)
            {
                StopCoroutine(_waitForOpponentRoutine);
            }

            _waitForOpponentRoutine = StartCoroutine(WaitForOpponentRoutine());
        }

        private IEnumerator WaitForOpponentRoutine ()
        {
            while (true)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    if (NetworkManager.Singleton.ConnectedClients.Count >= MatchHostConnectionMenu.TotalPlayers)
                    {
                        UpdateStatus("Opponent connected. Syncing setup...");
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private void UpdateStatus (string message)
        {
            if (_statusTextController != null)
            {
                _statusTextController.text = message;
            }
        }

        private void RegisterButtonHandlers ()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
                _retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
                _backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void OnRetryClicked ()
        {
            UpdateStatus("Retrying...");
            EConnection.Disconnect();
            CreateMatch_Async();
        }

        private void OnBackClicked ()
        {
            EConnection.Disconnect();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void SetRetryInteractable (bool isInteractable)
        {
            if (_retryButton != null)
            {
                _retryButton.interactable = isInteractable;
            }
        }

        private void StartConnectionWatch ()
        {
            if (_connectionWatchRoutine != null)
            {
                StopCoroutine(_connectionWatchRoutine);
            }

            _connectionWatchRoutine = StartCoroutine(ConnectionWatchRoutine());
        }

        private IEnumerator ConnectionWatchRoutine ()
        {
            while (true)
            {
                var isConnected = EConnection.ReadyToConnect();
                if (_wasConnected && !isConnected)
                {
                    UpdateStatus("Disconnected. Please retry.");
                    SetRetryInteractable(true);
                    yield break;
                }

                _wasConnected = isConnected;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void RegisterMatchSyncEvents ()
        {
            var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
            if (match != null)
            {
                match.OnSetupSynced += HandleSetupSynced;
            }
        }

        private void UnregisterMatchSyncEvents ()
        {
            var match = Assets.OnlineMode.GameMatch.EGameMatch.Singleton;
            if (match != null)
            {
                match.OnSetupSynced -= HandleSetupSynced;
            }
        }

        private void HandleSetupSynced ()
        {
            UpdateStatus("Setup synced. Starting battle...");
        }
    }
}
