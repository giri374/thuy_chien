# 🔧 Troubleshooting & Error Guide

## 🚨 Common Errors & Solutions

### 1. RelayServiceException: Join Code Invalid

**Lỗi**:
```
RelayServiceException: Join code "ABC123" is invalid
UnityEngine.Debug:LogError (object)
```

**Nguyên Nhân**:
- Mã tham gia sai format (không đúng 6 ký tự)
- Mã hết hạn (quá lâu chưa join)
- Relay server lỗi

**Giải Pháp**:
```csharp
// 1. Validate format
string joinCode = inputField.text.Trim();
if (joinCode.Length != 6)
{
    Debug.LogError("Join code must be 6 characters");
    return;
}

// 2. Retry với delay
await System.Threading.Tasks.Task.Delay(1000);
await MatchGuestConnectionMenu.ConnectToMatch_Async(joinCode);

// 3. Check internet connection
if (!Application.internetReachability == NetworkReachability.NotReachable)
{
    Debug.LogError("No internet connection");
    return;
}
```

**Phòng Tránh**:
- Sao chép mã chính xác từ Host
- Tham gia trong vòng 5-10 phút sau khi Host tạo
- Kiểm tra kết nối internet

---

### 2. "Something's wrong, check the stack trace"

**Lỗi**:
```
"Something's wrong, check the stack trace"
```

**Nguyên Nhân**:
Lỗi chung từ UnityRelay hoặc EUnityMultiplayerServices

**Giải Pháp**:
```csharp
// Thêm chi tiết logging
public static async Task DebugStartRelay_Async()
{
    try
    {
        Debug.Log("1. Initializing Unity Services...");
        await UnityServices.InitializeAsync();
        Debug.Log("✅ Services initialized");
        
        Debug.Log("2. Signing in...");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"✅ Signed in as: {AuthenticationService.Instance.PlayerId}");
        
        Debug.Log("3. Creating Relay allocation...");
        var allocation = await RelayService.Instance.CreateAllocationAsync(1);
        Debug.Log($"✅ Allocation created: {allocation.AllocationId}");
        
        Debug.Log("4. Getting join code...");
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log($"✅ Join code: {joinCode}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"❌ Error: {ex.GetType().Name}: {ex.Message}");
        Debug.LogException(ex);
    }
}
```

**Phòng Tránh**:
- Check các prerequisites trước
- Verify Unity Services configuration
- Check network connectivity

---

### 3. NullReferenceException: NetworkManager.Singleton is null

**Lỗi**:
```
NullReferenceException: Object reference not set to an instance of an object
Assets.OnlineMode.Connection.UnityRelay..cctor()
```

**Nguyên Nhân**:
- NetworkManager chưa được khởi tạo
- Singleton timeout

**Giải Pháp**:
```csharp
// ❌ Bad - gọi quá sớm
void Start()
{
    var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
}

// ✅ Good - chờ NetworkManager ready
async void OnEnable()
{
    // Chờ NetworkManager initialize
    int retries = 0;
    while (NetworkManager.Singleton == null && retries < 10)
    {
        await System.Threading.Tasks.Task.Delay(100);
        retries++;
    }
    
    if (NetworkManager.Singleton == null)
    {
        Debug.LogError("NetworkManager failed to initialize");
        return;
    }
    
    await EConnection.StartConnection_Async();
}
```

**Phòng Tránh**:
- Tạo NetworkManager GameObject đầu tiên
- Không gọi network code trong Awake()
- Chờ OnNetworkSpawn() để gọi RPC

---

### 4. Connection Hangs / Stuck on Loading

**Triệu Chứng**:
- UI stuck, không quay lại
- Không có error message
- Network không response

**Nguyên Nhân**:
- Network timeout
- Relay server không response
- Async không await đúng

**Giải Pháp**:
```csharp
// Thêm timeout
public static async Task<bool> StartConnectionWithTimeout_Async(
    string joinCode = null)
{
    var cts = new System.Threading.CancellationTokenSource(10000);  // 10 sec timeout
    
    try
    {
        if (joinCode == null)
        {
            await EConnection.StartConnection_Async();
        }
        else
        {
            await EConnection.StartConnection_Async(joinCode);
        }
        
        return EConnection.ReadyToConnect();
    }
    catch (System.OperationCanceledException)
    {
        Debug.LogError("Connection timeout - took more than 10 seconds");
        return false;
    }
}

// Sử dụng:
if (!await StartConnectionWithTimeout_Async())
{
    showErrorUI("Connection failed or timed out");
}
```

**Phòng Tránh**:
- Thêm timeout cho tất cả network operations
- Show loading UI với cancel button
- Log mỗi step của connection process

---

### 5. "IsConnectedClient returns false"

