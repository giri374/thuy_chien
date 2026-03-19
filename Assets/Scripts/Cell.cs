using Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
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
    public Sprite emptySprite;
    public Sprite hitSprite;
    public Sprite previewSprite;

    [Header("Effects")]
    public GameObject hitAnimationPrefab;
    public GameObject missAnimationPrefab;

    [Header("Ship Reference")]
    public Ship occupyingShip;

    private GridManager gridManager;

    public void Initialize(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridManager = manager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResetCell();
    }

    public void ResetCell()
    {
        cellState = CellState.Unknown;
        occupyingShip = null;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        gridManager.OnCellClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        spriteRenderer.sprite = previewSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisual();
    }

    public void UpdateVisual()
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

            case CellState.Sunk:
                spriteRenderer.sprite = hitSprite;
                spriteRenderer.color = hitColor;
                break;
        }
    }

    public void SetOccupyingShip(Ship ship)
    {
        occupyingShip = ship;
    }

    public bool IsAvailable()
    {
        return occupyingShip == null;
    }
}