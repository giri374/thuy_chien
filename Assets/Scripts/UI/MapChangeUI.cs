using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapChangeUI : MonoBehaviour
{
    // [SerializeField] private Image mapImage;
    // [SerializeField] private Sprite normalMapSprite;
    // [SerializeField] private Sprite AdvancedMapSprite;
    [SerializeField] private Image Map2;
    [SerializeField] private Image Map3;
    [SerializeField] private GameObject CP1;
    [SerializeField] private GameObject CP2;

    [SerializeField] private Button openWeaponPanelButton;
    [SerializeField] private GameObject weaponSelect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start ()
    {
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.gameMap)
            {
                case GameMap.NormalMap:

                    // Cài đặt UI cho Map 1
                    // mapImage.sprite = normalMapSprite;
                    Map2.gameObject.SetActive(false);
                    Map3.gameObject.SetActive(false);
                    openWeaponPanelButton.gameObject.SetActive(false);
                    weaponSelect.SetActive(false);
                    CP1.SetActive(false);
                    CP2.SetActive(false);
                    break;

                case GameMap.AdvancedMap:
                    // Cài đặt UI cho Map 2
                    // mapImage.sprite = AdvancedMapSprite;
                    Map2.gameObject.SetActive(true);
                    Map3.gameObject.SetActive(false);
                    openWeaponPanelButton.gameObject.SetActive(true);  // Hiển thị nút mở panel vũ khí
                    weaponSelect.SetActive(true);
                    CP1.SetActive(true);
                    CP2.SetActive(true);
                    break;


                default:
                    Debug.LogWarning("Unknown map type: " + GameManager.Instance.gameMap);
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update ()
    {

    }
}
