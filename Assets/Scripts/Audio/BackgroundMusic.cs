using UnityEngine;
using AudioSystem;

public class BackgroundMusic : MonoBehaviour
{
    public string musicName = "BattleshipBackground";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.PlayAudio(musicName);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
