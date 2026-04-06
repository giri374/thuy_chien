using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    public PlayerProgressData Data { get; private set; } = new PlayerProgressData();
    public bool IsReady { get; private set; } = false;

    private void Awake ()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start ()
    {
        // Load local ngay lập tức → UI hiển thị được ngay, kể cả offline
        Data = PlayerCloudSave.Instance.LoadLocal(); // <-- gọi thẳng, không async
        IsReady = true;

        // Sau đó mới thử connect UGS và ghi đè nếu cloud mới hơn
        await TryInitUGSAndSync();
    }

    private async Task TryInitUGSAndSync ()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Cloud load thành công → ghi đè local data
            var cloudData = await PlayerCloudSave.Instance.LoadAsync();
            Data = cloudData;
            Debug.Log($"[ProgressManager] Synced from cloud: Level {Data.level}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ProgressManager] Running offline: {e.Message}");
            // Data đã được load từ local ở Start() → không cần làm gì thêm
        }
    }

    // ── Public API ─────────────────────────────────────────────

    public void AddGold (int amount) => Data.AddGold(amount);
    public void AddExperience (int amount) => Data.AddExp(amount);

    public async Task SaveProgress ()
    {
        // PlayerCloudSave.SaveAsync tự xử lý local + cloud + dirty flag
        await PlayerCloudSave.Instance.SaveAsync(Data);
    }

    /// <summary>
    /// Gọi khi app focus trở lại hoặc reconnect mạng
    /// </summary>
    public async Task TrySyncToCloud ()
    {
        await PlayerCloudSave.Instance.TrySyncIfDirty(Data);
    }

#if UNITY_EDITOR
    private void Update ()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) { AddGold(10); AddExperience(20); Debug.Log($"[DEV] Gold: {Data.gold} | EXP: {Data.experience} | Level: {Data.level}"); }
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S)) { _ = SaveProgress(); Debug.Log("[DEV] Saved!"); }
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R)) { Data = new PlayerProgressData(); Debug.Log("[DEV] Reset data!"); }
    }
#endif
}