using UnityEngine;
using Core.Models;
using AudioSystem;

/// <summary>
/// Maps Core.Board data → UI Cell views
/// Keeps cells in sync with board state
/// </summary>
public class GridView : MonoBehaviour
{
    [Header("Grid Reference")]
    public Grid grid;
    public int gridWidth = 10;
    public int gridHeight = 10;

    private Board logicBoard;
    private Cell[,] cellViews;
    
    private SpriteRenderer[] cellSprites;

    // ── Initialization ────────────────────────────────────────

    /// <summary>
    /// Initialize GridView with a Board instance
    /// </summary>
    public void Initialize(Board board, Cell[,] cells)
    {
        logicBoard = board;
        cellViews = cells;

        if (cellViews == null)
        {
            Debug.LogError("GridView.Initialize: cellViews is null!");
            return;
        }

        // Cache sprite renderers for performance
        cellSprites = new SpriteRenderer[gridWidth * gridHeight];
        var index = 0;
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                if (cellViews[x, y] != null)
                {
                    cellSprites[index] = cellViews[x, y].spriteRenderer;
                }
                index++;
            }
        }

        // Initial sync
        SyncFromBoard();
    }

    // ── Sync Logic ─────────────────────────────────────────────

    /// <summary>
    /// Update all cell views from board state
    /// </summary>
    public void SyncFromBoard()
    {
        if (logicBoard == null || cellViews == null)
        {
            return;
        }

        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var cellData = logicBoard.GetCell(new Vector2Int(x, y));
                RenderCell(cellData);
            }
        }
    }

    /// <summary>
    /// Update a single cell view
    /// </summary>
    public void RenderCell(CellData cellData)
    {
        var pos = cellData.position;
        if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
        {
            return;
        }

        var cell = cellViews[pos.x, pos.y];
        if (cell == null)
        {
            return;
        }

        UpdateCellVisual(cell, cellData);
    }

    /// <summary>
    /// Update cell visual based on CellData state
    /// </summary>
    private void UpdateCellVisual(Cell cell, CellData cellData)
    {
        if (cell.spriteRenderer == null)
        {
            return;
        }

        // Map Core.Models.CellState to Cell.CellState
        CellState displayState = (CellState)(int)cellData.state;
        cell.cellState = displayState;
        cell.UpdateVisual();
    }

    // ── Animation Support ────────────────────────────────────────

    /// <summary>
    /// Show hit/miss animation at position
    /// </summary>
    public void ShowAttackAnimation(Vector2Int position, bool isHit)
    {
        var pos = cellViews[position.x, position.y];
        if (pos == null)
        {
            return;
        }

        var offset = new Vector3(0.5f, 0.5f, 0);
        var spawnPos = pos.transform.position + offset;

        if (isHit)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAudio("Hit");
            }

            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.GetEffect(EffectType.Hit, spawnPos);
            }
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAudio("Miss");
            }

            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.GetEffect(EffectType.Miss, spawnPos);
            }
        }
    }

    // ── Helper Methods ───────────────────────────────────────────

    /// <summary>
    /// Get board reference
    /// </summary>
    public Board GetBoard()
    {
        return logicBoard;
    }

    /// <summary>
    /// Get cell view at position
    /// </summary>
    public Cell GetCellView(Vector2Int position)
    {
        if (position.x < 0 || position.x >= gridWidth || position.y < 0 || position.y >= gridHeight)
        {
            return null;
        }

        return cellViews[position.x, position.y];
    }
}
