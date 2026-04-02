namespace Assets.OnlineMode.ConnectionMenu
{
    using System;
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection;
    using Unity.Services.Relay;
    using UnityEngine;

    internal static class MatchGuestConnectionMenu
    {
        public static async Task ConnectToMatch_Async (string matchId)
        {
            ConnectionErrorState.Clear();

            try
            {
                await EConnection.StartConnection_Async(matchId);
                if (!EConnection.ReadyToConnect())
                {
                    ConnectionErrorState.SetIfEmpty("Failed to connect. Please check join code.");
                    Debug.LogWarning("Guest connect failed.");
                }
            }
            catch (RelayServiceException e)
            {
                Debug.LogWarning($"[Relay] {e.Message}");
                ConnectionErrorState.Set("Join code not found. Please check and try again.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ConnectionErrorState.Set("Connection failed unexpectedly. Please retry.");
            }
        }
    }
}
