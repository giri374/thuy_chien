using AudioSystem;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CellState
{
    Unknown,    // Chưa khám phá
    Empty,      // Trống (đã bắn nhưng trượt) - hiện dấu O
    Hit         // Bắn trúng - hiện dấu X
}
[RequireComponent(typeof(SpriteRenderer))]


/// <summary>
/// Script quản lý từng ô trong lưới
/// </summary>
public class Cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cell Info")]
    public Vector2Int gridPosition;
    public CellState cellState = CellState.Unknown;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color emptyColor = Color.gray;
    public Color hitColor = Color.red;

    [Header("State Sprites")]
    public Sprite emptySprite; // Sprite dấu O
    public Sprite hitSprite;   // Sprite dấu X
    public Sprite previewSprite; // Sprite khi hover 

    [Header("Effects")]
    public GameObject hitAnimationPrefab;
    public GameObject missAnimationPrefab;

    [Header("Ship Reference")]
    public Ship occupyingShip; // Tàu đang chiếm ô này (nếu có)

    private GridManager gridManager;

    public void Initialize (Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridManager = manager;
        spriteRenderer = GetComponent<SpriteRenderer>();

        ResetCell();
    }

    public void ResetCell ()
    {
        cellState = CellState.Unknown;
        occupyingShip = null;
        UpdateVisual();
    }


    public void OnPointerClick (PointerEventData eventData)
    {

        gridManager.OnCellClicked(this);
    }

    public void OnPointerEnter (PointerEventData eventData)
    {

        spriteRenderer.sprite = previewSprite;

    }

    public void OnPointerExit (PointerEventData eventData)
    {

        UpdateVisual();
    }


    /// <summary>
    /// Đánh dấu ô bị tấn công
    /// </summary>
    /// <returns>True nếu trúng tàu, False nếu trượt</returns>
    public bool Attack ()
    {
        if (cellState != CellState.Unknown)
        {
            return false; // Đã tấn công rồi
        }

        var offset = new Vector3(0.5f, 0.5f, 0);
        var spawnPos = transform.position + offset;

        if (occupyingShip != null)
        {
            // Trúng tàu
            cellState = CellState.Hit;

            AudioManager.Instance.PlayAudio("Hit");
            occupyingShip.TakeHit(this);
            EffectPoolManager.Instance.GetEffect(EffectType.Hit, spawnPos);
            UpdateVisual();
            return true;
        }
        else
        {
            // Trượt
            cellState = CellState.Empty;
            AudioManager.Instance.PlayAudio("Miss");
            EffectPoolManager.Instance.GetEffect(EffectType.Miss, spawnPos);
            UpdateVisual();
            return false;
        }
    }

    /// <summary>
    /// Cập nhật visual dựa trên state hiện tại
    /// </summary>
    public void UpdateVisual ()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (cellState)
        {
            case CellState.Unknown:
                spriteRenderer.sprite = null;
                spriteRenderer.color = normalColor;
                break;

            case CellState.Empty:
                spriteRenderer.sprite = emptySprite;
                spriteRenderer.color = emptyColor;
                break;

            case CellState.Hit:
                spriteRenderer.sprite = hitSprite;
                spriteRenderer.color = hitColor;
                break;
        }
    }


    /// <summary>
    /// Set ô này bị chiếm bởi tàu
    /// </summary>
    public void SetOccupyingShip (Ship ship)
    {
        occupyingShip = ship;
    }

    /// <summary>
    /// Kiểm tra ô có trống không (để đặt tàu)
    /// </summary>
    public bool IsAvailable ()
    {
        return occupyingShip == null;
    }

}