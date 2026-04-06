using UnityEngine;
using UnityEngine.UI;

public class LevelImageUpdater : MonoBehaviour
{
    [Header("Level Images (index = level - 1)")]
    public Sprite[] levelSprites;

    [Header("Target Image")]
    public Image levelImage;

    public void Start ()
    {
        // Cập nhật hình ảnh level ngay khi bắt đầu
        if (ProgressManager.Instance != null)
        {
            UpdateLevelImage(ProgressManager.Instance.Data.level);
        }
    }

    private void OnEnable ()
    {
        // Poll liên tục để cập nhật hình ảnh level
        InvokeRepeating(nameof(RefreshLevelImage), 0f, 0.5f);
    }

    private void OnDisable ()
    {
        CancelInvoke(nameof(RefreshLevelImage));
    }

    private void RefreshLevelImage ()
    {
        if (ProgressManager.Instance != null)
        {
            UpdateLevelImage(ProgressManager.Instance.Data.level);
        }
    }
    public void UpdateLevelImage (int level)
    {
        if (levelImage == null || levelSprites == null) return;

        int index = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);

        if (levelSprites[index] != null)
            levelImage.sprite = levelSprites[index];
    }
}