**Lỗi**:
```
if (!EConnection.StillConnectedToServer())
{
    // Client disconnected unexpectedly
}
```

**Nguyên Nhân**:
- Network disconnect
- Server shutdown
- Relay connection lost
- Client timeout

**Giải Pháp**:
```csharp
// Setup connection monitoring
public class NetworkConnectionMonitor : MonoBehaviour
{
    private float disconnectCheckInterval = 5f;
    private float timeSinceLastCheck = 0f;
    
    private void Update()
    {
        timeSinceLastCheck += Time.deltaTime;
        
        if (timeSinceLastCheck >= disconnectCheckInterval)
        {
            CheckConnection();
            timeSinceLastCheck = 0f;
        }
    }
    
    private void CheckConnection()
    {
        if (!EConnection.StillConnectedToServer())
        {
            Debug.LogWarning("Lost connection to server!");
            HandleDisconnection();
        }
    }
    
    private void HandleDisconnection()
    {
        // Option 1: Auto reconnect
        TryReconnect();
        
        // Option 2: Return to menu
        // SceneManager.LoadScene("MainMenu");
    }
    
    private void TryReconnect()
    {
        Debug.Log("Attempting to reconnect...");
        EConnection.Disconnect();
        // Re-trigger connection logic
    }
}
```

**Phòng Tránh**:
- Monitor connection status regularly
- Implement reconnect logic
- Save game state để phục hồi

---

### 6. RPC Không Được Nhận

**Triệu Chứng**:
- RPC không trigger
- Không có callback
- Silent failure

**Nguyên Nhân**:
- RPC gọi trước khi ready
- NetworkObject chưa spawn
- Permission issues
- SendTo parameter sai

**Giải Pháp**:
```csharp
// ❌ Wrong
void Start()
{
    EGameMatch.Singleton.MarkCell_ServerRPC(0, 0);  // May be null!
}

// ✅ Correct
public void SendMarkCell(int row, int col)
{
    // 1. Check connection
    if (!EConnection.ReadyToConnect())
    {
        Debug.LogError("Not connected!");
        return;
    }
    
    // 2. Check game instance
    if (EGameMatch.Singleton == null)
    {
        Debug.LogError("Game match not initialized!");
        return;
    }
    
    // 3. Send RPC
    EGameMatch.Singleton.MarkCell_ServerRPC(row, col);
}
```

**Debug RPC**:
```csharp
// Thêm logging trong RPC
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int rowIndex, int columnIndex, RpcParams rpcParams = default)
{
    Debug.Log($"[RPC] Received from client: {rpcParams.Receive.SenderClientId}");
    Debug.Log($"[RPC] Position: ({rowIndex}, {columnIndex})");
    
    if (InvalidToExecute())
    {
        Debug.LogWarning($"[RPC] Invalid execution");
        return;
    }
    
    Debug.Log("[RPC] Broadcasting to all clients...");
    MarkCell_ClientsAndHostRPC(rowIndex, columnIndex);
}

[Rpc(SendTo.Everyone)]
private void MarkCell_ClientsAndHostRPC(int rowIndex, int columnIndex)
{
    Debug.Log($"[RPC] All clients received: ({rowIndex}, {columnIndex})");
    board[rowIndex, columnIndex] = currentMark;
}
```

**Phòng Tránh**:
- Luôn check ReadyToConnect() trước RPC
- Luôn wait for OnNetworkSpawn()
- Validate parameters trong RPC

---

### 7. Multiple EGameMatch Instances

**Lỗi**:
```
Multiple NetworkBehaviours detected
Singleton pattern violation
```

**Nguyên Nhân**:
- Scene reload không cleanup
- Multiple prefabs instantiated
- Singleton logic lỗi

**Giải Pháp**:
```csharp
// Trong EGameMatch.Awake()
private void Awake()
{
    if (Singleton != null)
    {
        Debug.LogWarning($"Duplicate EGameMatch detected, destroying this one");
        Destroy(gameObject);
        return;
    }
    
    Singleton = this;
    // ... rest of setup
}
```

**Phòng Tránh**:
- Cleanup trước load scene mới
- Đảm bảo chỉ 1 EGameMatch prefab
- Network disconnect trước scene change

---

### 8. "IsPlayerTurn returns false"

**Lỗi**:
- RPC bị reject vì invalid turn
- Client không thể đánh

**Nguyên Nhân**:
- Logic xác định lượt sai
- Turn không được sync đúng
- Client info không match server

