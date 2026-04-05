using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponPanelRoll : MonoBehaviour
{
    [SerializeField] private GameObject weaponPanel;
    [SerializeField] private Button openPanelButton;
    [SerializeField] private Button closePanelButton;
    [SerializeField] private float animationDuration = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start ()
    {
        // Đảm bảo ban đầu scale Y = 0
        weaponPanel.transform.localScale = new Vector3(weaponPanel.transform.localScale.x, 0, weaponPanel.transform.localScale.z);
        openPanelButton.onClick.AddListener(OpenPanel);
        closePanelButton.onClick.AddListener(ClosePanel);
    }
    public void OpenPanel ()
    {
        transform.DOKill();
        weaponPanel.transform.DOScaleY(1f, animationDuration).SetEase(Ease.OutQuad);
    }
    public void ClosePanel ()
    {
        transform.DOKill();
        weaponPanel.transform.DOScaleY(0f, animationDuration).SetEase(Ease.InQuad);
    }
}
