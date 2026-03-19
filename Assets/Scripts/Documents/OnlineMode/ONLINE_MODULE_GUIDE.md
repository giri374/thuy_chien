# 📡 Hướng Dẫn Module Online - Cơ Chế Mạng & Cách Sử Dụng

## 📋 Mục Lục
1. [Tổng Quan Kiến Trúc](#tổng-quan-kiến-trúc)
2. [Các Thành Phần Chính](#các-thành-phần-chính)
3. [Cơ Chế Mạng Chi Tiết](#cơ-chế-mạng-chi-tiết)
4. [Hướng Dẫn Sử Dụng](#hướng-dẫn-sử-dụng)
5. [Luồng Kết Nối](#luồng-kết-nối)
6. [Ví Dụ Thực Tế](#ví-dụ-thực-tế)

---

## 🏗️ Tổng Quan Kiến Trúc

Module Online của dự án **Thủy Chiến** sử dụng kiến trúc **Client-Server** với công nghệ:
- **Netcode for GameObjects** (Unity Netcode): Xử lý networking logic
- **Unity Relay Service**: Cung cấp server relay để kết nối các client
- **Unity Multiplayer Services**: Xác thực người dùng

### Sơ Đồ Kiến Trúc

```
┌─────────────────────────────────────────────────────────────┐
│                    Unity Relay Service                       │
│              (Server trung gian - P2P relay)                │
└────────────────┬────────────────────────────┬────────────────┘
                 │                            │
          ┌──────▼──────┐            ┌────────▼──────┐
          │    HOST     │────────────│    GUEST      │
          │  (Server)   │  RPC Calls │    (Client)   │
          │   - Quản lý │            │  - Gửi input  │
          │   - Sync    │            │  - Nhận state │
          └─────────────┘            └───────────────┘
```

---

## 🔧 Các Thành Phần Chính

### 1. **Connection Layer** (`Assets/OnlineMode/Connection/`)

#### `EConnection.cs` - Điểm Trung Tâm Kết Nối
- **Chức năng**: Giao diện chính để quản lý kết nối
- **Phương thức chính**:
  - `ReadyToConnect()`: Kiểm tra xem NetworkManager có sẵn sàng không
  - `StartConnection_Async()`: Khởi động Host (máy chủ)
  - `StartConnection_Async(joinString)`: Khởi động Client với mã tham gia
  - `Disconnect()`: Ngắt kết nối

```csharp
// Ví dụ sử dụng
await EConnection.StartConnection_Async();  // Tạo Host
await EConnection.StartConnection_Async("ABC123");  // Tham gia với mã
EConnection.Disconnect();  // Ngắt kết nối
```

#### `HostConnection.cs` - Kết Nối Host
- **Chức năng**: Thiết lập máy chủ (Host)
- **Quy trình**:
  1. Kiểm tra hợp lệ (chưa kết nối)
  2. Khởi động Relay Server
  3. Bắt đầu làm Host

```csharp
await HostConnection.StartConnection_Async();
// - Tạo Relay Allocation
// - Lấy MatchId (mã tham gia 6 ký tự)
// - StartHost()
```

#### `ClientConnection.cs` - Kết Nối Guest
- **Chức năng**: Kết nối như Client (Guest)
- **Quy trình**:
  1. Kiểm tra mã tham gia hợp lệ (6 ký tự)
  2. Khởi động Relay với mã tham gia
  3. Bắt đầu làm Client

```csharp
await ClientConnection.StartConnection_Async("ABC123");
// - Xác minh mã tham gia
// - Join Relay Allocation
// - StartClient()
```

### 2. **Unity Relay Services** (`Assets/OnlineMode/Connection/UnityMultiplayerServices/`)

#### `EUnityMultiplayerServices.cs` - Facade Dịch Vụ
- **Chức năng**: Khởi tạo dịch vụ Unity và xác thực
- **Quy trình khởi tạo**:
  1. Khởi tạo Unity Services
  2. Đăng nhập xác thực (Anonymous)
  3. Khởi động Relay

```csharp
await EUnityMultiplayerServices.StartRelay_Async();  // Host
await EUnityMultiplayerServices.StartRelay_Async("ABC123");  // Guest
```

#### `UnityRelay.cs` - Xử Lý Relay Protocol
- **Chức năng**: Quản lý kết nối Relay cụ thể
- **Transport**: UnityTransport (UDP-based via UTP)
- **Connection Type**: DTLS (Datagram TLS - mã hóa)

**Quy trình Host:**
```
1. RelayService.CreateAllocationAsync()
   ↓
2. GetJoinCodeAsync() → MatchId (6 ký tự)
   ↓
3. ConfigureRelayForTransport()
   ↓
4. NetworkManager.StartHost()
```

**Quy trình Guest:**
```
1. RelayService.JoinAllocationAsync(joinString)
   ↓
2. ConfigureRelayForTransport()
   ↓
3. NetworkManager.StartClient()
```

### 3. **Game Match Layer** (`Assets/OnlineMode/GameMatch/`)

#### `EGameMatch.cs` - Logic Trò Chơi Online
- **Kiểu**: NetworkBehaviour (kế thừa từ Netcode)
- **Singleton Pattern**: Đảm bảo chỉ có 1 instance

**Phương thức RPC:**
```csharp
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int rowIndex, int columnIndex, RpcParams rpcParams)
{
    // Nhận request từ Client
    // Kiểm tra lượt chơi
    // Broadcast cho tất cả Client
}
```

**RPC (Remote Procedure Call) Flow:**
```
Client                  Server              Clients
  │                       │                    │
  ├─ MarkCell_ServerRPC ──┤                    │
  │   (rowIndex,column)    │                    │
  │                        ├─ Validate Turn    │
  │                        │                    │
  │                        ├─ MarkCell_ClientsAndHostRPC ──→
  │                        │                    │
  │←─ Sync State ─────────┴────────────────────┤
```

### 4. **Connection Menu** (`Assets/OnlineMode/ConnectionMenu/`)

#### `MatchHostConnectionMenu.cs` - Menu Host
```csharp
public static int MatchGuestCapacity = 1;  // Số lượng Guest
public static int TotalPlayers = 2;  // Host + Guest
public static string MatchId;  // Mã tham gia (từ Relay)

await MatchHostConnectionMenu.CreateMatch_Async();
```

#### `MatchGuestConnectionMenu.cs` - Menu Guest
```csharp
await MatchGuestConnectionMenu.ConnectToMatch_Async("ABC123");
```

### 5. **Online Player** (`Assets/OnlineMode/OnlinePlayer/`)

#### `EOnlinePlayer.cs` - Wrapper Cho Player
- **Điểm vào (Entry Point)** để giao tiếp với Online Module

```csharp
// Gửi nước đi
EOnlinePlayer.MarkCell((3, 2));

// Rời trò chơi
EOnlinePlayer.LeaveMatch();
```

---

## 🔄 Cơ Chế Mạng Chi Tiết

### 1. **Xác Thực & Khởi Tạo**

```
┌──────────────────────────────────────┐
│  Unity Services Initialization       │
├──────────────────────────────────────┤
│ 1. UnityServices.InitializeAsync()   │
│    - Kiểm tra trạng thái             │
│    - Khởi tạo lần đầu                │
├──────────────────────────────────────┤
│ 2. AuthenticationService.SignInAnony  │
│    - Đăng nhập ẩn danh               │
│    - Lấy user token                  │
├──────────────────────────────────────┤
│ 3. RelayService Ready                │
│    - Sẵn sàng tạo/join allocation    │
└──────────────────────────────────────┘
```

**Xác thực Ẩn Danh (Anonymous):**
- Không yêu cầu tài khoản
- Tự động tạo user ID duy nhất
- Phù hợp cho trò chơi casual

### 2. **Tạo Phòng (Host)**

```
Host Setup Flow:
┌────────────────────────────────────────┐
│ CreateMatch_Async()                    │
├────────────────────────────────────────┤
│ 1. EConnection.StartConnection_Async() │
├────────────────────────────────────────┤
│ 2. HostConnection.StartConnection_Async()
│    ↓                                   │
│    • RelayService.CreateAllocationAsync()
│      → Lấy Allocation (IP, Port, Keys)│
│                                        │
│    • RelayService.GetJoinCodeAsync()   │
│      → MatchId = "ABC123" (6 ký tự)   │
│                                        │
│    • Transport.SetRelayServerData()    │
│      → Cấu hình endpoint               │
│                                        │
│    • NetworkManager.StartHost()        │
│      → Bắt đầu listening               │
├────────────────────────────────────────┤
│ 3. Chia sẻ MatchId cho Guest           │
│    (QR Code, Chat, vv...)              │
└────────────────────────────────────────┘
```

### 3. **Tham Gia Phòng (Guest)**

```
Guest Join Flow:
┌────────────────────────────────────────┐
│ ConnectToMatch_Async("ABC123")         │
├────────────────────────────────────────┤
│ 1. EConnection.StartConnection_Async(id)
├────────────────────────────────────────┤
│ 2. ClientConnection.StartConnection_Async(id)
│    ↓                                   │
│    • Validate: joinString.Length == 6  │
│                                        │
│    • RelayService.JoinAllocationAsync()│
│      → Xác minh mã                     │
│      → Lấy endpoint của Host           │
│                                        │
│    • Transport.SetRelayServerData()    │
│      → Cấu hình kết nối đến Host       │
│                                        │
│    • NetworkManager.StartClient()      │
│      → Kết nối đến Host                │
├────────────────────────────────────────┤
│ 3. NetworkManager.OnClientConnected    │
│    → Khởi tạo trò chơi                 │
└────────────────────────────────────────┘
```

### 4. **Đồng Bộ Game State**

#### NetworkVariable (State Synchronization)
```csharp
// Tự động đồng bộ khi thay đổi
// Mỗi khi Value thay đổi → tự động broadcast cho clients

// Ví dụ:
private NetworkVariable<GameState> CurrentGameState = new();
```

#### RPC (Action Replication)
```csharp
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col)
{
    // Nhận từ Client
    // Validate
    // Broadcast: MarkCell_ClientsAndHostRPC()
}

[Rpc(SendTo.Everyone)]
private void MarkCell_ClientsAndHostRPC(int row, int col)
{
    // Tất cả (Host + Clients) cập nhật UI
    board[row, col] = currentMark;
    RefreshUI();
}
```

### 5. **Đúc Kết Luồng Trò Chơi**

```
Timeline:
┌─────────────┬──────────────┬──────────┬──────────────┐
│   Setup     │   Running    │  Match   │  Cleanup     │
│             │              │ Finished │              │
├─────────────┼──────────────┼──────────┼──────────────┤
│ 1. Auth     │ 1. RPC Call  │ 1. Check │ 1. Shutdown  │
│ 2. Create/  │ 2. Validate  │    Win   │ 2. Cleanup   │
│    Join     │ 3. Broadcast │ 2. Clear │    Network   │
│ 3. Match    │ 4. Sync      │    Board │              │
│    Spawn    │              │          │              │
└─────────────┴──────────────┴──────────┴──────────────┘
```

---

## 📖 Hướng Dẫn Sử Dụng

### Bước 1: Khởi Tạo Module

**Trong Scene Setup:**
```csharp
// 1. Tạo NetworkManager GameObject
// 2. Cấu hình:
//    - Transport: UnityNetcodeTransport
//    - Prefabs: Đăng ký NetworkPrefabs cần dùng

// 3. Tạo EGameMatch GameObject với NetworkObject script

// 4. EGameMatch sẽ tự động Singleton
```

### Bước 2: Tạo Trận Đấu (Host)

```csharp
// Trong MatchHostConnectionMenu hoặc UI Handler
private async void OnCreateMatchButton()
{
    await MatchHostConnectionMenu.CreateMatch_Async();
    
    if (EConnection.ReadyToConnect())
    {
        // ✅ Thành công
        string matchId = MatchHostConnectionMenu.MatchId;
        
        // Hiển thị cho người chơi chia sẻ
        DisplayMatchId(matchId);  // QR Code, Text, vv
    }
    else
    {
        // ❌ Lỗi - check logs
        Debug.LogError("Failed to create match");
    }
}
```

### Bước 3: Tham Gia Trận Đấu (Guest)

```csharp
// Trong UI Join Input
private async void OnJoinMatchButton(string matchIdInput)
{
    // Validate input
    if (string.IsNullOrEmpty(matchIdInput) || matchIdInput.Length != 6)
    {
        Debug.LogError("Invalid match ID format");
        return;
    }
    
    await MatchGuestConnectionMenu.ConnectToMatch_Async(matchIdInput);
    
    if (EConnection.ReadyToConnect())
    {
        // ✅ Kết nối thành công
        // Scene sẽ tự động load
    }
    else
    {
        // ❌ Kết nối thất bại
        ShowErrorMessage("Could not connect to match");
    }
}
```

### Bước 4: Thực Hiện Hành Động Trong Trò Chơi

```csharp
// Trong GameplayController
public void PlayerClickedCell(int row, int col)
{
    // Gửi hành động qua network
    EOnlinePlayer.MarkCell((row, col));
    
    // RPC sẽ tự động:
    // 1. Gửi đến Server
    // 2. Server validate
    // 3. Server broadcast cho tất cả
    // 4. Tất cả cập nhật state
}
```

### Bước 5: Rời Trận Đấu

```csharp
// Trong Exit Handler
public void LeaveMatch()
{
    EOnlinePlayer.LeaveMatch();
    // EConnection.Disconnect() được gọi bên trong
    
    // Cleanup:
    // - NetworkManager.Shutdown()
    // - Quay về Menu
}
```

---

## 🔀 Luồng Kết Nối Chi Tiết

### Scenario 1: Host tạo phòng

```
USER ACTION: Click "Create Match" Button
        ↓
MatchHostConnectionMenu.CreateMatch_Async()
        ↓
EConnection.StartConnection_Async()  [không có joinString]
        ↓
HostConnection.StartConnection_Async()
        ↓
EUnityMultiplayerServices.StartRelay_Async()  [không có joinString]
        ↓
├─ UnityServices.InitializeAsync()
├─ AuthenticationService.SignInAnonymously()
└─ UnityRelay.StartRelay_Async()  [overload không param]
        ↓
├─ RelayService.CreateAllocationAsync(capacity: 1)
│  → Lấy Allocation object
│  → Có IP, Port, 3 Keys (Allocation, Relay, etc)
│
├─ RelayService.GetJoinCodeAsync(allocationId)
│  → Lấy MatchId (6 ký tự, VD: "ABC123")
│  → Lưu vào MatchHostConnectionMenu.MatchId
│
├─ Transport.SetRelayServerData(allocationData)
│  → Cấu hình UnityTransport với endpoint Relay
│
└─ NetworkManager.StartHost()
        ↓
✅ Host listening trên Relay Server
   Player có MatchId để chia sẻ
```

### Scenario 2: Guest tham gia phòng

```
USER ACTION: Input MatchId "ABC123" → Click "Join" Button
        ↓
MatchGuestConnectionMenu.ConnectToMatch_Async("ABC123")
        ↓
EConnection.StartConnection_Async("ABC123")
        ↓
ClientConnection.StartConnection_Async("ABC123")
        ↓
Validate: "ABC123".Length == 6 ✅
        ↓
EUnityMultiplayerServices.StartRelay_Async("ABC123")
        ↓
├─ UnityServices.InitializeAsync()
├─ AuthenticationService.SignInAnonymously()
└─ UnityRelay.StartRelay_Async("ABC123")  [overload với joinString]
        ↓
├─ RelayService.JoinAllocationAsync("ABC123")
│  → Relay xác minh mã
│  → Lấy JoinAllocation (chứa Host endpoint)
│
├─ Transport.SetRelayServerData(joinAllocationData)
│  → Cấu hình kết nối đến Host qua Relay
│
└─ NetworkManager.StartClient()
        ↓
┌─ NetworkManager.OnClientConnected(clientId: Guest)
│  [Server nhận signal từ Guest]
│
├─ EGameMatch.OnNetworkSpawn()
│  ├─ NetworkManager.OnServerStarted += InitializeGameMatch
│  └─ InitializeGameMatch()
│      └─ OnNewClientConnected(clientId)
│          ├─ Kiểm tra match đã start?
│          ├─ Lưu clientId vào AllPlayerClientIds
│          └─ Nếu đủ players → Start match
│
└─ ✅ Game initialized, ready to play
```

### Scenario 3: Thực hiện nước đi trong trò chơi

```
USER ACTION: Click cell (3, 2)
        ↓
GameplayController.PlayerClickedCell(3, 2)
        ↓
EOnlinePlayer.MarkCell((3, 2))
        ↓
EGameMatch.MarkCell_ServerRPC(rowIndex: 3, columnIndex: 2)
        ↓
[Netcode sends RPC to Server with this Client's clientId]
        ↓
SERVER:
├─ Receive MarkCell_ServerRPC
├─ RpcParams rpcParams → clientId
├─ IsPlayerTurn(rpcParams)?
│  ├─ Validate: Có phải lượt của client này?
│  └─ ✅ Yes → Continue
│
└─ Call: MarkCell_ClientsAndHostRPC(3, 2)
        ↓
[Netcode broadcasts to ALL clients (including Server/Host)]
        ↓
ALL CLIENTS:
├─ MarkCell_ClientsAndHostRPC(3, 2)
├─ Update game board[3][2] = currentMark
├─ Refresh UI
└─ Switch turn
        ↓
✅ All players see the same board state
```

### Scenario 4: Ngắt kết nối

```
USER ACTION: Click "Leave" Button / Close App
        ↓
LeaveMatchButton.OnClick()
        ↓
EOnlinePlayer.LeaveMatch()
        ↓
EConnection.Disconnect()
        ↓
if (EConnection.ReadyToConnect())  // true
    └─ NetworkManager.Singleton.Shutdown()
        ↓
├─ Close WebSocket/UDP connections
├─ Notify Relay Server
├─ Signal to Host/Guests (disconnect)
├─ Cleanup NetworkObjects
└─ Return to Menu Scene
        ↓
✅ Clean disconnect
```

---

## 💡 Ví Dụ Thực Tế

### Ví Dụ 1: Tạo UI để Tạo Phòng

```csharp
using Assets.OnlineMode.ConnectionMenu;
using Assets.OnlineMode.Connection;
using UnityEngine;
using UnityEngine.UI;

public class CreateMatchUI : MonoBehaviour
{
    [SerializeField] private Button createMatchButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Image matchIdQRCode;  // QR Code image
    
    private void Start()
    {
        createMatchButton.onClick.AddListener(OnCreateMatchClicked);
    }
    
    private async void OnCreateMatchClicked()
    {
        statusText.text = "Creating match...";
        createMatchButton.interactable = false;
        
        try
        {
            // Tạo phòng
            await MatchHostConnectionMenu.CreateMatch_Async();
            
            // Kiểm tra kết nối
            if (EConnection.ReadyToConnect())
            {
                string matchId = MatchHostConnectionMenu.MatchId;
                
                statusText.text = $"Match ID: {matchId}";
                statusText.color = Color.green;
                
                // Hiển thị QR code
                DisplayQRCode(matchId);
                
                Debug.Log($"✅ Match created with ID: {matchId}");
            }
            else
            {
                statusText.text = "Failed to create match";
                statusText.color = Color.red;
                createMatchButton.interactable = true;
            }
        }
        catch (System.Exception ex)
        {
            statusText.text = $"Error: {ex.Message}";
            statusText.color = Color.red;
            createMatchButton.interactable = true;
            Debug.LogError($"❌ Error creating match: {ex}");
        }
    }
    
    private void DisplayQRCode(string matchId)
    {
        // Tạo QR code từ matchId
        // VD: QRCodeGenerator.GenerateQRCode(matchId)
        // Gán vào matchIdQRCode.sprite
    }
}
```

### Ví Dụ 2: Tạo UI để Tham Gia Phòng

```csharp
using Assets.OnlineMode.ConnectionMenu;
using Assets.OnlineMode.Connection;
using UnityEngine;
using UnityEngine.UI;

public class JoinMatchUI : MonoBehaviour
{
    [SerializeField] private InputField matchIdInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Text statusText;
    
    private void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
    }
    
    private async void OnJoinClicked()
    {
        string matchId = matchIdInput.text.Trim();
        
        // Validate
        if (string.IsNullOrEmpty(matchId))
        {
            statusText.text = "Please enter Match ID";
            return;
        }
        
        if (matchId.Length != 6)
        {
            statusText.text = "Match ID must be 6 characters";
            return;
        }
        
        statusText.text = "Joining...";
        joinButton.interactable = false;
        
        try
        {
            await MatchGuestConnectionMenu.ConnectToMatch_Async(matchId);
            
            if (EConnection.ReadyToConnect())
            {
                statusText.text = "Connected! Loading game...";
                statusText.color = Color.green;
                
                Debug.Log($"✅ Successfully joined match: {matchId}");
                
                // Scene sẽ auto load trong EGameMatch.OnNetworkSpawn()
            }
            else
            {
                statusText.text = "Failed to connect to match";
                statusText.color = Color.red;
                joinButton.interactable = true;
            }
        }
        catch (System.Exception ex)
        {
            statusText.text = $"Error: {ex.Message}";
            statusText.color = Color.red;
            joinButton.interactable = true;
            Debug.LogError($"❌ Error joining match: {ex}");
        }
    }
}
```

### Ví Dụ 3: Xử Lý Nước Đi Trong Trò Chơi

```csharp
using Assets.OnlineMode.OnlinePlayer;
using Assets.OnlineMode.GameMatch;
using UnityEngine;
using UnityEngine.UI;

public class GameBoardUI : MonoBehaviour
{
    private Button[][] boardButtons;  // 6x6 grid
    private Text turnIndicator;
    
    private void Start()
    {
        InitializeBoard();
    }
    
    private void InitializeBoard()
    {
        // Tạo 6x6 grid buttons
        boardButtons = new Button[6][];
        
        for (int row = 0; row < 6; row++)
        {
            boardButtons[row] = new Button[6];
            
            for (int col = 0; col < 6; col++)
            {
                int r = row, c = col;  // Closure
                
                Button cell = CreateCellButton(row, col);
                boardButtons[row][col] = cell;
                
                cell.onClick.AddListener(() => OnCellClicked(r, c));
            }
        }
    }
    
    private void OnCellClicked(int row, int col)
    {
        // Kiểm tra ô đã được đánh chưa
        if (boardButtons[row][col].interactable == false)
        {
            Debug.Log("Cell already marked");
            return;
        }
        
        // Gửi nước đi qua network
        EOnlinePlayer.MarkCell((row, col));
        
        // Tắt nút tạm thời (chờ sync từ server)
        boardButtons[row][col].interactable = false;
        
        Debug.Log($"Cell ({row}, {col}) marked, waiting for server confirmation");
    }
    
    // Được gọi khi nhận RPC từ Server
    public void UpdateBoardUI(int row, int col, Mark mark)
    {
        // Cập nhật giao diện
        boardButtons[row][col].image.color = GetColorForMark(mark);
        boardButtons[row][col].interactable = false;
        
        // Cập nhật turn indicator
        UpdateTurnIndicator();
    }
    
    private Color GetColorForMark(Mark mark)
    {
        return mark == Mark.Cross ? Color.red : Color.blue;
    }
    
    private void UpdateTurnIndicator()
    {
        string currentPlayer = EGameMatch.Singleton.IsHostTurn ? "Host" : "Guest";
        turnIndicator.text = $"Current: {currentPlayer}'s turn";
    }
}
```

### Ví Dụ 4: Error Handling & Reconnection

```csharp
using Assets.OnlineMode.Connection;
using UnityEngine;
using System;

public class NetworkErrorHandler : MonoBehaviour
{
    private int reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 3;
    
    private void Start()
    {
        // Subscribe to network events
        if (UnityEngine.Networking.NetworkManager.Singleton != null)
        {
            UnityEngine.Networking.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            UnityEngine.Networking.NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"Client {clientId} disconnected");
        
        if (clientId == UnityEngine.Networking.NetworkManager.Singleton.LocalClientId)
        {
            HandleLocalPlayerDisconnect();
        }
    }
    
    private void OnTransportFailure()
    {
        Debug.LogError("Transport layer failure");
        HandleNetworkFailure();
    }
    
    private void HandleLocalPlayerDisconnect()
    {
        if (!EConnection.StillConnectedToServer())
        {
            Debug.Log("Lost connection to server");
            TryReconnect();
        }
    }
    
    private void HandleNetworkFailure()
    {
        Debug.LogError("Network failure detected");
        TryReconnect();
    }
    
    private async void TryReconnect()
    {
        if (reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
        {
            Debug.LogError("Max reconnection attempts reached. Returning to menu.");
            ReturnToMainMenu();
            return;
        }
        
        reconnectAttempts++;
        Debug.Log($"Attempting to reconnect... (Attempt {reconnectAttempts})");
        
        // Đợi 2 giây trước khi reconnect
        await System.Threading.Tasks.Task.Delay(2000);
        
        // Thử kết nối lại
        EConnection.Disconnect();
        // Sau đó gọi CreateMatch hoặc ConnectToMatch again
    }
    
    private void ReturnToMainMenu()
    {
        EConnection.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }
    
    private void OnDestroy()
    {
        if (UnityEngine.Networking.NetworkManager.Singleton != null)
        {
            UnityEngine.Networking.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            UnityEngine.Networking.NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }
    }
}
```

---

## 🐛 Debug & Troubleshooting

### Kiểm Tra Kết Nối

```csharp
// Trong Console hoặc Log
if (EConnection.ReadyToConnect())
{
    Debug.Log("✅ NetworkManager is listening");
}
else
{
    Debug.Log("❌ NetworkManager not ready");
}

if (EConnection.StillConnectedToServer())
{
    Debug.Log("✅ Still connected to server");
}
else
{
    Debug.Log("❌ Disconnected from server");
}
```

### Common Issues

| Issue | Nguyên Nhân | Giải Pháp |
|-------|-----------|----------|
| "Join String Invalid" | Mã tham gia không đúng format | Đảm bảo đúng 6 ký tự |
| "RelayServiceException" | Relay Service không khởi tạo | Check Unity Services config |
| "IsPlayerTurn returns false" | Lượt không phải của player | Kiểm tra logic turn |
| "NetworkObject is null" | EGameMatch chưa Spawn | Chờ OnNetworkSpawn() |

---

## 📋 Tóm Tắt Kiến Trúc

```
OnlineMode/
├── Connection/
│   ├── EConnection.cs                 ← Facade chính
│   ├── HostConnection.cs              ← Logic Host
│   ├── ClientConnection.cs            ← Logic Client
│   └── UnityMultiplayerServices/
│       ├── EUnityMultiplayerServices.cs
│       └── UnityRelay.cs              ← Relay Protocol
│
├── ConnectionMenu/
│   ├── MatchHostConnectionMenu.cs     ← State & Methods Host
│   ├── MatchGuestConnectionMenu.cs    ← State & Methods Guest
│   └── MenuViewControllers/           ← UI binding
│
├── GameMatch/
│   └── EGameMatch.cs                  ← NetworkBehaviour chính
│                                        - RPC definitions
│                                        - Game state sync
│
└── OnlinePlayer/
    └── EOnlinePlayer.cs               ← Wrapper cho gameplay
```

---

## 🎯 Quy Trình Thêm Feature Mới

Nếu muốn thêm feature mới vào online mode:

1. **Định nghĩa RPC trong EGameMatch.cs**
   ```csharp
   [Rpc(SendTo.Server)]
   public void NewFeature_ServerRPC(/* params */)
   ```

2. **Tạo handler broadcast**
   ```csharp
   [Rpc(SendTo.Everyone)]
   private void NewFeature_ClientsAndHostRPC(/* params */)
   ```

3. **Thêm wrapper method trong EOnlinePlayer.cs**
   ```csharp
   public static void UseNewFeature(/* params */)
   {
       EGameMatch.Singleton.NewFeature_ServerRPC(/* args */);
   }
   ```

4. **Gọi từ gameplay**
   ```csharp
   EOnlinePlayer.UseNewFeature(/* values */);
   ```

---

**End of Guide**

Cập nhật: 2024
Phiên bản: 1.0
