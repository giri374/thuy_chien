namespace Assets.OnlineMode.ConnectionMenu
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection;
    using UnityEngine;

    internal static class MatchHostConnectionMenu
    {
        static MatchHostConnectionMenu ()
        {
            MatchGuestCapacity = 1;

            const int numberOfMatchHost = 1;
            TotalPlayers = MatchGuestCapacity + numberOfMatchHost;
        }

        public static int MatchGuestCapacity { get; }
        public static int TotalPlayers { get; }

        public static string MatchId
        {
            get => _matchId;
            set
            {
                _matchId = value;
            }
        }

        public static async Task CreateMatch_Async ()
        {
            await EConnection.StartConnection_Async();
            if (!EConnection.ReadyToConnect())
            {
                Debug.LogError("Something's wrong, check the stack trace");
            }
        }

        private static string _matchId;
    }
}
