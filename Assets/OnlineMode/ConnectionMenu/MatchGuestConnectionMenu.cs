namespace Assets.OnlineMode.ConnectionMenu
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection;
    using UnityEngine;

    internal static class MatchGuestConnectionMenu
    {
        public static async Task ConnectToMatch_Async (string matchId)
        {
            await EConnection.StartConnection_Async(matchId);
            if (!EConnection.ReadyToConnect())
            {
                Debug.LogError("Something's wrong, check the stack trace");
            }
        }
    }
}
