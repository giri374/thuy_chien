namespace Assets.OnlineMode.Connection
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection.UnityMultiplayerServices;
    using Unity.Netcode;
    using UnityEngine;

    internal static class HostConnection
    {
        public static async Task StartConnection_Async ()
        {
            if (InvalidToExecute())
            {
                return;
            }

            bool relayReady = await EUnityMultiplayerServices.StartRelay_Async();
            if (!relayReady)
            {
                Debug.LogWarning("Host start aborted. Relay setup was not completed.");
                return;
            }

            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("Failed to start host.");
            }

            static bool InvalidToExecute ()
            {
                return EConnection.ReadyToConnect();
            }
        }
    }
}
