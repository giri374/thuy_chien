using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Kéo thả tàu với snap vào ô lưới real-time
/// </summary>
public class ShipPlacement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("References")]
    public Ship ship;

    [Header("Preview Visual")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    // ── Private ───────────────────────────────────────────────
    private GridManager gridManager;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    private Vector3 originalPosition;
    private Color originalColor;
    private bool isDragging = false;
    private bool isPlaced = false;

    private Vector2Int currentSnapPos;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start()
    {
        mainCamera = Camera.main;
        gridManager = FindObjectOfType<GridManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalPosition = transform.position;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (gridManager == null)
            Debug.LogError("ShipPlacement: GridManager not found!");
    }

    // ── Click để xoay ─────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        ship?.Rotate();

        if (isPlaced)
        {
            RemoveShipFromGrid();
            TryPlaceAtCurrentSnap();
        }
    }

    // ── Drag ──────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        if (!isPlaced)
            originalPosition = transform.position;

        if (isPlaced)
            RemoveShipFromGrid();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2Int snapPos = GetSnapPosition(eventData.position);
        currentSnapPos = snapPos;

        SnapShipToGrid(snapPos);
        UpdatePreviewColor(snapPos);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        ResetColor();

        bool success = TryPlaceAtCurrentSnap();

        if (!success)
        {
            transform.position = originalPosition;
            Debug.Log("Cannot place ship here — returned to original position.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private Vector2Int GetSnapPosition(Vector2 screenPos)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Vector3Int cellPos = gridManager.grid.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
    }

    private void SnapShipToGrid(Vector2Int snapPos)
    {
        if (gridManager == null) return;

        Vector3 snappedWorld = gridManager.grid.CellToWorld(new Vector3Int(snapPos.x, snapPos.y, 0));
        snappedWorld.z = 0f;

        if (ship != null && !ship.isHorizontal)
            snappedWorld.x += gridManager.grid.cellSize.x;

        transform.position = snappedWorld;
    }

    private bool TryPlaceAtCurrentSnap()
    {
        if (gridManager == null) return false;
        if (!gridManager.CanPlaceShip(ship, currentSnapPos)) return false;

        bool success = gridManager.PlaceShip(ship, currentSnapPos);

        if (success)
        {
            isPlaced = true;

            // ── Lưu vào GameManager (thay thế SetupSceneManager.Instance cũ) ──
            SetupSceneManager.Instance?.SaveShipPlacement(ship);

            Debug.Log($"Ship placed at {currentSnapPos}");
        }

        return success;
    }

    private void UpdatePreviewColor(Vector2Int snapPos)
    {
        if (spriteRenderer == null) return;

        bool canPlace = gridManager != null && gridManager.CanPlaceShip(ship, snapPos);
        spriteRenderer.color = canPlace ? validColor : invalidColor;
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void RemoveShipFromGrid()
    {
        if (ship?.occupiedCells == null) return;

        foreach (Cell cell in ship.occupiedCells)
            cell?.SetOccupyingShip(null);

        ship.occupiedCells.Clear();
        gridManager?.ships.Remove(ship);

        isPlaced = false;
    }

    /// <summary>
    /// Gọi từ SetupSceneManager.OnReset() — tháo khỏi lưới,
    /// trả tàu về vị trí ban đầu và reset rotation về ngang.
    /// </summary>
    public void ResetToOrigin()
    {
        if (isPlaced)
            RemoveShipFromGrid();

        // Reset rotation về ngang nếu đang dọc
        if (ship != null && !ship.isHorizontal)
            ship.Rotate();

        transform.position = originalPosition;
        ResetColor();
    }
}