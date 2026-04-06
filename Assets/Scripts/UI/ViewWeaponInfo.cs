using UnityEngine;
using UnityEngine.UI;

public class ViewWeaponInfo : MonoBehaviour
{
    [SerializeField] private GameObject PanelWeaponInfo1;
    [SerializeField] private GameObject PanelWeaponInfo2;
    [SerializeField] private GameObject PanelWeaponInfo3;
    [SerializeField] private GameObject PanelWeaponInfo4;
    [SerializeField] private GameObject PanelWeaponInfo5;
    [SerializeField] private Button WeponButton1;
    [SerializeField] private Button WeponButton2;
    [SerializeField] private Button WeponButton3;
    [SerializeField] private Button WeponButton4;
    [SerializeField] private Button WeponButton5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start ()
    {
        WeponButton1.onClick.AddListener(() => ShowWeaponInfo(1));
        WeponButton2.onClick.AddListener(() => ShowWeaponInfo(2));
        WeponButton3.onClick.AddListener(() => ShowWeaponInfo(3));
        WeponButton4.onClick.AddListener(() => ShowWeaponInfo(4));
        WeponButton5.onClick.AddListener(() => ShowWeaponInfo(5));
    }
    void ShowWeaponInfo (int weaponNumber)
    {
        // Ẩn tất cả panel trước khi hiển thị panel tương ứng
        PanelWeaponInfo1.SetActive(false);
        PanelWeaponInfo2.SetActive(false);
        PanelWeaponInfo3.SetActive(false);
        PanelWeaponInfo4.SetActive(false);
        PanelWeaponInfo5.SetActive(false);

        // Hiển thị panel tương ứng với nút được nhấn
        switch (weaponNumber)
        {
            case 1:
                PanelWeaponInfo1.SetActive(true);
                break;
            case 2:
                PanelWeaponInfo2.SetActive(true);
                break;
            case 3:
                PanelWeaponInfo3.SetActive(true);
                break;
            case 4:
                PanelWeaponInfo4.SetActive(true);
                break;
            case 5:
                PanelWeaponInfo5.SetActive(true);
                break;
            default:
                Debug.LogWarning("Invalid weapon number: " + weaponNumber);
                break;
        }
    }

    // Update is called once per frame
    void Update ()
    {

    }
}
