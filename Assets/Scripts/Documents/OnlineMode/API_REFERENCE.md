# 📚 API Reference - Module Online

## 📌 Quick Reference

### Entry Points

```csharp
// Main API cho Gameplay
namespace Assets.OnlineMode.OnlinePlayer
{
    EOnlinePlayer.MarkCell((row, col));  // Send player action
    EOnlinePlayer.LeaveMatch();            // Disconnect
}

// Connection Management
namespace Assets.OnlineMode.Connection
{
    EConnection.ReadyToConnect();                       // bool
    EConnection.StillConnectedToServer();               // bool
    await EConnection.StartConnection_Async();          // Host
    await EConnection.StartConnection_Async(joinCode);  // Guest
    EConnection.Disconnect();                           // Disconnect
}

// Match Creation/Join
namespace Assets.OnlineMode.ConnectionMenu
{
    await MatchHostConnectionMenu.CreateMatch_Async();
    await MatchGuestConnectionMenu.ConnectToMatch_Async(matchId);
    
    string matchId = MatchHostConnectionMenu.MatchId;  // Get created match ID
    int capacity = MatchHostConnectionMenu.MatchGuestCapacity;  // 1
    int totalPlayers = MatchHostConnectionMenu.TotalPlayers;    // 2 (Host + Guest)
}

// Game State Access
namespace Assets.OnlineMode.GameMatch
{
    EGameMatch.Singleton;  // Access the network game instance
}
```

---

## 🔧 Detailed API Documentation

### 1. EConnection - Connection Facade

**Namespace**: `Assets.OnlineMode.Connection`

**File**: `Connection/EConnection.cs`

**Mô tả**: Static class quản lý toàn bộ lifecycle kết nối

#### Methods

##### `ReadyToConnect() : bool`

```csharp
public static bool ReadyToConnect()
```

**Mô tả**: Kiểm tra xem NetworkManager có khởi tạo và sẵn sàng không

**Return**: 
- `true` - NetworkManager đang listening (đã kết nối)
- `false` - NetworkManager chưa sẵn sàng

**Ví dụ**:
```csharp
if (EConnection.ReadyToConnect())
{
    Debug.Log("✅ Ready to send RPC calls");
}
else
{
    Debug.Log("⏳ Still connecting...");
}
```

**Thực hiện**:
```csharp
return NetworkManager.Singleton.IsListening;
```

---

##### `StillConnectedToServer() : bool`

```csharp
public static bool StillConnectedToServer()
```

**Mô tả**: Kiểm tra kết nối hiện tại với server

**Return**:
- `true` - Client vẫn kết nối (hoặc là Host)
- `false` - Mất kết nối

**Ví dụ**:
```csharp
if (!EConnection.StillConnectedToServer())
{
    Debug.LogError("Lost connection, attempting reconnect...");
    // Thực hiện logic reconnect
}
```

**Thực hiện**:
```csharp
return NetworkManager.Singleton.IsConnectedClient;
```

---

##### `StartConnection_Async() : Task`

```csharp
public static async Task StartConnection_Async()
```

**Mô tả**: Khởi động kết nối **HOST** (máy chủ)

**Parameters**: Không có

**Return**: `Task` (async operation)

**Throws**: 
- `RelayServiceException` - Nếu Relay Service thất bại

**Ví dụ**:
```csharp
try
{
    await EConnection.StartConnection_Async();
    
    if (EConnection.ReadyToConnect())
    {
        Debug.Log("✅ Host started successfully");
        string matchId = MatchHostConnectionMenu.MatchId;
        Debug.Log($"Share this ID: {matchId}");
    }
}
catch (Exception ex)
{
    Debug.LogError($"❌ Failed to start host: {ex.Message}");
}
```

**Quy trình**:
1. Kiểm tra chưa kết nối (`EConnection.ReadyToConnect()` = false)
2. Gọi `HostConnection.StartConnection_Async()`
3. `HostConnection` gọi `EUnityMultiplayerServices.StartRelay_Async()`
4. Services khởi tạo + Auth + Relay tạo Allocation
5. Lấy JoinCode → lưu vào `MatchHostConnectionMenu.MatchId`
6. `NetworkManager.StartHost()` bắt đầu listening

---

##### `StartConnection_Async(string joinString) : Task`

```csharp
public static async Task StartConnection_Async(string joinString)
```

**Mô tả**: Khởi động kết nối **CLIENT/GUEST** (khách)

