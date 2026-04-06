using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

/// <summary>
/// Gắn vào GameObject chứa Slider — hiển thị experience / ExpToNextLevel.
/// </summary>
public class UserProgressView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private TMP_InputField hackCodeInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button upLevelButton;

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

        // Setup hack code input
        if (hackCodeInput != null)
        {
            hackCodeInput.onEndEdit.AddListener(ProcessHackCode);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        if (upLevelButton != null)
        {
            upLevelButton.onClick.AddListener(() =>
            {
                UpLevel();
            });
        }
    }

    private void OnDisable ()
    {
        CancelInvoke(nameof(Refresh));

        // Remove listeners
        if (hackCodeInput != null)
        {
            hackCodeInput.onEndEdit.RemoveListener(ProcessHackCode);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        }

        if (upLevelButton != null)
        {
            upLevelButton.onClick.RemoveListener(() =>
            {
                UpLevel();
            });
        }
    }

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

    private void OnSubmitButtonClicked ()
    {
        if (hackCodeInput != null)
        {
            ProcessHackCode(hackCodeInput.text);
        }
    }



    private async void ProcessHackCode (string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Kiểm tra nếu bắt đầu bằng "winner"
        if (input.StartsWith("winner"))
        {
            string numberPart = input.Substring(6); // Lấy phần sau "winner"

            if (int.TryParse(numberPart, out int multiplier))
            {
                ProgressManager.Instance.AddGold(10 * multiplier);
                ProgressManager.Instance.AddExperience(20 * multiplier);
                await ProgressManager.Instance.SaveProgress();
                Debug.Log($"[HACK] Applied winner{multiplier}: Gold +{10 * multiplier}, EXP +{20 * multiplier}");

                // Clear input sau khi xử lý
                if (hackCodeInput != null)
                {
                    hackCodeInput.text = "";
                }
            }
            else
            {
                Debug.LogWarning($"[HACK] Invalid code format: {input}");
            }
        }
    }

    private async void UpLevel ()
    {
        ProgressManager.Instance.AddGold(50);
        ProgressManager.Instance.AddExperience(100);
        await ProgressManager.Instance.SaveProgress();
    }

}