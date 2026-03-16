using AudioSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handler để quay lại MenuScene.
/// Có thể attach script này vào button "Back to Menu" và gọi BackToMenu() từ onClick event.
/// </summary>
public class SoundSettingUI : MonoBehaviour
{
    public Button musicOnButton;
    public Button musicOffButton;
    public Button soundEffectOnButton;
    public Button soundEffectOffButton;
    private void Start ()
    {
        if (musicOnButton != null)
        {
            musicOnButton.onClick.AddListener(MusicOn);
        }

        if (musicOffButton != null)
        {
            musicOffButton.onClick.AddListener(MusicOff);
        }

        if (soundEffectOnButton != null)
        {
            soundEffectOnButton.onClick.AddListener(SoundEffectsOn);
        }

        if (soundEffectOffButton != null)
        {
            soundEffectOffButton.onClick.AddListener(SoundEffectsOff);
        }

        if (AudioManager.Instance == null)
        {
            Debug.Log("AudioManager.Instance is null in SoundSettingUI.Start!");
        }
    }

    public void MusicOn ()
    {
        AudioManager.Instance.SetMusic(true);
        Debug.Log("Music turned on.");
    }
    public void MusicOff () => AudioManager.Instance.SetMusic(false);
    public void SoundEffectsOn () => AudioManager.Instance.SetSfx(true);
    public void SoundEffectsOff () => AudioManager.Instance.SetSfx(false);
}
