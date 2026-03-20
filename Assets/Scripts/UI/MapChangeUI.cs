using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapChangeUI : MonoBehaviour
{
    public Image mapImage;
    public Sprite normalMapSprite;
    public Sprite AdvancedMapSprite;

    [SerializeField] private Button openWeaponPanelButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start ()
    {
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.gameMap)
            {
                case GameMap.NormalMap:

                    // Cài đặt UI cho Map 1
                    mapImage.sprite = normalMapSprite;
                    openWeaponPanelButton.gameObject.SetActive(false);


                    break;

                case GameMap.AdvancedMap:
                    // Cài đặt UI cho Map 2
                    mapImage.sprite = AdvancedMapSprite;
                    openWeaponPanelButton.gameObject.SetActive(true);  // Hiển thị nút mở panel vũ khí
                    break;


                default:
                    Debug.LogWarning("Unknown map type: " + GameManager.Instance.gameMap);
                    break;
            }
        }

        // Update is called once per frame
        void Update ()
        {

        }
    }
}
