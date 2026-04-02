namespace Assets.OnlineMode.Connection.UnityMultiplayerServices
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection;
    using Assets.OnlineMode.ConnectionMenu;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using Unity.Services.Relay;
    using Unity.Services.Relay.Models;
    using UnityEngine;

    internal static class UnityRelay
    {
        private const string RelayConnectionType = "dtls";

        public static async Task<bool> StartRelay_Async ()
        {
            var relayAllocation = await RelayAllocation_Async();
            if (relayAllocation == null)
            {
                return false;
            }

            if (!TryConfigureRelayForTransport(relayAllocation))
            {
                return false;
            }

            return await SetMatchIdInMatchHostMenu();

            static async Task<Allocation> RelayAllocation_Async ()
            {
                try
                {
                    return await RelayService.Instance.CreateAllocationAsync(MatchHostConnectionMenu.MatchGuestCapacity);
                }
                catch (RelayServiceException e)
                {
                    Debug.LogError(e);
                    return null;
                }
            }

            async Task<bool> SetMatchIdInMatchHostMenu ()
            {
                try
                {
                    MatchHostConnectionMenu.MatchId = await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
                    return true;
                }
                catch (RelayServiceException e)
                {
                    Debug.LogError(e);
                    return false;
                }
            }
        }

        public static async Task<bool> StartRelay_Async (string joinString)
        {
            var relayAllocation = await RelayAllocation_Async();
            if (relayAllocation == null)
            {
                return false;
            }

            return TryConfigureRelayForTransport(relayAllocation);

            async Task<JoinAllocation> RelayAllocation_Async ()
            {
                try
                {
                    return await RelayService.Instance.JoinAllocationAsync(joinString);
                }
                catch (RelayServiceException e)
                {
                    bool joinCodeNotFound = e.Message != null && e.Message.ToLowerInvariant().Contains("join code not found");
                    string errorMessage = joinCodeNotFound
                        ? "Join code not found. Please check and try again."
                        : "Unable to join match. Please try again.";

                    if (joinCodeNotFound)
                    {
                        Debug.LogWarning($"[Relay] {errorMessage}");
                    }
                    else
                    {
                        Debug.LogError(e);
                    }

                    ConnectionErrorState.Set(errorMessage);
                    return null;
                }
            }

        }

        private static bool TryConfigureRelayForTransport (Allocation allocation)
        {
            var transport = GetTransport();
            if (transport == null)
            {
                return false;
            }

            transport.SetRelayServerData(serverData: AllocationUtils.ToRelayServerData(allocation, RelayConnectionType));
            return true;
        }

        private static bool TryConfigureRelayForTransport (JoinAllocation allocation)
        {
            var transport = GetTransport();
            if (transport == null)
            {
                return false;
            }

            transport.SetRelayServerData(serverData: AllocationUtils.ToRelayServerData(allocation, RelayConnectionType));
            return true;
        }

        private static UnityTransport GetTransport ()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null.");
                return null;
            }

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("NetworkTransport is not UnityTransport.");
            }

            return transport;
        }
    }
}
