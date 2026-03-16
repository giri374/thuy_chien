namespace Assets.OnlineMode.ConnectionMenu
{
    using Assets.Commons.YesNoMenuHandlers;
    using UnityEngine;
    using UnityEngine.UI;

    public class MatchRoleMenuViewController : MonoBehaviour
    {
        private void Awake ()
        {
            _createMatchButton.onClick.AddListener(ConfirmShowMatchHostMenu);
            _joinMatchButton.onClick.AddListener(ShowMatchGuestMenu);
        }

        [SerializeField]
        private Button _createMatchButton;
        [SerializeField]
        private GameObject _matchHostMenu;

        [SerializeField]
        private Button _joinMatchButton;
        [SerializeField]
        private GameObject _matchGuestMenu;

        [SerializeField]
        private YesNoMenuViewController _yesNoMenuViewController;

        private void ShowMatchGuestMenu ()
        {
            gameObject.SetActive(false);
            _matchGuestMenu.SetActive(true);
        }

        private void ConfirmShowMatchHostMenu ()
        {
            _yesNoMenuViewController.ConfirmBeforeExecuting(ShowMatchHostMenu);

            void ShowMatchHostMenu ()
            {
                gameObject.SetActive(false);
                _matchHostMenu.SetActive(true);
            }
        }
    }
}
