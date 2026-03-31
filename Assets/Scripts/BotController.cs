using System.Collections.Generic;
using Core.Models;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("References")]
    public GridManager myGrid;       // Lưới của bot (để đặt tàu)
    public GridManager targetGrid;   // Lưới của đối thủ (để tấn công)
    public ShipListData shipListData;

    [Header("Settings")]
    public float delayBetweenMoves = 1f;

    // ── AI State ─────────────────────────────────────────────

    // Danh sách các ô tiềm năng cần bắn (chế độ Target)
    private readonly List<Vector2Int> _potentialTargets = new List<Vector2Int>();

    // TrackShip mode: Theo dõi tàu được xác định từ 2 lượt bắn trúng liên tiếp
    private Dictionary<int, ShipTracker> _activeShips = new Dictionary<int, ShipTracker>();
    private HashSet<int> _sunkShips = new HashSet<int>();

    // ── Public API ───────────────────────────────────────────

    /// <summary>
    /// Đặt tàu ngẫu nhiên cho bot
    /// </summary>
    public void PlaceShipsRandomly ()
    {
        if (myGrid == null || shipListData == null)
        {
            Debug.LogWarning("BotController: Missing references for Ship Placement.");
            return;
        }

        // Reset grid trước khi đặt
        myGrid.ResetGrid(); // Đảm bảo lưới trống

        var maxAttempts = 100;

        foreach (var data in shipListData.ships)
        {
            var isplaced = false;
            var attempts = 0;

            while (!isplaced && attempts < maxAttempts)
            {
                attempts++;

                // Random hướng và vị trí
                var horizontal = Random.value > 0.5f;
                var x = Random.Range(0, myGrid.gridWidth);
                var y = Random.Range(0, myGrid.gridHeight);
                var pos = new Vector2Int(x, y);

                // Tạo object tàu tạm
                var obj = Instantiate(data.shipPrefab, myGrid.transform);
                var ship = obj.GetComponent<Ship>();

                if (ship == null)
                {
                    Destroy(obj);
                    continue;
                }

                // Setup tàu
                ship.Initialize(data);
                if (!horizontal)
                {
                    ship.Rotate();
                }

                // Thử đặt vào grid
                if (myGrid.PlaceShip(ship, pos))
                {
                    ship.SetVisible(false); // Ẩn tàu của bot

                    // Disable interaction for enemy ships so they don't block clicks on the grid
                    var collider = ship.GetComponent<Collider2D>();
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }

                    if (ship.TryGetComponent<ShipPlacement>(out var placementScript))
                    {
                        Destroy(placementScript);
                    }

                    isplaced = true;
                }
                else
                {
                    // Nếu không đặt được thì hủy
                    Destroy(obj);
                }
            }

            if (!isplaced)
            {
                Debug.LogWarning($"BotController: Could not place {data.shipName} after {maxAttempts} attempts.");
            }
        }
    }

    /// <summary>
    /// Thực hiện lượt bắn của bot
    /// </summary>
    public void MakeTurn ()
    {
        StartCoroutine(ProcessTurn());
    }

    // ── TrackShip Mode ──────────────────────────────────────

    /// <summary>
    /// Lớp để theo dõi một tàu đã xác định từ 2 lượt bắn trúng liên tiếp
    /// </summary>
    private class ShipTracker
    {
        public int shipId;                          // ID tàu duy nhất
        public List<Vector2Int> hits;               // Danh sách tất cả các ô trúng đạn
        public Vector2Int direction;                // Hướng tàu (dương hoặc âm trên 1 trục)
        public Vector2Int end1;                     // Đầu thứ nhất của đoạn thẳng
        public Vector2Int end2;                     // Đầu thứ hai của đoạn thẳng
        public int currentEndIndex = 0;             // Chỉ số đầu hiện tại (0 = end1, 1 = end2)
        public bool isConfirmed = false;            // True khi có 2+ trúng đạn

        public ShipTracker (int id)
        {
            shipId = id;
            hits = new List<Vector2Int>();
        }
    }

    // ── Logic ───────────────────────────────────────────────

    private System.Collections.IEnumerator ProcessTurn ()
    {
        // Delay action để người chơi kịp nhìn
        yield return new WaitForSeconds(delayBetweenMoves);

        Vector2Int targetPos = Vector2Int.zero;  // Initialize to default value
        bool foundTarget = false;

        // 1. Chế độ TrackShip: Nếu có tàu đang được theo dõi, ưu tiên bắn theo tàu đó
        if (_activeShips.Count > 0)
        {
            targetPos = GetNextTrackShipTarget();
            if (targetPos != Vector2Int.one * -1)  // -1 là giá trị đặc biệt "không tìm thấy"
            {
                foundTarget = true;
            }
        }

        // 2. Chế độ Target: Nếu chưa tìm thấy mục tiêu TrackShip, bắn các ô tiềm năng
        if (!foundTarget && _potentialTargets.Count > 0)
        {
            // Lấy goal cuối cùng (stack behavior)
            var index = _potentialTargets.Count - 1;
            targetPos = _potentialTargets[index];
            _potentialTargets.RemoveAt(index);
            foundTarget = true;
        }

        // 3. Chế độ Hunt: Nếu cả TrackShip và Target hết, bắn random
        if (!foundTarget)
        {
            targetPos = GetRandomCell();
        }

        // Kiểm tra tính hợp lệ (tránh bắn lại ô cũ)
        var cell = targetGrid.GetCell(targetPos);

        // Nếu ô này đã bắn rồi (không còn Unknown), thử lại ngay trong frame này (hoặc đệ quy)
        // Tuy nhiên để an toàn, ta lặp tìm ô hợp lệ nếu đang ở chế độ Random
        // Nếu là Target mode mà ô đã bắn rồi thì bỏ qua.
        if (cell == null || cell.cellState != CellState.Unknown)
        {
            // Nếu list target hết hoặc điểm không hợp lệ, thử random lại
            if (_potentialTargets.Count == 0)
            {
                // Thử tìm ô khác
                MakeTurn();
            }
            else
            {
                // Thử mục tiêu tiếp theo trong stack
                MakeTurn();
            }
            yield break;
        }

        // Execute attack using the command system
        // Bot attacks with NormalShot weapon by default
        BattleSceneLogic.Instance.ExecuteBotAttackCommand(WeaponType.NormalShot, targetPos);

        // Note: The result handling is now done via HandleAttackResult callback in BattleSceneLogic
        // We don't call OnBotFinishedTurn anymore from here
    }

    private Vector2Int GetRandomCell ()
    {
        // Random cơ bản
        // Có thể tối ưu bằng cách lưu list các ô Unknown để không phải random lại nhiều lần
        int x, y;
        var maxTries = 100;
        do
        {
            x = Random.Range(0, targetGrid.gridWidth);
            y = Random.Range(0, targetGrid.gridHeight);
            maxTries--;
        }
        while (targetGrid.GetCell(x, y) != null && targetGrid.GetCell(x, y).cellState != CellState.Unknown && maxTries > 0);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Lấy mục tiêu tiếp theo từ chế độ TrackShip
    /// Trả về Vector2Int.one * -1 nếu không tìm thấy mục tiêu hợp lệ
    /// </summary>
    private Vector2Int GetNextTrackShipTarget ()
    {
        // Lặp qua các tàu đang được theo dõi và tìm mục tiêu hợp lệ
        foreach (var shipTracker in _activeShips.Values)
        {
            // Cố gắng bắn đầu hiện tại
            for (int attempt = 0; attempt < 2; attempt++)  // Tối đa 2 lần (độ dài tàu)
            {
                Vector2Int currentEnd = shipTracker.currentEndIndex == 0 ? shipTracker.end1 : shipTracker.end2;
                var cell = targetGrid.GetCell(currentEnd);

                // Nếu ô hợp lệ (Unknown), bắn nó
                if (cell != null && cell.cellState == CellState.Unknown)
                {
                    return currentEnd;
                }

                // Ô này đã bị bắn (hoặc ngoài biên), di chuyển endpoint vào và chuẩn bị cho lần sau
                if (shipTracker.currentEndIndex == 0)
                {
                    shipTracker.end1 += shipTracker.direction;
                }
                else
                {
                    shipTracker.end2 -= shipTracker.direction;
                }

                // Chuyển sang đầu kia
                shipTracker.currentEndIndex = (shipTracker.currentEndIndex == 0) ? 1 : 0;
            }

            // Cả hai đầu đều bị chặn - tàu này có thể đã bị chìm
            // (sẽ bị xóa khỏi _activeShips bởi CheckSunkShips)
        }

        return Vector2Int.one * -1;  // Không tìm thấy mục tiêu hợp lệ
    }

    public void AddNeighborsToTargets (Vector2Int pos)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // Shuffle dirs để bot bắn không quá máy móc
        Shuffle(dirs);

        foreach (var dir in dirs)
        {
            var checkPos = pos + dir;
            var cell = targetGrid.GetCell(checkPos);

            if (cell != null && cell.cellState == CellState.Unknown)
            {
                // Chỉ thêm nếu chưa có trong list
                if (!_potentialTargets.Contains(checkPos))
                {
                    _potentialTargets.Add(checkPos);
                }
            }
        }

        // Sau khi thêm neighbors, kiểm tra xem có 2 ô liên tiếp bị trúng không (để kích hoạt TrackShip)
        TryDetectShipDirection(pos);
    }

    /// <summary>
    /// Kiểm tra xem có 2 ô bị trúng liên tiếp không để xác định hướng tàu
    /// Nếu tìm thấy, kích hoạt chế độ TrackShip cho tàu đó
    /// </summary>
    private void TryDetectShipDirection (Vector2Int lastHitPos)
    {
        // Kiểm tra các ô lân cận của lastHitPos xem có ô bị trúng không
        Vector2Int[] checkDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in checkDirs)
        {
            var checkPos = lastHitPos + dir;
            var cell = targetGrid.GetCell(checkPos);

            if (cell != null && (cell.cellState == CellState.Hit || cell.cellState == CellState.Sunk))
            {
                // Tìm thấy 1 ô khác bị trúng kế cận → xác định hướng tàu
                DetectAndTrackShip(lastHitPos, checkPos);
                break;  // Chỉ cần tìm 1 ô liên tiếp là đủ để xác định hướng
            }
        }
    }

    /// <summary>
    /// Xác định hướng tàu dựa vào 2 ô bị trúng liên tiếp
    /// Kích hoạt chế độ TrackShip với tàu mới hoặc cập nhật tàu hiện có
    /// </summary>
    private void DetectAndTrackShip (Vector2Int hit1, Vector2Int hit2)
    {
        // Xác định hướng tàu từ delta của 2 ô
        Vector2Int delta = hit2 - hit1;
        Vector2Int direction = Vector2Int.zero;

        if (delta.x != 0)
        {
            // Tàu nằm ngang
            direction = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else if (delta.y != 0)
        {
            // Tàu nằm dọc
            direction = delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
        else
        {
            return;  // Không hợp lệ
        }

        // Cố gắng lấy actual shipId từ board
        int shipId = -1;
        Board board = targetGrid?.GetBoard();
        if (board != null)
        {
            var shipAtHit1 = board.GetShipAt(hit1);
            if (shipAtHit1.HasValue)
            {
                shipId = shipAtHit1.Value.shipId;
            }
            else
            {
                var shipAtHit2 = board.GetShipAt(hit2);
                if (shipAtHit2.HasValue)
                {
                    shipId = shipAtHit2.Value.shipId;
                }
            }
        }

        // Nếu không tìm được shipId từ board, tạo one dựa vào positions
        if (shipId == -1)
        {
            Vector2Int minPos = hit1.x < hit2.x || (hit1.x == hit2.x && hit1.y < hit2.y) ? hit1 : hit2;
            Vector2Int maxPos = hit1 == minPos ? hit2 : hit1;
            shipId = (minPos.x * 100 + minPos.y) * 100 + (maxPos.x * 100 + maxPos.y);
        }

        if (!_activeShips.ContainsKey(shipId) && !_sunkShips.Contains(shipId))
        {
            // Tạo ShipTracker mới
            var tracker = new ShipTracker(shipId);
            tracker.hits.Add(hit1);
            tracker.hits.Add(hit2);
            tracker.direction = direction;
            tracker.isConfirmed = true;

            // Tính toán 2 đầu của tàu
            // Đầu 1: từ hit1 trong hướng ngược lại
            // Đầu 2: từ hit2 trong hướng hiện tại
            tracker.end1 = hit1 - direction;
            tracker.end2 = hit2 + direction;

            _activeShips[shipId] = tracker;

            Debug.Log($"[BotController] TrackShip activated for ship {shipId} | Direction: {direction} | End1: {tracker.end1}, End2: {tracker.end2}");
        }
        else if (_activeShips.ContainsKey(shipId))
        {
            // Cập nhật tàu hiện có với hit mới
            var tracker = _activeShips[shipId];
            if (!tracker.hits.Contains(hit1)) tracker.hits.Add(hit1);
            if (!tracker.hits.Contains(hit2)) tracker.hits.Add(hit2);

            // Xác minh hướng (nên giữ nguyên nếu không xung đột)
            // Có thể mở rộng endpoints nếu hit nằm ngoài range hiện tại
        }
    }

    private void Shuffle<T> (T[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var rnd = Random.Range(0, array.Length);
            (array[rnd], array[i]) = (array[i], array[rnd]);
        }
    }

    /// <summary>
    /// Xóa tàu khỏi danh sách theo dõi khi tàu đó bị chìm
    /// Được gọi từ BattleSceneLogic khi phát hiện tàu bị chìm
    /// </summary>
    public void RemoveTrackedShip (int shipId)
    {
        if (_activeShips.ContainsKey(shipId))
        {
            _activeShips.Remove(shipId);
            _sunkShips.Add(shipId);
            Debug.Log($"[BotController] Removed tracked ship {shipId} (sunk)");
        }
    }

    /// <summary>
    /// Xóa mục tiêu tiềm năng của tàu khỏi list Target khi tàu bị chìm
    /// </summary>
    public void RemoveTargetPositions (List<Vector2Int> positions)
    {
        foreach (var pos in positions)
        {
            _potentialTargets.Remove(pos);
        }
    }
}
