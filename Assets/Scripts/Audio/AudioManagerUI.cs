using UnityEngine;
using UnityEngine.UI;
using AudioSystem;
public class AudioManagerUI : MonoBehaviour
{
    [Header("Music Buttons")]
    public Button btnMusicOn;
    public Button btnMusicOff;

    [Header("SFX Buttons")]
    public Button btnSfxOn;
    public Button btnSfxOff;

    void Start()
    {
        // Gán sự kiện cho các nút
        btnMusicOn.onClick.AddListener(() => OnMusicClick(true));
        btnMusicOff.onClick.AddListener(() => OnMusicClick(false));

        btnSfxOn.onClick.AddListener(() => OnSfxClick(true));
        btnSfxOff.onClick.AddListener(() => OnSfxClick(false));

        // Cập nhật trạng thái hiển thị nút ban đầu
        UpdateUI();
    }

    void OnMusicClick(bool isOn)
    {
        AudioManager.Instance.SetMusic(isOn);
        UpdateUI();
    }

    void OnSfxClick(bool isOn)
    {
        AudioManager.Instance.SetSfx(isOn);
        UpdateUI();
    }

    void UpdateUI()
    {

        // // Hoặc bạn có thể dùng SetActive để ẩn hẳn nút nếu muốn:
        btnMusicOn.gameObject.SetActive(!AudioManager.Instance.isMusicOn);
        btnMusicOff.gameObject.SetActive(AudioManager.Instance.isMusicOn);
        btnSfxOn.gameObject.SetActive(!AudioManager.Instance.isSfxOn);
        btnSfxOff.gameObject.SetActive(AudioManager.Instance.isSfxOn);
    }
}