**Parameters**:
- `joinString` (string): Mã tham gia (6 ký tự từ Host)

**Return**: `Task` (async operation)

**Throws**:
- `RelayServiceException` - Nếu mã không hợp lệ
- `ArgumentException` - Nếu format mã sai

**Ví dụ**:
```csharp
string matchId = inputField.text.Trim();

try
{
    await EConnection.StartConnection_Async(matchId);
    
    if (EConnection.ReadyToConnect())
    {
        Debug.Log($"✅ Connected to match: {matchId}");
        // Scene sẽ tự động load
    }
}
catch (RelayServiceException ex)
{
    Debug.LogError($"❌ Invalid match ID or server error: {ex.Message}");
}
```

**Quy trình**:
1. Kiểm tra chưa kết nối
2. Gọi `ClientConnection.StartConnection_Async(joinString)`
3. `ClientConnection` validate format (6 ký tự)
4. Gọi `EUnityMultiplayerServices.StartRelay_Async(joinString)`
5. Services khởi tạo + Auth + Relay join với code
6. Lấy Host endpoint từ Relay
7. `NetworkManager.StartClient()` kết nối đến Host

---

##### `Disconnect() : void`

```csharp
public static void Disconnect()
```

**Mô tả**: Ngắt kết nối từ server/relay

**Parameters**: Không có

**Return**: void

**Side Effects**:
- Gọi `NetworkManager.Singleton.Shutdown()`
- Đóng kết nối UDP
- Thông báo cho Host/Clients
- Cleanup NetworkObjects

**Ví dụ**:
```csharp
// Trong Exit button handler
public void OnExitButtonClicked()
{
    EConnection.Disconnect();
    
    // Sau khi disconnect:
    // - Scene resources được cleanup
    // - Load Menu scene
}
```

**Thực hiện**:
```csharp
public static void Disconnect()
{
    if (InvalidToExecute())  // Check !ReadyToConnect()
    {
        return;
    }
    
    NetworkManager.Singleton.Shutdown();
}
```

---

### 2. HostConnection - Host-Specific Connection

**Namespace**: `Assets.OnlineMode.Connection`

**File**: `Connection/HostConnection.cs`

**Mô tả**: Static class xử lý logic khởi động Host

#### Methods

##### `StartConnection_Async() : Task`

```csharp
internal static async Task StartConnection_Async()
```

**Visibility**: Internal (chỉ dùng qua `EConnection`)

**Mô tả**: Khởi động Host và lấy join code

**Return**: `Task` (async operation)

**Quy trình chi tiết**:
```
1. InvalidToExecute() check
   ├─ Nếu đã ReadyToConnect() → return early
   └─ Nếu chưa → continue
   
2. EUnityMultiplayerServices.StartRelay_Async()
   ├─ Init Unity Services
   ├─ Sign in anonymously
   └─ Call UnityRelay.StartRelay_Async() (no params)
   
3. UnityRelay.StartRelay_Async()
   ├─ RelayService.CreateAllocationAsync(capacity: 1)
   │  └─ Lấy allocation object (IP, port, keys)
   ├─ RelayService.GetJoinCodeAsync(allocationId)
   │  └─ Lấy MatchId (6 ký tự)
   ├─ MatchHostConnectionMenu.MatchId = joinCode
   └─ Transport.SetRelayServerData(allocation)
   
4. NetworkManager.StartHost()
   └─ Bắt đầu listening cho clients
   
5. EGameMatch.OnNetworkSpawn()
   ├─ Register NetworkManager.OnServerStarted callback
   └─ Ready để nhận clients
```

---

### 3. ClientConnection - Guest-Specific Connection

**Namespace**: `Assets.OnlineMode.Connection`

**File**: `Connection/ClientConnection.cs`

**Mô tả**: Static class xử lý logic khởi động Client/Guest

#### Methods

##### `StartConnection_Async(string joinString) : Task`

```csharp
internal static async Task StartConnection_Async(string joinString)
```

**Visibility**: Internal (chỉ dùng qua `EConnection`)

**Parameters**:
- `joinString` (string): Match ID từ Host (6 ký tự)

**Return**: `Task` (async operation)

**Validation**:
```csharp
private static bool JoinStringDoesntSeemValid(string joinString)
{
    return string.IsNullOrEmpty(joinString) || (joinString.Length != 6);
}
```

