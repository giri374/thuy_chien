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

    private void Start ()
    {
        mainCamera = Camera.main;
        gridManager = FindObjectOfType<GridManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalPosition = transform.position;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (gridManager == null)
        {
            Debug.LogError("ShipPlacement: GridManager not found!");
        }
    }

    // ── Click để xoay ─────────────────────────────────────────

    public void OnPointerClick (PointerEventData eventData)
    {
        if (isDragging)
        {
            return;
        }

        ship?.Rotate();

        if (isPlaced)
        {
            RemoveShipFromGrid();
            TryPlaceAtCurrentSnap();
        }
    }

    // ── Drag ──────────────────────────────────────────────────

    public void OnBeginDrag (PointerEventData eventData)
    {
        isDragging = true;

        if (!isPlaced)
        {
            originalPosition = transform.position;
        }

        if (isPlaced)
        {
            RemoveShipFromGrid();
        }
    }

    public void OnDrag (PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        var snapPos = GetSnapPosition(eventData.position);
        currentSnapPos = snapPos;

        SnapShipToGrid(snapPos);
        UpdatePreviewColor(snapPos);
    }

    public void OnEndDrag (PointerEventData eventData)
    {
        isDragging = false;
        ResetColor();

        var success = TryPlaceAtCurrentSnap();

        if (!success)
        {
            transform.position = originalPosition;
            Debug.Log("Cannot place ship here — returned to original position.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private Vector2Int GetSnapPosition (Vector2 screenPos)
    {
        var worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        var cellPos = gridManager.grid.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
    }

    private void SnapShipToGrid (Vector2Int snapPos)
    {
        if (gridManager == null)
        {
            return;
        }

        var snappedWorld = gridManager.grid.CellToWorld(new Vector3Int(snapPos.x, snapPos.y, 0));
        snappedWorld.z = 0f;

        if (ship != null && !ship.isHorizontal)
        {
            snappedWorld.x += gridManager.grid.cellSize.x;
        }

        transform.position = snappedWorld;
    }

    private bool TryPlaceAtCurrentSnap()
    {
        if (gridManager == null)
        {
            return false;
        }

        // Use Board logic from GridManager
        if (!gridManager.CanPlaceShip(ship, currentSnapPos))
        {
            return false;
        }

        var success = gridManager.PlaceShip(ship, currentSnapPos);

        if (success)
        {
            isPlaced = true;

            // ── Lưu vào GameManager (thay thế SetupSceneUIManager.Instance cũ) ──
            SetupSceneLogic.Instance?.SaveShipPlacement(ship);

            Debug.Log($"Ship placed at {currentSnapPos}");
        }

        return success;
    }

    private void UpdatePreviewColor (Vector2Int snapPos)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        var canPlace = gridManager != null && gridManager.CanPlaceShip(ship, snapPos);
        spriteRenderer.color = canPlace ? validColor : invalidColor;
    }

    private void ResetColor ()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void RemoveShipFromGrid()
    {
        if (ship?.occupiedCells == null)
        {
            return;
        }

        // Use GridManager's Board reference
        gridManager?.RemoveShip(ship);
        isPlaced = false;
    }

    /// <summary>
    /// Gọi từ SetupSceneUIManager.OnReset() — tháo khỏi lưới,
    /// trả tàu về vị trí ban đầu và reset rotation về ngang.
    /// </summary>
    public void ResetToOrigin ()
    {
        if (isPlaced)
        {
            RemoveShipFromGrid();
        }

        // Reset rotation về ngang nếu đang dọc
        if (ship != null && !ship.isHorizontal)
        {
            ship.Rotate();
        }

        transform.position = originalPosition;
        ResetColor();
    }

    /// <summary>
    /// Dùng cho auto/random placement từ SetupSceneUIManager.
    /// </summary>
    public bool TryPlaceAt (Vector2Int origin, bool horizontal)
    {
        if (gridManager == null || ship == null)
        {
            return false;
        }

        if (isPlaced)
        {
            RemoveShipFromGrid();
        }

        // Đảm bảo hướng khớp trước khi check vị trí hợp lệ
        if (ship.isHorizontal != horizontal)
        {
            ship.Rotate();
        }

        currentSnapPos = origin;
        var success = TryPlaceAtCurrentSnap();
        if (!success)
        {
            transform.position = originalPosition;
        }

        return success;
    }
}
