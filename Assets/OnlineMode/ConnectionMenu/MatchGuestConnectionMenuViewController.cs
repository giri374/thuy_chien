namespace Assets.OnlineMode.ConnectionMenu
{
    using System.Collections;
    using System.Threading.Tasks;
    using Assets.Commons.YesNoMenuHandlers;
    using Assets.OnlineMode.Connection;
    using UnityEngine;
    using UnityEngine.UI;
    using Unity.Netcode;
    using UnityEngine.SceneManagement;
    using TMPro;

    public class MatchGuestConnectionMenuViewController : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _matchIdTextController;
        [SerializeField]
        private Button _joinMatchButton;

        [SerializeField]
        private TextMeshProUGUI _statusTextController;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private YesNoMenuViewController _yesNoMenuViewController;

        private Coroutine _waitForHostRoutine;
        private Coroutine _connectionWatchRoutine;
        private bool _wasConnected;

        private void Awake ()
        {
            _joinMatchButton.onClick.AddListener(ConfirmJoinMatch);
        }

        private void OnEnable ()
        {
            RegisterButtonHandlers();
            RegisterMatchSyncEvents();
            StartConnectionWatch();
        }

        private string JoinString => _matchIdTextController.text.Trim().ToUpperInvariant();

        private void ConfirmJoinMatch ()
        {
            if (!IsJoinStringValid())
            {
                UpdateStatus("Join code must be 6 characters.");
                return;
            }

            _yesNoMenuViewController.ConfirmBeforeExecuting(JoinMatch);
            async Task JoinMatch ()
            {
                SetJoinButtonInteractable(false);
                SetRetryInteractable(false);

                // Reset previous attempt state so the next join uses a clean network state.
                _wasConnected = false;
                EConnection.Disconnect();

                if (_waitForHostRoutine != null)
                {
                    StopCoroutine(_waitForHostRoutine);
                    _waitForHostRoutine = null;
                }

                UpdateStatus("Connecting...");
                await MatchGuestConnectionMenu.ConnectToMatch_Async(JoinString);

                if (!EConnection.ReadyToConnect())
                {
                    UpdateStatus(ConnectionErrorState.GetOrDefault("Failed to connect. Check join code."));
                    SetJoinButtonInteractable(true);
                    SetRetryInteractable(true);
                    return;
                }

                UpdateStatus("Connected. Waiting for host...");
                StartWaitingForHost();
            }
        }

        private bool IsJoinStringValid ()
        {
            if (_matchIdTextController == null)
            {
                return false;
            }

            var text = _matchIdTextController.text;
            return !string.IsNullOrEmpty(text) && text.Trim().Length == 6;
        }

        private void OnDisable ()
        {
            if (_waitForHostRoutine != null)
            {
                StopCoroutine(_waitForHostRoutine);
                _waitForHostRoutine = null;
            }

            if (_connectionWatchRoutine != null)
            {
                StopCoroutine(_connectionWatchRoutine);
                _connectionWatchRoutine = null;
            }

            UnregisterMatchSyncEvents();
        }

        private void StartWaitingForHost ()
        {
            if (_waitForHostRoutine != null)
            {
                StopCoroutine(_waitForHostRoutine);
            }

            _waitForHostRoutine = StartCoroutine(WaitForHostRoutine());
        }

        private IEnumerator WaitForHostRoutine ()
        {
            while (true)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                {
                    UpdateStatus("Host connected. Syncing setup...");
                    yield break;
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

        private void SetJoinButtonInteractable (bool isInteractable)
        {
            if (_joinMatchButton != null)
            {
                _joinMatchButton.interactable = isInteractable;
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
            SetJoinButtonInteractable(true);
            SetRetryInteractable(false);
        }

        private void OnBackClicked ()
        {
            // Xóa tất cả dữ liệu placement
            GameManager.Instance.ClearAllPlacements();

            // Reset game state
            GameManager.Instance.SetGameMode(GameMode.PlayWithBot);
            GameManager.Instance.SetCurrentSetupPlayer(1);
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
                    SetJoinButtonInteractable(true);
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
