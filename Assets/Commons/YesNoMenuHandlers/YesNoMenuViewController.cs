namespace Assets.Commons.YesNoMenuHandlers
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UI;

    public class YesNoMenuViewController : MonoBehaviour
    {
        public void ConfirmBeforeExecuting (Action doIfChoseYes)
        {
            if (doIfChoseYes == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
                return;
            }

            _buttonYes.onClick.AddListener(OnClickedYes);

            ShowThisMenu();

            void OnClickedYes ()
            {
                StopReceivingPlayerInput();

                LogicalYesNoMenu.ConfirmYes(doIfChoseYes);

                CloseThisMenu();
            }
        }

        public void ConfirmBeforeExecuting (Func<Task> doIfChoseYes_Async)
        {
            if (doIfChoseYes_Async == null)
            {
                Debug.LogError("Something's wrong, check the stack trace");
                return;
            }

            _buttonYes.onClick.AddListener(OnClickedYes_Async);

            ShowThisMenu();

            async void OnClickedYes_Async ()
            {
                StopReceivingPlayerInput();

                await LogicalYesNoMenu.ConfirmYes_Async(doIfChoseYes_Async);

                CloseThisMenu();
            }
        }

        [SerializeField]
        private Button _buttonYes;
        [SerializeField]
        private Button _buttonNo;

        private void Awake ()
        {
            _buttonNo.onClick.AddListener(OnClickedNo);
            LogicalYesNoMenu = new YesNoMenu();

            void OnClickedNo ()
            {
                CloseThisMenu();
            }
        }

        private YesNoMenu LogicalYesNoMenu { get; set; }

        private void ShowThisMenu ()
        {
            _buttonYes.interactable = true;
            _buttonNo.interactable = true;
            gameObject.SetActive(true);
        }

        private void CloseThisMenu ()
        {
            gameObject.SetActive(false);
            _buttonYes.onClick.RemoveAllListeners();
        }

        private void StopReceivingPlayerInput ()
        {
            MakeAllButtonsNonClickable();

            void MakeAllButtonsNonClickable ()
            {
                _buttonYes.interactable = false;
                _buttonNo.interactable = false;
            }
        }
    }
}
