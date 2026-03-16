namespace Assets.OnlineMode.ConnectionMenu
{
    using System.Threading.Tasks;
    using Assets.Commons.YesNoMenuHandlers;
    using UnityEngine;
    using UnityEngine.UI;

    public class MatchGuestConnectionMenuViewController : MonoBehaviour
    {
        [SerializeField]
        private Text _matchIdTextController;
        [SerializeField]
        private Button _joinMatchButton;

        [SerializeField]
        private YesNoMenuViewController _yesNoMenuViewController;

        private void Awake ()
        {
            _joinMatchButton.onClick.AddListener(ConfirmJoinMatch);
        }

        private string JoinString => _matchIdTextController.text.ToUpper();

        private void ConfirmJoinMatch ()
        {
            _yesNoMenuViewController.ConfirmBeforeExecuting(JoinMatch);
            async Task JoinMatch ()
            {
                gameObject.SetActive(false);

                await MatchGuestConnectionMenu.ConnectToMatch_Async(JoinString);
            }
        }
    }
}