**Quy trình chi tiết**:
```
1. InvalidToExecute() check
   ├─ Nếu EConnection.ReadyToConnect() = true → return
   ├─ Nếu JoinStringDoesntSeemValid() = true → return
   └─ Nếu ok → continue
   
2. EUnityMultiplayerServices.StartRelay_Async(joinString)
   ├─ Init Unity Services
   ├─ Sign in anonymously
   └─ Call UnityRelay.StartRelay_Async(joinString)
   
3. UnityRelay.StartRelay_Async(joinString)
   ├─ RelayService.JoinAllocationAsync(joinString)
   │  ├─ Verify join code với Relay server
   │  └─ Lấy Host endpoint info
   └─ Transport.SetRelayServerData(joinAllocation)
   
4. NetworkManager.StartClient()
   └─ Kết nối đến Host qua Relay
   
5. NetworkManager.OnClientConnected(clientId)
   ├─ Host nhận signal từ new client
   └─ EGameMatch.OnNewClientConnected(clientId)
      └─ Lưu clientId, check start conditions
```

---

### 4. UnityRelay - Relay Protocol Handler

**Namespace**: `Assets.OnlineMode.Connection.UnityMultiplayerServices`

**File**: `Connection/UnityMultiplayerServices/UnityRelay.cs`

**Mô tả**: Static class quản lý Relay Service API calls

#### Properties

##### `Transport : UnityTransport` (static property)

```csharp
private static UnityTransport Transport { get; }
```

**Mô tả**: UnityTransport instance từ NetworkManager

**Initialize**:
```csharp
static UnityRelay()
{
    Transport = NetworkManager.Singleton.NetworkConfig
                             .NetworkTransport as UnityTransport;
}
```

---

##### `RelayConnectionType : string` (static property)

```csharp
private static string RelayConnectionType { get; }
```

**Value**: `"dtls"`

**Mô tả**: DTLS (Datagram TLS) - mã hóa UDP

---

#### Methods

##### `StartRelay_Async() : Task` (Host)

```csharp
public static async Task StartRelay_Async()
```

**Mô tả**: Khởi động Relay cho HOST

**Return**: `Task`

**Quy trình**:
```
1. RelayAllocation_Async()
   └─ RelayService.Instance.CreateAllocationAsync(capacity: 1)
      └─ Lấy Allocation object

2. ConfigureRelayForTransport()
   └─ Transport.SetRelayServerData(allocation data)
   
3. SetMatchIdInMatchHostMenu()
   └─ RelayService.Instance.GetJoinCodeAsync(allocationId)
      └─ Lấy MatchId (6 ký tự)
      └─ MatchHostConnectionMenu.MatchId = joinCode
```

**Error Handling**:
```csharp
try
{
    return await RelayService.Instance.CreateAllocationAsync(
        MatchHostConnectionMenu.MatchGuestCapacity
    );
}
catch (RelayServiceException e)
{
    Debug.LogError(e);
    return null;  // ← Trả về null nếu lỗi
}
```

---

##### `StartRelay_Async(string joinString) : Task` (Guest)

```csharp
public static async Task StartRelay_Async(string joinString)
```

**Mô tả**: Khởi động Relay cho GUEST/CLIENT

**Parameters**:
- `joinString` (string): Join code từ Host

**Return**: `Task`

**Quy trình**:
```
1. RelayAllocation_Async(joinString)
   └─ RelayService.Instance.JoinAllocationAsync(joinString)
      ├─ Verify code
      └─ Lấy JoinAllocation (Host endpoint)

2. ConfigureRelayForTransport()
   └─ Transport.SetRelayServerData(joinAllocation data)
   └─ Thiết lập kết nối đến Host
```

**Error Handling**:
```csharp
try
{
    return await RelayService.Instance.JoinAllocationAsync(joinString);
}
catch (RelayServiceException e)
{
    Debug.LogError(e);
    return null;
}
```

---

### 5. EUnityMultiplayerServices - Services Orchestration

**Namespace**: `Assets.OnlineMode.Connection.UnityMultiplayerServices`

**File**: `Connection/UnityMultiplayerServices/EUnityMultiplayerServices.cs`

**Mô tả**: Static class khởi tạo và quản lý Unity Services

#### Methods

##### `StartRelay_Async() : Task` (Host)

```csharp
public static async Task StartRelay_Async()
```

**Mô tả**: Khởi tạo services + khởi động Relay cho Host

**Return**: `Task`

