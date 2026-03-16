namespace Assets.OnlineMode.ConnectionMenu
{
    using UnityEngine;
    using UnityEngine.UI;

    public class MatchHostConnectionMenuViewController : MonoBehaviour
    {
        [SerializeField]
        private Text _matchIdTextController;

        private void OnEnable ()
        {
            CreateMatch_Async();
        }

        private async void CreateMatch_Async ()
        {
            await MatchHostConnectionMenu.CreateMatch_Async();
            JoinString = MatchHostConnectionMenu.MatchId;
        }

        private string JoinString
        {
            set => _matchIdTextController.text = value;
        }
    }
}
