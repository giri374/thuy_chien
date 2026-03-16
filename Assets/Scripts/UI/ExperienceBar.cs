using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào GameObject chứa Slider — hiển thị experience / ExpToNextLevel.
/// </summary>
public class ExperienceSlider : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text labelText; // VD: "120 / 500 XP"  (có thể để null)

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

        float max = Mathf.Max(data.ExpToNextLevel, 1);        // tránh chia 0
        slider.value = Mathf.Clamp01(data.experience / max);

        if (labelText != null)
        {
            labelText.text = $"{data.experience} / {data.ExpToNextLevel}";
        }
    }
}