**Quy trình**:
```
1. InitializeUnityServicesAndSignIn()
   ├─ InitializeUnityServices_Async()
   │  └─ Check UnityServices.State
   │  └─ Nếu Uninitialized → UnityServices.InitializeAsync()
   │
   └─ SignInAuthenticationService_Async()
      └─ Check AuthenticationService.IsSignedIn
      └─ Nếu chưa → SignInAnonymouslyAsync()

2. UnityRelay.StartRelay_Async()
```

---

##### `StartRelay_Async(string joinString) : Task` (Guest)

```csharp
public static async Task StartRelay_Async(string joinString)
```

**Mô tả**: Khởi tạo services + khởi động Relay cho Guest

**Parameters**:
- `joinString` (string): Join code

**Return**: `Task`

**Quy trình**: Giống host, nhưng cuối cùng gọi `UnityRelay.StartRelay_Async(joinString)`

---

### 6. EGameMatch - Game State & RPC

**Namespace**: `Assets.OnlineMode.GameMatch`

**File**: `GameMatch/EGameMatch.cs`

**Base Class**: `NetworkBehaviour` (from Netcode)

**Mô tả**: NetworkBehaviour quản lý trò chơi online

#### Properties

##### `Singleton : EGameMatch`

```csharp
public static EGameMatch Singleton
{
    get => _singleton;
    private set { ... }
}
```

**Mô tả**: Singleton instance

**Initialized in**: `Awake()`

**Note**: Duplicate instances sẽ bị destroy

---

##### `AllPlayerClientIds : ulong[]`

```csharp
public ulong[] AllPlayerClientIds { get; }
```

**Mô tả**: Danh sách ClientIds của tất cả players

**Array Length**: `MatchHostConnectionMenu.TotalPlayers` (2)

**Index**:
- `[0]` - Host ClientId
- `[1]` - Guest ClientId

**Usage**:
```csharp
AllPlayerClientIds[0];  // Host
AllPlayerClientIds[1];  // Guest
```

---

#### Methods (RPC)

##### `MarkCell_ServerRPC(int rowIndex, int columnIndex, RpcParams rpcParams = default) : void`

```csharp
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int rowIndex, int columnIndex, RpcParams rpcParams = default)
```

**Mô tả**: RPC gọi từ Client, xử lý bởi Server

**Parameters**:
- `rowIndex` (int): Hàng (0-5)
- `columnIndex` (int): Cột (0-5)
- `rpcParams` (RpcParams): Metadata (optional, chứa clientId)

**Return**: void

**Validation**:
```csharp
private bool IsPlayerTurn(RpcParams rpcParams)
{
    // Kiểm tra:
    // 1. rpcParams.Receive.SenderClientId có phải lượt không?
    // 2. Match đã bắt đầu chưa?
    // ...
}
```

**Server-side Execution**:
```
1. Server nhận RPC từ Client
2. IsPlayerTurn(rpcParams)?
   ├─ true  → Continue
   └─ false → Return (invalid)
   
3. Validate cell position
   ├─ Cell in bounds?
   └─ Cell not already marked?
   
4. Call MarkCell_ClientsAndHostRPC()
   └─ Broadcast đến tất cả clients
```

---

##### `MarkCell_ClientsAndHostRPC(int rowIndex, int columnIndex, RpcParams rpcParams = default) : void`

```csharp
[Rpc(SendTo.Everyone)]
private void MarkCell_ClientsAndHostRPC(int rowIndex, int columnIndex)
```

**Mô tả**: RPC broadcast từ Server tới tất cả clients + server

**Parameters**:
- `rowIndex` (int): Hàng
- `columnIndex` (int): Cột

**Return**: void

**Client-side Execution**:
```
1. Tất cả clients (include host) nhận RPC
2. Update game board[rowIndex][columnIndex]
3. Refresh UI
4. Switch turn
5. Check win condition
```

---

### 7. MatchHostConnectionMenu - Host Match Management

**Namespace**: `Assets.OnlineMode.ConnectionMenu`

**File**: `ConnectionMenu/MatchHostConnectionMenu.cs`

**Mô tả**: Static class quản lý trạng thái phòng Host

#### Properties (Static)

##### `MatchGuestCapacity : int` (readonly)

```csharp
public static int MatchGuestCapacity { get; }
// Value: 1
```

**Mô tả**: Số lượng guest tối đa (1 = 1v1 game)

---

##### `TotalPlayers : int` (readonly)

