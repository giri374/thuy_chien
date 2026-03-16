namespace Assets.Commons.YesNoMenuHandlers
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    internal class YesNoMenu
    {
        public void ConfirmYes (Action doIfChoseYes)
        {
            doIfChoseYes?.Invoke();

            if (doIfChoseYes == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
            }
        }

        public async Task ConfirmYes_Async (Func<Task> doIfChoseYes_Async)
        {
            await doIfChoseYes_Async?.Invoke();

            if (doIfChoseYes_Async == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
            }
        }
    }
}
