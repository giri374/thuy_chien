# 🚀 Quick Start Guide - Module Online

## ⚡ 60 Giây Khởi Động

### Bước 1: Khởi Tạo Scene
```csharp
// Trong Scene Setup
1. Create GameObject "NetworkManager"
   - Add: NetworkManager script
   - Add: UnityNetcodeTransport
   
2. Create GameObject "GameMatch"
   - Add: EGameMatch script
   - Add: NetworkObject component
```

### Bước 2: Host Tạo Phòng
```csharp
await MatchHostConnectionMenu.CreateMatch_Async();

if (EConnection.ReadyToConnect())
{
    string matchId = MatchHostConnectionMenu.MatchId;  // "ABC123"
    // Share with other player
}
```

### Bước 3: Guest Tham Gia Phòng
```csharp
await MatchGuestConnectionMenu.ConnectToMatch_Async("ABC123");

if (EConnection.ReadyToConnect())
{
    // Connected!
}
```

### Bước 4: Gửi Nước Đi Trong Game
```csharp
EOnlinePlayer.MarkCell((3, 2));  // Send action
// Server validates & broadcasts automatically
```

### Bước 5: Rời Phòng
```csharp
EOnlinePlayer.LeaveMatch();  // Disconnect
```

---

## 📚 Cấu Trúc Thư Mục

```
Assets/OnlineMode/
├── Connection/                    # Kết nối
│   ├── EConnection.cs            # 🔑 Facade chính
│   ├── HostConnection.cs
│   ├── ClientConnection.cs
│   └── UnityMultiplayerServices/
│       ├── EUnityMultiplayerServices.cs
│       └── UnityRelay.cs
│
├── ConnectionMenu/                # UI Menu
│   ├── MatchHostConnectionMenu.cs
│   └── MatchGuestConnectionMenu.cs
│
├── GameMatch/
│   └── EGameMatch.cs             # 🔑 Game state + RPC
│
└── OnlinePlayer/
    └── EOnlinePlayer.cs           # 🔑 Gameplay API
```

---

## 🎯 API Cheatsheet

### Connection API
```csharp
// Check status
EConnection.ReadyToConnect()              // bool
EConnection.StillConnectedToServer()      // bool

// Start connection
await EConnection.StartConnection_Async();             // Host
await EConnection.StartConnection_Async("ABC123");     // Guest

// Disconnect
EConnection.Disconnect();
```

### Match Management
```csharp
// Host
await MatchHostConnectionMenu.CreateMatch_Async();
string id = MatchHostConnectionMenu.MatchId;

// Guest
await MatchGuestConnectionMenu.ConnectToMatch_Async("ABC123");
```

### Gameplay
```csharp
// Send action
EOnlinePlayer.MarkCell((row, col));

// Leave match
EOnlinePlayer.LeaveMatch();

// Access game state
EGameMatch.Singleton;
```

---

## 🔄 Network Flow Tóm Tắt

### Tạo Phòng (Host)
```
Player Click "Create"
    ↓
MatchHostConnectionMenu.CreateMatch_Async()
    ↓
EConnection.StartConnection_Async()
    ↓
HostConnection → EUnityMultiplayerServices → UnityRelay
    ↓
RelayService.CreateAllocationAsync()
    ↓
RelayService.GetJoinCodeAsync()  → MatchId = "ABC123"
    ↓
NetworkManager.StartHost()
    ↓
✅ Phòng tạo, chia sẻ MatchId
```

### Tham Gia Phòng (Guest)
```
Player Input "ABC123" → Click "Join"
    ↓
MatchGuestConnectionMenu.ConnectToMatch_Async("ABC123")
    ↓
EConnection.StartConnection_Async("ABC123")
    ↓
ClientConnection → EUnityMultiplayerServices → UnityRelay
    ↓
RelayService.JoinAllocationAsync("ABC123")
    ↓
NetworkManager.StartClient()
    ↓
✅ Kết nối thành công
```

### Gửi Nước Đi (In-Game)
```
Client:
Player Click Cell(3, 2)
    ↓
EOnlinePlayer.MarkCell((3, 2))
    ↓
EGameMatch.MarkCell_ServerRPC(3, 2)  [SendTo.Server]
    ↓
    ├─────────────────────────────────────────→ SERVER
    │
    │ Server:
    │ ├─ Validate turn
    │ ├─ Check cell empty
    │ └─ Broadcast MarkCell_ClientsAndHostRPC()
    │
    │ ├───────────────────────────────→ ALL CLIENTS
    │
Client:
    │
    ├─ Receive RPC
    ├─ Update board[3][2]
    ├─ Refresh UI
    └─ Switch turn
```

---

## 🧪 Ví Dụ Thực Hiện

