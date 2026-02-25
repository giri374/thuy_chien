using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Ship Reference")]
    public Ship occupyingShip; // Tàu đang chiếm ô này (nếu có)

    private GridManager gridManager;
    private bool isInteractable = true;

    public void Initialize(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridManager = manager;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        ResetCell();
    }

    public void ResetCell()
    {
        cellState = CellState.Unknown;
        occupyingShip = null;
        UpdateVisual();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable)
        {
            Debug.Log($"[Cell] Blocked: isInteractable=false");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError($"[Cell] gridManager is NULL on {gridPosition}!");
            return;
        }

        gridManager.OnCellClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        if (cellState == CellState.Unknown && spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;

        UpdateVisual();
    }

    /// <summary>
    /// Cập nhật visual dựa trên state hiện tại
    /// </summary>
    public void UpdateVisual()
    {
        if (spriteRenderer == null) return;

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
    /// Đánh dấu ô bị tấn công
    /// </summary>
    /// <returns>True nếu trúng tàu, False nếu trượt</returns>
    public bool Attack()
    {
        if (cellState != CellState.Unknown)
            return false; // Đã tấn công rồi

        if (occupyingShip != null)
        {
            // Trúng tàu
            cellState = CellState.Hit;
            occupyingShip.TakeHit(this);
            UpdateVisual();
            return true;
        }
        else
        {
            // Trượt
            cellState = CellState.Empty;
            UpdateVisual();
            return false;
        }
    }

    /// <summary>
    /// Set ô này bị chiếm bởi tàu
    /// </summary>
    public void SetOccupyingShip(Ship ship)
    {
        occupyingShip = ship;
    }

    /// <summary>
    /// Kiểm tra ô có trống không (để đặt tàu)
    /// </summary>
    public bool IsAvailable()
    {
        return occupyingShip == null;
    }
}