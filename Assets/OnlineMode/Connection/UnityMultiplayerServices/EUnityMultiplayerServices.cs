namespace Assets.OnlineMode.Connection.UnityMultiplayerServices
{
    using System.Threading.Tasks;
    using Assets.OnlineMode.Connection;
    using Unity.Services.Authentication;
    using Unity.Services.Core;
    using UnityEngine;

    internal static class EUnityMultiplayerServices
    {
        public static async Task<bool> StartRelay_Async (string joinString)
        {
            if (!await InitializeUnityServicesAndSignIn())
            {
                return false;
            }

            return await UnityRelay.StartRelay_Async(joinString);
        }

        public static async Task<bool> StartRelay_Async ()
        {
            if (!await InitializeUnityServicesAndSignIn())
            {
                return false;
            }

            return await UnityRelay.StartRelay_Async();
        }

        private static async Task<bool> InitializeUnityServicesAndSignIn ()
        {
            if (!await InitializeUnityServices_Async())
            {
                return false;
            }

            return await SignInAuthenticationService_Async();

            static async Task<bool> InitializeUnityServices_Async ()
            {
                if (UnityServices.State != ServicesInitializationState.Uninitialized)
                {
                    return true;
                }

                try
                {
                    await UnityServices.InitializeAsync();
                    return true;
                }
                catch
                {
                    Debug.LogError("Something's wrong, check the stack trace");
                    ConnectionErrorState.Set("Failed to initialize Unity Services.");
                    return false;
                }
            }

            static async Task<bool> SignInAuthenticationService_Async ()
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    return true;
                }

                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    return true;
                }
                catch
                {
                    Debug.LogError("Something's wrong, check the stack trace");
                    ConnectionErrorState.Set("Authentication failed. Please try again.");
                    return false;
                }
            }
        }
    }
}
