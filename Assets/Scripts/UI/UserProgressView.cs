using TMPro;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject chứa Slider — hiển thị experience / ExpToNextLevel.
/// </summary>
public class UserProgressView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject progressPanel;

    private void Start ()
    {
        // Nếu data đã sẵn sàng (load local xong) → cập nhật ngay
        if (ProgressManager.Instance != null && ProgressManager.Instance.IsReady)
        {
            Refresh();
        }

    }

    private void OnEnable ()
    {
        // Poll nhẹ để bắt kịp lúc cloud sync xong
        InvokeRepeating(nameof(Refresh), 0f, 0.5f);
    }

    private void OnDisable () => CancelInvoke(nameof(Refresh));

    /// <summary>Kéo dữ liệu mới nhất từ ProgressManager và cập nhật Slider.</summary>
    public void Refresh ()
    {
        if (ProgressManager.Instance == null || !ProgressManager.Instance.IsReady)
        {
            return;
        }

        var data = ProgressManager.Instance.Data;

        progressText.text = $"Level {data.level} - Exp: {data.experience} / {data.ExpToNextLevel} - Gold: {data.gold} ";

    }

}