**Giải Pháp**:
```csharp
// Debug turn logic
private bool IsPlayerTurn(RpcParams rpcParams)
{
    ulong senderId = rpcParams.Receive.SenderClientId;
    
    Debug.Log($"[Turn Check] Sender: {senderId}");
    Debug.Log($"[Turn Check] Current turn owner: {CurrentTurnPlayer}");
    Debug.Log($"[Turn Check] Match started: {MatchStarted}");
    
    // Check 1: Match started?
    if (!MatchStarted)
    {
        Debug.LogWarning("[Turn Check] Match not started yet");
        return false;
    }
    
    // Check 2: Is sender's turn?
    if (AllPlayerClientIds.Length < 2)
    {
        Debug.LogWarning("[Turn Check] Not enough players");
        return false;
    }
    
    bool isHost = senderId == AllPlayerClientIds[0];
    bool isGuest = senderId == AllPlayerClientIds[1];
    
    if (!isHost && !isGuest)
    {
        Debug.LogWarning($"[Turn Check] Unknown client: {senderId}");
        return false;
    }
    
    // Check 3: Turn validation
    bool correctTurn = (CurrentTurnPlayer == senderId);
    Debug.Log($"[Turn Check] Correct turn: {correctTurn}");
    
    return correctTurn;
}
```

**Phòng Tránh**:
- Sync turn state qua NetworkVariable
- Validate trên server only
- Log debugging info

---

## 📊 Connection Checklist

### Pre-Connection
- [ ] Internet connection active
- [ ] Unity Services configured (Project ID, etc)
- [ ] Network Manager in scene
- [ ] EGameMatch in scene with NetworkObject

### During Connection
- [ ] Await async operations
- [ ] Check ReadyToConnect() after await
- [ ] Validate input format
- [ ] Handle RelayServiceException

### Post-Connection
- [ ] OnNetworkSpawn() called
- [ ] AllPlayerClientIds populated
- [ ] RPC can be sent
- [ ] Network state synced

### During Gameplay
- [ ] Monitor StillConnectedToServer()
- [ ] Handle RPC failures gracefully
- [ ] Log important events
- [ ] Show user feedback

---

## 🔍 Debugging Checklist

### Enable Verbose Logging
```csharp
// Trong EGameMatch hoặc connection code
#if UNITY_EDITOR
    private const bool DEBUG_LOG = true;
#else
    private const bool DEBUG_LOG = false;
#endif

void DebugLog(string message)
{
    if (DEBUG_LOG)
        Debug.Log($"[OnlineModule] {message}");
}
```

### Monitor Network Events
```csharp
void SetupNetworkDebug()
{
    NetworkManager nm = NetworkManager.Singleton;
    
    nm.OnServerStarted += () => Debug.Log("✅ Server started");
    nm.OnClientConnectedCallback += (id) => Debug.Log($"✅ Client {id} connected");
    nm.OnClientDisconnectCallback += (id) => Debug.Log($"❌ Client {id} disconnected");
    nm.OnTransportFailure += () => Debug.Log("❌ Transport failed");
}
```

### Use Network Simulator
```csharp
// Trong NetworkManager Inspector:
// Settings → Network Simulator
// - Enable: true
// - Delay: 100-200ms
// - Jitter: 20-50ms
// - Drop Rate: 1-5%
// - Max Frames: 10

// Test reconnection handling
```

---

## 📈 Performance Optimization

### Reduce RPC Frequency
```csharp
// ❌ Bad - send every frame
void Update()
{
    EOnlinePlayer.MarkCell(input);  // Too much!
}

// ✅ Good - send on input
void OnInputReceived(int row, int col)
{
    if (IsValidMove(row, col))
    {
        EOnlinePlayer.MarkCell((row, col));
    }
}
```

### Batch Updates
```csharp
// ❌ Bad - multiple RPC calls
for (int i = 0; i < 10; i++)
{
    EGameMatch.MarkCell_ServerRPC(row[i], col[i]);
}

// ✅ Good - single RPC with array
[Rpc(SendTo.Server)]
public void MarkCells_ServerRPC(int[] rows, int[] cols)
{
    // ...
}
```

### Use NetworkVariable for State
```csharp
// ❌ Bad - RPC không sync on late join
[Rpc(SendTo.Everyone)]
void UpdateGameState(GameState state)
{
    // Late joiners won't get old updates
}

// ✅ Good - NetworkVariable auto-syncs
private NetworkVariable<GameState> CurrentState = new();
// New clients auto-sync on connect
```

---

## 🎯 Best Practices Summary

| Do | Don't |
|----|-------|
| Await async operations | Fire and forget async calls |
| Check ReadyToConnect() | Assume connection success |
| Validate RPC params | Trust client input |
| Monitor disconnections | Ignore network errors |
| Log connection steps | Silent failures |
| Test with network sim | Only test on stable net |
| Use timeouts | Wait forever |
| Handle exceptions | Crash on error |
| Cleanup on disconnect | Leave dangling resources |
| Test edge cases | Assume happy path |

---

**Version**: 1.0
**Updated**: 2024
