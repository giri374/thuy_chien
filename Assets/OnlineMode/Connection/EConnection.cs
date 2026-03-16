namespace Assets.OnlineMode.Connection
{
    using System.Threading.Tasks;
    using Unity.Netcode;

    internal static class EConnection
    {
        public static bool ReadyToConnect ()
        {
            return NetworkManager.Singleton.IsListening;
        }

        public static bool StillConnectedToServer ()
        {
            // IsConnectedClient == true => IsListening == true
            return NetworkManager.Singleton.IsConnectedClient;
        }

        public static async Task StartConnection_Async ()
        {
            await HostConnection.StartConnection_Async();
        }

        public static async Task StartConnection_Async (string joinString)
        {
            await ClientConnection.StartConnection_Async(joinString);
        }

        public static void Disconnect ()
        {
            if (InvalidToExecute())
            {
                return;
            }

            NetworkManager.Singleton.Shutdown();

            static bool InvalidToExecute ()
            {
                return !ReadyToConnect();
            }
        }
    }
}
