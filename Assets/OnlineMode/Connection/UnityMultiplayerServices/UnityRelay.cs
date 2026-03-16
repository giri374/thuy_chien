namespace Assets.OnlineMode.Connection.UnityMultiplayerServices
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.ConnectionMenu;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using Unity.Services.Relay;
    using Unity.Services.Relay.Models;
    using UnityEngine;

    internal static class UnityRelay
    {
        static UnityRelay ()
        {
            Transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            RelayConnectionType = "dtls";
        }

        public static async Task StartRelay_Async ()
        {
            var relayAllocation = await RelayAllocation_Async();
            if (relayAllocation == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
                return;
            }

            ConfigureRelayForTransport();
            await SetMatchIdInMatchHostMenu();

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

            void ConfigureRelayForTransport ()
            {
                Transport.SetRelayServerData(serverData: AllocationUtils.ToRelayServerData(relayAllocation, RelayConnectionType));
            }

            async Task SetMatchIdInMatchHostMenu ()
            {
                try
                {
                    MatchHostConnectionMenu.MatchId = await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
                }
                catch (RelayServiceException e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public static async Task StartRelay_Async (string joinString)
        {
            var relayAllocation = await RelayAllocation_Async();
            if (relayAllocation == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
                return;
            }

            ConfigureRelayForTransport();

            async Task<JoinAllocation> RelayAllocation_Async ()
            {
                try
                {
                    return await RelayService.Instance.JoinAllocationAsync(joinString);
                }
                catch (RelayServiceException e)
                {
                    Debug.LogError(e);
                    return null;
                }
            }

            void ConfigureRelayForTransport ()
            {
                Transport.SetRelayServerData(serverData: AllocationUtils.ToRelayServerData(relayAllocation, RelayConnectionType));
            }
        }

        private static UnityTransport Transport { get; }
        private static string RelayConnectionType { get; }
    }
}
