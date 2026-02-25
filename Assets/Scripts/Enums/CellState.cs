using UnityEngine;

/// <summary>
/// Trạng thái của mỗi ô trong lưới
/// </summary>
public enum CellState
{
    Unknown,    // Chưa khám phá
    Empty,      // Trống (đã bắn nhưng trượt) - hiện dấu O
    Hit         // Bắn trúng - hiện dấu X
}
