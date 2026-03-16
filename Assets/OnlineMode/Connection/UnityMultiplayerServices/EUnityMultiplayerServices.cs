namespace Assets.OnlineMode.Connection.UnityMultiplayerServices
{
    using System.Threading.Tasks;
    using Unity.Services.Authentication;
    using Unity.Services.Core;
    using UnityEngine;

    internal static class EUnityMultiplayerServices
    {
        public static async Task StartRelay_Async (string joinString)
        {
            await InitializeUnityServicesAndSignIn();
            await UnityRelay.StartRelay_Async(joinString);
        }

        public static async Task StartRelay_Async ()
        {
            await InitializeUnityServicesAndSignIn();
            await UnityRelay.StartRelay_Async();
        }

        private static async Task InitializeUnityServicesAndSignIn ()
        {
            await InitializeUnityServices_Async();
            await SignInAuthenticationService_Async();

            static async Task InitializeUnityServices_Async ()
            {
                if (UnityServices.State != ServicesInitializationState.Uninitialized)
                {
                    return;
                }

                try
                {
                    await UnityServices.InitializeAsync();
                }
                catch
                {
                    Debug.LogError("Something's wrong, check the stack trace");
                }
            }

            static async Task SignInAuthenticationService_Async ()
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    return;
                }

                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch
                {
                    Debug.LogError("Something's wrong, check the stack trace");
                }
            }
        }
    }
}
