
using UnityEngine;
using UnityEngine.UI;
using AudioSystem;

public class PlaySoundOnClick : MonoBehaviour
{

    void Start()
    {
        // Tìm tất cả Button hiện có trong Scene
        Button[] allButtons = FindObjectsOfType<Button>();
        Debug.Log($"Found {allButtons.Length} buttons");

        foreach (Button btn in allButtons)
        {
            // Thêm sự kiện phát âm thanh khi Click
            btn.onClick.AddListener(() => PlaySound());
        }
    }

    void PlaySound()
    {
        Debug.Log("PlaySound called");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudio("Click");
        }
        else
        {
            Debug.LogError("AudioManager.Instance is null!");
        }
    }
}
