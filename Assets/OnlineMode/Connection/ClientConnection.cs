namespace Assets.OnlineMode.Connection
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection.UnityMultiplayerServices;
    using Unity.Netcode;
    using UnityEngine;

    internal static class ClientConnection
    {
        public static async Task StartConnection_Async (string joinString)
        {
            if (InvalidToExecute())
            {
                return;
            }

            bool relayReady = await EUnityMultiplayerServices.StartRelay_Async(joinString);
            if (!relayReady)
            {
                Debug.LogWarning("Join failed. Relay setup was not completed.");
                return;
            }

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("Failed to start client.");
            }

            bool InvalidToExecute ()
            {
                return EConnection.ReadyToConnect() || JoinStringDoesntSeemValid(joinString);
            }
        }

        private static bool JoinStringDoesntSeemValid (string joinString)
        {
            return string.IsNullOrEmpty(joinString) || (joinString.Length != 6);
        }
    }
}
