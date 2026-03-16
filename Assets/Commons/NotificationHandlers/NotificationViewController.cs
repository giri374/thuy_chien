namespace Assets.Commons.NotificationHandlers
{
    using UnityEngine;
    using UnityEngine.UI;

    public class NotificationViewController : MonoBehaviour
    {
        public void ShowNotification (string notification)
        {
            gameObject.SetActive(true);
            NotificationContent = notification;
        }

        [SerializeField]
        private Text _notificationContentController;
        [SerializeField]
        private Button _buttonUnderstood;

        private void Awake ()
        {
            _buttonUnderstood.onClick.AddListener(OnClickedButtonCloseNotification);

            void OnClickedButtonCloseNotification ()
            {
                gameObject.SetActive(false);
            }
        }

        private string NotificationContent
        {
            set => _notificationContentController.text = value;
        }
    }
}
