using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using UnityEngine;

public class PlayerCloudSave : MonoBehaviour
{
    public static PlayerCloudSave Instance { get; private set; }

    private const string CLOUD_KEY = "player_progress";
    private const string LOCAL_KEY = "player_progress_local";
    private const string DIRTY_KEY = "player_progress_dirty";

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

    // ── Save ───────────────────────────────────────────────────

    /// <summary>
    /// Luôn ghi local trước, rồi thử cloud.
    /// Nếu cloud thất bại → đánh dấu dirty để sync sau.
    /// </summary>
    public async Task SaveAsync (PlayerProgressData data)
    {
        SaveLocal(data);                    // ghi local ngay lập tức

        var cloudOk = await TrySaveCloud(data);
        SetDirty(!cloudOk);                 // dirty = true nếu cloud thất bại

        Debug.Log($"[PlayerCloudSave] Saved local ✓ | Cloud: {(cloudOk ? "✓" : "pending...")}");
    }

    // ── Load ───────────────────────────────────────────────────

    /// <summary>
    /// Ưu tiên cloud. Nếu không có mạng thì đọc local.
    /// </summary>
    public async Task<PlayerProgressData> LoadAsync ()
    {
        // Thử cloud trước
        var cloudData = await TryLoadCloud();
        if (cloudData != null)
        {
            SaveLocal(cloudData);           // đồng bộ local với cloud
            SetDirty(false);
            return cloudData;
        }

        // Fallback: đọc từ PlayerPrefs
        Debug.Log("[PlayerCloudSave] Offline → loading from local cache.");
        return LoadLocal();
    }

    // ── Sync dirty data khi có mạng trở lại ───────────────────

    public async Task<bool> TrySyncIfDirty (PlayerProgressData currentData)
    {
        if (!IsDirty())
        {
            return true;        // không cần sync
        }

        var ok = await TrySaveCloud(currentData);
        if (ok)
        {
            SetDirty(false);
            Debug.Log("[PlayerCloudSave] Dirty data synced to cloud ✓");
        }
        return ok;
    }

    // ── Local helpers ──────────────────────────────────────────

    private void SaveLocal (PlayerProgressData data)
    {
        PlayerPrefs.SetString(LOCAL_KEY, JsonConvert.SerializeObject(data));
        PlayerPrefs.Save();
    }

    public PlayerProgressData LoadLocal ()     // bỏ private, đổi thành public
    {
        var json = PlayerPrefs.GetString(LOCAL_KEY, null);
        if (string.IsNullOrEmpty(json))
        {
            return new PlayerProgressData();
        }

        try
        {
            return JsonConvert.DeserializeObject<PlayerProgressData>(json);
        }
        catch
        {
            return new PlayerProgressData();
        }
    }

    private bool IsDirty () => PlayerPrefs.GetInt(DIRTY_KEY, 0) == 1;
    private void SetDirty (bool dirty)
    {
        PlayerPrefs.SetInt(DIRTY_KEY, dirty ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Cloud helpers ──────────────────────────────────────────

    private async Task<bool> TrySaveCloud (PlayerProgressData data)
    {
        try
        {
            var payload = new Dictionary<string, object>
                { { CLOUD_KEY, JsonConvert.SerializeObject(data) } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerCloudSave] Cloud save failed: {e.Message}");
            return false;
        }
    }

    private async Task<PlayerProgressData> TryLoadCloud ()
    {
        try
        {
            var keys = new HashSet<string> { CLOUD_KEY };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(CLOUD_KEY, out var item))
                return JsonConvert.DeserializeObject<PlayerProgressData>(item.Value.GetAsString());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerCloudSave] Cloud load failed (offline?): {e.Message}");
        }
        return null;
    }
}