using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script quản lý tàu trong game
/// </summary>
public class Ship : MonoBehaviour
{
    [Header("Ship Data")]
    public ShipData shipData;

    [Header("Grid Info")]
    public Vector2Int gridPosition; // Vị trí góc trái dưới của tàu
    public bool isHorizontal = true; // True: ngang, False: dọc

    [Header("Occupied Cells")]
    public List<Cell> occupiedCells = new List<Cell>();

    [Header("Hit Tracking")]
    private int hitCount = 0;

    [Header("Visual")]
    public SpriteRenderer shipSpriteRenderer;
    private bool isVisible = false;

    public void Initialize(ShipData data)
    {
        shipData = data;

        if (shipSpriteRenderer == null)
            shipSpriteRenderer = GetComponent<SpriteRenderer>();

        // Ẩn tàu ban đầu (chỉ hiện khi bị đánh chìm)
        SetVisible(false);
    }

    /// <summary>
    /// Xoay tàu 90 độ
    /// </summary>
    public void Rotate()
    {
        isHorizontal = !isHorizontal;

        if (transform != null)
        {
            transform.rotation = Quaternion.Euler(0, 0, isHorizontal ? 0 : 90);
        }
    }

    /// <summary>
    /// Lấy kích thước tàu dựa theo hướng hiện tại
    /// </summary>
    public Vector2Int GetSize()
    {
        if (shipData == null) return Vector2Int.one;

        return isHorizontal ? shipData.size : new Vector2Int(shipData.size.y, shipData.size.x);
    }

    /// <summary>
    /// Lấy danh sách offset ô mà tàu chiếm, tính từ (0,0).
    /// Caller cộng thêm origin để ra vị trí tuyệt đối.
    /// </summary>
    public List<Vector2Int> GetOccupiedPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int size = GetSize();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                positions.Add(new Vector2Int(x, y)); // offset thuần, không cộng gridPosition
            }
        }

        return positions;
    }

    /// <summary>
    /// Set các ô mà tàu đang chiếm
    /// </summary>
    public void SetOccupiedCells(List<Cell> cells)
    {
        occupiedCells = cells;

        foreach (Cell cell in occupiedCells)
        {
            cell.SetOccupyingShip(this);
        }
    }

    /// <summary>
    /// Nhận đòn tấn công vào 1 ô
    /// </summary>
    public void TakeHit(Cell hitCell)
    {
        hitCount++;

        // Kiểm tra nếu tàu bị chìm
        if (IsSunk())
        {
            OnSunk();
        }
    }

    /// <summary>
    /// Kiểm tra tàu đã bị chìm chưa
    /// </summary>
    public bool IsSunk()
    {
        return hitCount >= occupiedCells.Count;
    }

    /// <summary>
    /// Xử lý khi tàu bị chìm
    /// </summary>
    private void OnSunk()
    {
        // Hiện hình ảnh tàu
        SetVisible(true);

        Debug.Log($"{shipData.shipName} đã bị chìm!");
    }

    /// <summary>
    /// Hiện/ẩn sprite của tàu
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (shipSpriteRenderer != null)
        {
            shipSpriteRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// Đặt vị trí tàu trên grid
    /// </summary>
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }
}