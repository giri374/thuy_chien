using AudioSystem;
using DG.Tweening; // Thêm thư viện DOTween
using UnityEngine;
using UnityEngine.UI;

public class PlaySoundOnClick : MonoBehaviour
{
    [Header("Settings")]
    public float scaleTo = 0.9f;     // Tỉ lệ thu nhỏ
    public float duration = 0.1f;    // Thời gian hiệu ứng

    private void Start ()
    {
        var allButtons = FindObjectsOfType<Button>();

        foreach (var btn in allButtons)
        {
            // Lưu lại scale ban đầu để đảm bảo nút quay về đúng cỡ
            var originalScale = btn.transform.localScale;

            btn.onClick.AddListener(() =>
            {
                PlaySound();
                PlayClickEffect(btn.transform, originalScale);
            });
        }
    }

    private void PlayClickEffect (Transform target, Vector3 originalScale)
    {
        // Hiệu ứng: Thu nhỏ lại rồi phóng lớn về ban đầu (Yoyo)
        target.DOScale(originalScale * scaleTo, duration)
              .SetEase(Ease.OutQuad)
              .SetLoops(2, LoopType.Yoyo);
    }

    private void PlaySound ()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudio("Click");
        }
    }
}
