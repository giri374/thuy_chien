using UnityEngine;
using UnityEngine.UI;

public class LevelImageUpdater : MonoBehaviour
{
    [Header("Level Images (index = level - 1)")]
    public Sprite[] levelSprites;

    [Header("Target Image")]
    public Image levelImage;

    public void Start()
    {
        
        // Cập nhật hình ảnh level ngay khi bắt đầu
        UpdateLevelImage(ProgressManager.Instance.Data.level);
    }
    public void UpdateLevelImage(int level)
    {
        if (levelImage == null || levelSprites == null) return;

        int index = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);

        if (levelSprites[index] != null)
            levelImage.sprite = levelSprites[index];
    }
}