### Host Setup
```csharp
public class HostSetup : MonoBehaviour
{
    [SerializeField] private Text matchIdDisplay;
    [SerializeField] private Button createButton;
    
    private void Start()
    {
        createButton.onClick.AddListener(OnCreateMatch);
    }
    
    private async void OnCreateMatch()
    {
        await MatchHostConnectionMenu.CreateMatch_Async();
        
        if (EConnection.ReadyToConnect())
        {
            matchIdDisplay.text = MatchHostConnectionMenu.MatchId;
        }
    }
}
```

### Guest Setup
```csharp
public class GuestSetup : MonoBehaviour
{
    [SerializeField] private InputField codeInput;
    [SerializeField] private Button joinButton;
    
    private void Start()
    {
        joinButton.onClick.AddListener(OnJoin);
    }
    
    private async void OnJoin()
    {
        string code = codeInput.text;
        await MatchGuestConnectionMenu.ConnectToMatch_Async(code);
        
        if (EConnection.ReadyToConnect())
        {
            // Load game scene
        }
    }
}
```

### Gameplay
```csharp
public class GameBoard : MonoBehaviour
{
    public void OnCellClicked(int row, int col)
    {
        if (IsValidMove(row, col))
        {
            EOnlinePlayer.MarkCell((row, col));
        }
    }
    
    public void OnExitClicked()
    {
        EOnlinePlayer.LeaveMatch();
    }
}
```

---

## ⚠️ Common Mistakes

### ❌ Không validate input
```csharp
// Bad
await EConnection.StartConnection_Async(userInput);

// Good
if (userInput.Length == 6)
{
    await EConnection.StartConnection_Async(userInput);
}
```

### ❌ Gọi trước khi ready
```csharp
// Bad
EGameMatch.Singleton.MarkCell_ServerRPC(3, 2);  // May be null!

// Good
if (EConnection.ReadyToConnect())
{
    EOnlinePlayer.MarkCell((3, 2));
}
```

### ❌ Không handle async
```csharp
// Bad
EConnection.StartConnection_Async();  // Fire and forget

// Good
await EConnection.StartConnection_Async();
if (EConnection.ReadyToConnect())
{
    // Continue...
}
```

### ❌ RPC từ Client không validate
```csharp
// Bad - Server không check
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col)
{
    board[row, col] = mark;  // Trust client?
}

// Good
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col, RpcParams rpcParams)
{
    if (!IsValidTurn(rpcParams)) return;
    if (row < 0 || row >= 6) return;
    if (board[row, col] != null) return;
    
    board[row, col] = mark;
}
```

---

## 🐛 Debugging Tips

### Check Connection Status
```csharp
Debug.Log($"Is Listening: {EConnection.ReadyToConnect()}");
Debug.Log($"Still Connected: {EConnection.StillConnectedToServer()}");
```

### Log Network Events
```csharp
void OnNetworkSpawn()
{
    Debug.Log($"Local ClientId: {NetworkManager.Singleton.LocalClientId}");
    Debug.Log($"Is Host: {IsHost}");
    Debug.Log($"Is Owner: {IsOwner}");
}
```

### RPC Debug
```csharp
[Rpc(SendTo.Server)]
public void MarkCell_ServerRPC(int row, int col, RpcParams rpcParams)
{
    Debug.Log($"Received RPC from client: {rpcParams.Receive.SenderClientId}");
    Debug.Log($"Row: {row}, Col: {col}");
    // ...
}
```

### Relay Connection Debug
```csharp
try
{
    await EConnection.StartConnection_Async("ABC123");
}
catch (RelayServiceException ex)
{
    Debug.LogError($"Relay Error: {ex.Message}");
}
```

---

## 📊 Key Points

| Aspect | Details |
|--------|---------|
| **Arch** | Client-Server via Relay |
| **Transport** | UDP (UTP) + DTLS encryption |
| **Players** | 2 (Host + 1 Guest) |
| **Sync Method** | RPC (Server validates, broadcasts) |
| **Auth** | Anonymous (Unity Auth) |
| **Bandwidth** | ~40 bytes per action |
| **Latency** | 100-300ms (depends on region) |

---

## 🔗 File References

```
Main Entry Points:
- EConnection        → Connection facade
- EOnlinePlayer      → Gameplay API
- MatchHostConnectionMenu   → Host management
- MatchGuestConnectionMenu  → Guest management
- EGameMatch         → Game state + RPC

Detailed Docs:
- ONLINE_MODULE_GUIDE.md           → Full documentation
- NETWORK_ARCHITECTURE_DIAGRAMS.md → Visual diagrams
- API_REFERENCE.md                 → API details
```

---

**Version**: 1.0
**Updated**: 2024