```csharp
public static int TotalPlayers { get; }
// Value: 2 (Host + Guest)
```

**Mô tả**: Tổng số players

---

##### `MatchId : string`

```csharp
public static string MatchId
{
    get => _matchId;
    set
    {
        if (!string.IsNullOrEmpty(_matchId))  // Set once only
            return;
        _matchId = value;
    }
}
```

**Mô tả**: Join code của phòng (6 ký tự)

**Nhận giá trị từ**: `UnityRelay.StartRelay_Async()`

**Protected**: Setter không cho phép overwrite (set once)

**Ví dụ**:
```csharp
string code = MatchHostConnectionMenu.MatchId;  // "ABC123"
```

---

#### Methods (Static)

##### `CreateMatch_Async() : Task`

```csharp
public static async Task CreateMatch_Async()
```

**Mô tả**: Tạo phòng mới

**Return**: `Task`

**Quy trình**:
```
1. EConnection.StartConnection_Async()
   └─ HostConnection.StartConnection_Async()
   
2. Validate kết nối
   if (!EConnection.ReadyToConnect())
       Debug.LogError("Something's wrong...");
```

**Ví dụ**:
```csharp
private async void OnCreateMatchButtonClicked()
{
    await MatchHostConnectionMenu.CreateMatch_Async();
    
    if (EConnection.ReadyToConnect())
    {
        string matchId = MatchHostConnectionMenu.MatchId;
        DisplayMatchId(matchId);  // Show QR, text, etc
    }
}
```

---

### 8. MatchGuestConnectionMenu - Guest Match Management

**Namespace**: `Assets.OnlineMode.ConnectionMenu`

**File**: `ConnectionMenu/MatchGuestConnectionMenu.cs`

**Mô tả**: Static class quản lý kết nối Guest

#### Methods (Static)

##### `ConnectToMatch_Async(string matchId) : Task`

```csharp
public static async Task ConnectToMatch_Async(string matchId)
```

**Mô tả**: Kết nối đến phòng hiện có

**Parameters**:
- `matchId` (string): Join code từ Host (6 ký tự)

**Return**: `Task`

**Quy trình**:
```
1. EConnection.StartConnection_Async(matchId)
   └─ ClientConnection.StartConnection_Async(matchId)
   
2. Validate kết nối
   if (!EConnection.ReadyToConnect())
       Debug.LogError("Failed to connect...");
```

**Ví dụ**:
```csharp
private async void OnJoinMatchButtonClicked()
{
    string matchId = inputField.text.Trim();
    
    // Validate
    if (matchId.Length != 6)
    {
        ShowError("Invalid format");
        return;
    }
    
    await MatchGuestConnectionMenu.ConnectToMatch_Async(matchId);
    
    if (EConnection.ReadyToConnect())
    {
        Debug.Log("✅ Joined successfully");
    }
}
```

---

### 9. EOnlinePlayer - Gameplay API

**Namespace**: `Assets.OnlineMode.OnlinePlayer`

**File**: `OnlinePlayer/EOnlinePlayer.cs`

**Mô tả**: Static wrapper class - Entry point cho gameplay

#### Methods (Static)

##### `MarkCell(ValueTuple<int, int> position) : void`

```csharp
public static void MarkCell((int rowIndex, int columnIndex) position)
```

**Mô tả**: Gửi hành động đánh dấu ô

**Parameters**:
- `position` : (int, int) - Tuple (rowIndex, columnIndex)

**Return**: void

**Implementation**:
```csharp
EGameMatch.Singleton.MarkCell_ServerRPC(
    rowIndex: position.rowIndex, 
    columnIndex: position.columnIndex
);
```

**Ví dụ**:
```csharp
// Trong GameBoardUI
public void OnCellClicked(int row, int col)
{
    EOnlinePlayer.MarkCell((row, col));
    
    // RPC workflow:
    // 1. Send to Server
    // 2. Server validates
    // 3. Server broadcasts to all
    // 4. All clients update UI
}
```

---

##### `LeaveMatch() : void`

```csharp
public static void LeaveMatch()
```

**Mô tả**: Rời khỏi trò chơi, ngắt kết nối

**Parameters**: Không có

**Return**: void

**Implementation**:
```csharp
EConnection.Disconnect();
```

**Side Effects**:
- NetworkManager shutdown
- Scene cleanup
- Quay về Menu

