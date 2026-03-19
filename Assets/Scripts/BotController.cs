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

    // ── Logic ───────────────────────────────────────────────

    private System.Collections.IEnumerator ProcessTurn ()
    {
        // Delay action để người chơi kịp nhìn
        yield return new WaitForSeconds(delayBetweenMoves);

        Vector2Int targetPos;

        // 1. Chế độ Target: Nếu có mục tiêu tiềm năng trong danh sách, bắn nó trước
        if (_potentialTargets.Count > 0)
        {
            // Lấy goal cuối cùng (stack behavior)
            var index = _potentialTargets.Count - 1;
            targetPos = _potentialTargets[index];
            _potentialTargets.RemoveAt(index);
        }
        // 2. Chế độ Hunt: Random
        else
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

        // Tấn công
        var hit = targetGrid.AttackCell(targetPos);
        Debug.Log($"Bot attacks {targetPos}: {(hit ? "HIT" : "MISS")}");

        // Nếu bắn trúng, thêm các ô xung quanh vào danh sách tiềm năng
        if (hit)
        {
            AddNeighborsToTargets(targetPos);

            // Bot được bắn tiếp nếu trúng? 
            // Tùy luật game. Nếu luật là "trúng được bắn tiếp", gọi MakeTurn() tiếp.
            // Nếu luật là lượt đổi, thì thôi.
            // BattleSceneLogic đang xử lý logic lượt, ở đây ta chỉ bắn 1 phát rồi callback?
            // Hiện tại BattleSceneLogic gọi EnemyTurn -> bắn 1 phát -> check sunk -> nếu hit thì gọi EnemyTurn tiếp.
            // Nên ta chỉ cần thực hiện 1 cú bắn và Logic game loop sẽ lo phần còn lại.

            // Update: BattleSceneLogic sẽ kiểm tra kết quả attack.
        }

        // Báo cho BattleSceneLogic biết bot đã bắn xong?
        // Hiện tại BattleSceneLogic gọi EnemyTurn và không chờ return.
        // Nhưng GridManager.AttackCell đã kích hoạt event/log state.

        // Cần gọi callback để BattleSceneLogic xử lý tiếp (CheckSunk, SwitchTurn...)
        // Vì logic game loop đang nằm cứng trong BattleSceneLogic, ta cần call ngược lại
        // hoặc BattleSceneLogic quan sát Grid.

        // Cách tốt nhất: BotController bắn xong -> BattleSceneLogic kiểm tra kết quả.
        // Nhưng BotController chạy Coroutine (async).
        // => Cần event hoặc callback.

        BattleSceneLogic.Instance.OnBotFinishedTurn(hit);
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

    private void AddNeighborsToTargets (Vector2Int pos)
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
    }

    private void Shuffle<T> (T[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var rnd = Random.Range(0, array.Length);
            (array[rnd], array[i]) = (array[i], array[rnd]);
        }
    }
}
