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

            await EUnityMultiplayerServices.StartRelay_Async();
            NetworkManager.Singleton.StartHost();

            static bool InvalidToExecute ()
            {
                return EConnection.ReadyToConnect();
            }
        }
    }
}
