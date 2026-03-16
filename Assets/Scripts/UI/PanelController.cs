using UnityEngine;
using DG.Tweening;
public class PanelController : MonoBehaviour
{
    public RectTransform panelTransform; // Kéo Panel vào đây
    public float duration = 0.5f;        // Thời gian di chuyển

    [SerializeField] private float hiddenYPosition = 400f;
    [SerializeField] private float visibleYPosition = 0f;
    public void ShowPanel()
    {
        // Di chuyển Panel về vị trí Y = 0 (giữa màn hình)
        // Dùng DOAnchorPosY cho UI RectTransform
        panelTransform.DOAnchorPosY(visibleYPosition, duration).SetEase(Ease.OutBack);
    }

    public void HidePanel()
    {
        // Di chuyển Panel ngược lại lên trên (ví dụ Y = 600)
        panelTransform.DOAnchorPosY(hiddenYPosition, duration).SetEase(Ease.InBack);
    }
}