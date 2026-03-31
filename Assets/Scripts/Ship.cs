using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Ship : MonoBehaviour
{
    [Header("Ship Data")]
    public ShipData shipData;

    [Header("Grid Info")]
    public Vector2Int gridPosition;
    public bool isHorizontal = true;

    [Header("Occupied Cells")]
    public List<Cell> occupiedCells = new List<Cell>();

    [Header("Hit Tracking")]
    private int hitCount;

    [Header("Visual")]
    public SpriteRenderer shipSpriteRenderer;
    private bool isVisible;

    public void Initialize (ShipData data)
    {
        shipData = data;
        shipSpriteRenderer = GetComponent<SpriteRenderer>();
        SetVisible(false);
    }

    public void Rotate ()
    {
        isHorizontal = !isHorizontal;
        transform.rotation = Quaternion.Euler(0, 0, isHorizontal ? 0 : 90);
    }

    public Vector2Int GetSize ()
    {
        if (shipData == null)
        {
            return Vector2Int.one;
        }

        return isHorizontal
            ? shipData.size
            : new Vector2Int(shipData.size.y, shipData.size.x);
    }

    public void TakeHit (Cell hitCell)
    {
        hitCount++;

        if (IsSunk())
        {
            OnSunk();
        }
    }

    public bool IsSunk ()
    {
        return hitCount >= occupiedCells.Count;
    }

    private void OnSunk ()
    {
        SetVisible(true);
        Debug.Log($"{shipData.shipName} đã bị chìm!");
    }

    public void SetVisible (bool visible)
    {
        isVisible = visible;

        if (shipSpriteRenderer != null)
        {
            shipSpriteRenderer.enabled = visible;
        }
    }

    public List<Vector2Int> GetOccupiedPositions ()
    {
        var positions = new List<Vector2Int>();
        var size = GetSize();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }

        return positions;
    }

    public void SetOccupiedCells (List<Cell> cells)
    {
        occupiedCells = cells;

        foreach (var cell in occupiedCells)
        {
            cell.SetOccupyingShip(this);
        }
    }

    public void SetGridPosition (Vector2Int position)
    {
        gridPosition = position;
    }
}