**Ví dụ**:
```csharp
// Trong UI Exit button
public void OnExitButtonClicked()
{
    EOnlinePlayer.LeaveMatch();
}
```

---

## 🎯 Common Usage Patterns

### Pattern 1: Create & Share Match

```csharp
public class HostMatchUI : MonoBehaviour
{
    public async void OnCreateMatchClicked()
    {
        // 1. Create match
        await MatchHostConnectionMenu.CreateMatch_Async();
        
        // 2. Check if ready
        if (!EConnection.ReadyToConnect())
        {
            Debug.LogError("Failed to create match");
            return;
        }
        
        // 3. Get match ID
        string matchId = MatchHostConnectionMenu.MatchId;
        
        // 4. Display to player (QR Code, Text, etc)
        DisplayMatchCode(matchId);
        
        Debug.Log($"✅ Match created: {matchId}");
    }
    
    private void DisplayMatchCode(string code)
    {
        // Generate QR code
        // OR show as text
        // OR copy to clipboard
    }
}
```

### Pattern 2: Join Match

```csharp
public class GuestMatchUI : MonoBehaviour
{
    [SerializeField] private InputField joinCodeInput;
    
    public async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim();
        
        // 1. Validate
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            ShowError("Invalid join code format");
            return;
        }
        
        // 2. Try join
        await MatchGuestConnectionMenu.ConnectToMatch_Async(code);
        
        // 3. Check if connected
        if (!EConnection.ReadyToConnect())
        {
            ShowError("Failed to join match");
            return;
        }
        
        Debug.log("✅ Joined successfully");
    }
}
```

### Pattern 3: Send Game Action

```csharp
public class GameplayController : MonoBehaviour
{
    private void OnBoardCellClicked(int row, int col)
    {
        // Check if valid
        if (!IsValidMove(row, col))
        {
            Debug.LogWarning("Invalid move");
            return;
        }
        
        // Send action
        EOnlinePlayer.MarkCell((row, col));
        
        // UI feedback (pending state)
        ShowCellAsMarking(row, col);
    }
    
    private bool IsValidMove(int row, int col)
    {
        return row >= 0 && row < 6 &&
               col >= 0 && col < 6 &&
               board[row, col] == null;
    }
}
```

### Pattern 4: Exit Match

```csharp
public class GameExitUI : MonoBehaviour
{
    public void OnExitButtonClicked()
    {
        if (EditorUtility.DisplayDialog(
            "Exit Match?",
            "Are you sure?",
            "Yes", "No"))
        {
            EOnlinePlayer.LeaveMatch();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
```

---

## ⚠️ Important Notes

### Thread Safety

```csharp
// ✅ Safe - called from main thread
public void OnUIButtonClicked()
{
    await EConnection.StartConnection_Async();
}

// ❌ Unsafe - called from other thread
new Thread(() => {
    EConnection.Disconnect();  // Don't do this!
}).Start();
```

### Singleton Safety

```csharp
// ✅ Safe - after OnNetworkSpawn
public override void OnNetworkSpawn()
{
    var match = EGameMatch.Singleton;  // OK
    // ...
}

// ❌ Risky - may be null if called too early
void Start()
{
    var match = EGameMatch.Singleton;  // Could be null!
}
```

### RPC Validation

```csharp
// ✅ Server validates RPC from client
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col, RpcParams rpcParams)
{
    if (!IsPlayerTurn(rpcParams))
        return;  // Reject invalid RPC
    
    // Process...
}

// ❌ Don't trust client input
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col)
{
    // Always validate row, col ranges
    // Even though client validated it
}
```

---

## 📊 Performance Notes

### Bandwidth Usage

```
Per MarkCell action:
- RPC to Server: ~10-20 bytes
- RPC from Server: ~10-20 bytes
- Total: ~40 bytes per action

At ~5 actions/second = ~200 bytes/sec = very low
```

### Latency

```
Typical latency:
- Local (same network): 10-50ms
- Regional (nearby): 50-150ms
- International: 150-300ms

RPC roundtrip: Client → Server → Clients = 2x latency
```

### NetworkVariable vs RPC

```
NetworkVariable:
✅ Auto-sync state on change
❌ Higher bandwidth (full state sync)
❌ No history/audit log

RPC:
✅ Lower bandwidth (actions only)
✅ Easy to audit/replay
❌ Manual sync on reconnect
❌ No state if client connects late
```

---

**Document Version**: 1.0
**Last Updated**: 2024
**Status**: ✅ Complete
