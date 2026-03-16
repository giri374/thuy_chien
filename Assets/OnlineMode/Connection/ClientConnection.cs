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

            await EUnityMultiplayerServices.StartRelay_Async(joinString);
            NetworkManager.Singleton.StartClient();

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
