# 🔌 Sơ Đồ Chi Tiết - Module Online Thủy Chiến

## 1️⃣ Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────────────────────────┐
│                    UNITY RELAY SERVER                        │
│                 (Cloud Infrastructure)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │ (UDP over UTP + DTLS)
        ┌──────────────┴──────────────┐
        │                             │
     ┌──▼──────────────┐      ┌───────▼──────┐
     │      HOST       │      │    GUEST     │
     │   (Server)      │──────│   (Client)   │
     │                 │  RPC │              │
     └─────────────────┘      └──────────────┘
        │                          │
        ├─ NetworkManager          ├─ NetworkManager
        │  (Singleton)             │  (Singleton)
        │                          │
        ├─ EGameMatch              ├─ EGameMatch
        │  (NetworkBehaviour)      │  (NetworkBehaviour)
        │                          │
        └─ GameLogic               └─ GameLogic
```

## 2️⃣ Class Relationship Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                        GAMEPLAY CODE                          │
│  (GameBoardUI, GameplayController, etc)                      │
└────────────────────┬─────────────────────────────────────────┘
                     │ Calls
                     ▼
        ┌────────────────────────┐
        │    EOnlinePlayer       │  ← Entry Point for Gameplay
        │  (Static Wrapper)      │
        └──────────┬─────────────┘
                   │ Delegates to
        ┌──────────▼──────────────┐
        │   EGameMatch            │
        │ (NetworkBehaviour)      │
        │ - Singleton pattern     │
        │ - RPC definitions       │
        │ - Game state sync       │
        └──────────┬──────────────┘
                   │ Uses
        ┌──────────▼──────────────────────┐
        │    EConnection                   │
        │  (Static Connection Facade)      │
        │                                  │
        │  ├─ ReadyToConnect()             │
        │  ├─ StartConnection_Async()      │
        │  ├─ Disconnect()                 │
        │  └─ StillConnectedToServer()     │
        └──────────┬──────────────────────┘
                   │ Delegates to
        ┌──────────┴──────────────────────┐
        │                                  │
     ┌──▼─────────────────┐   ┌──────────▼──────┐
     │ HostConnection     │   │ ClientConnection│
     │ - CreateAlloc      │   │ - JoinAlloc     │
     │ - GetJoinCode      │   │ - StartClient   │
     │ - StartHost        │   │                 │
     └──────────┬─────────┘   └────────┬────────┘
                │                      │
                └──────────┬───────────┘
                           │ Both use
        ┌──────────────────▼────────────────┐
        │  EUnityMultiplayerServices         │
        │                                    │
        │  ├─ StartRelay_Async()             │
        │  ├─ StartRelay_Async(joinCode)    │
        │  └─ Init + SignIn                 │
        └──────────────────┬────────────────┘
                           │ Uses
        ┌──────────────────▼────────────────┐
        │     UnityRelay                     │
        │  (Relay Protocol Handler)          │
        │                                    │
        │  ├─ CreateAllocation()             │
        │  ├─ JoinAllocation()               │
        │  ├─ GetJoinCode()                  │
        │  └─ SetRelayServerData()           │
        └──────────────────┬────────────────┘
                           │ Communicates with
        ┌──────────────────▼────────────────┐
        │   Unity Relay Service (Cloud)     │
        │   Unity Authentication Service    │
        └────────────────────────────────────┘
```

## 3️⃣ Host Creation Flow - Detailed Sequence Diagram

```
Host Player                     Code Layer              Relay Service
     │                              │                        │
     │ Click "Create Match"         │                        │
     ├──────────────────────────────>                        │
     │                              │                        │
     │                    MatchHostConnectionMenu            │
     │                    .CreateMatch_Async()               │
     │                              │                        │
     │                       EConnection                     │
     │                  .StartConnection_Async()             │
     │                              │                        │
     │                   HostConnection                      │
     │               .StartConnection_Async()                │
     │                              │                        │
     │                  EUnityMultiplayerServices            │
     │                              │                        │
     │                   Initialize Services                 │
     │                              ├───────────────────────>│
     │                              │   Initialize           │
     │                              │<───────────────────────┤
     │                              │   ✅ Initialized       │
     │                              │                        │
     │                   SignIn Anonymously                  │
     │                              ├───────────────────────>│
     │                              │   Sign In              │
     │                              │<───────────────────────┤
     │                              │   ✅ Signed In         │
     │                              │                        │
     │                        UnityRelay                     │
     │                              │                        │
     │                CreateAllocationAsync()                │
     │                              ├───────────────────────>│
     │                              │   Allocate Resources   │
     │                              │<───────────────────────┤
     │                              │ Allocation Object      │
     │                              │ (IP, Port, Keys)       │
     │                              │                        │
     │                GetJoinCodeAsync()                     │
     │                              ├───────────────────────>│
     │                              │   Generate Code        │
     │                              │<───────────────────────┤
     │                              │ JoinCode: "ABC123"     │
     │                              │                        │
     │                              │ MatchHostConnectionMenu│
     │                              │  .MatchId = "ABC123"   │
     │                              │                        │
     │                SetRelayServerData()                   │
     │                              │ (Configure Transport)  │
     │                              │                        │
     │               NetworkManager.StartHost()              │
     │                              │ (Listen on Relay)      │
     │                              │                        │
     │<─────────────────────────────┤                        │
     │ ✅ Match Created              │                        │
     │ MatchId: "ABC123"             │                        │
     │ (Share with other player)     │                        │
```

## 4️⃣ Guest Join Flow - Detailed Sequence Diagram

```
Guest Player        MatchGuestConnectionMenu      UnityRelay      Relay Service
     │                         │                      │                 │
     │ Input: "ABC123"         │                      │                 │
     │ Click "Join"            │                      │                 │
     ├────────────────────────>│                      │                 │
     │                         │                      │                 │
     │           ConnectToMatch_Async("ABC123")       │                 │
     │                         │                      │                 │
     │                   EConnection                  │                 │
     │          .StartConnection_Async("ABC123")      │                 │
     │                         │                      │                 │
     │                ClientConnection                │                 │
     │        .StartConnection_Async("ABC123")        │                 │
     │                         │                      │                 │
     │              [Validate: Length == 6] ✅        │                 │
     │                         │                      │                 │
     │                  EUnityMultiplayerServices     │                 │
     │                         │                      │                 │
     │              Initialize + SignIn               │                 │
     │                         ├─────────────────────>│                 │
     │                         │                      ├────────────────>│
     │                         │                      │    Initialize   │
     │                         │                      │<────────────────┤
     │                         │                      │   ✅ Done       │
     │                         │                      │                 │
     │                   UnityRelay                   │                 │
     │                         │                      │                 │
     │              JoinAllocationAsync("ABC123")     │                 │
     │                         ├─────────────────────>│                 │
     │                         │                      ├────────────────>│
     │                         │                      │ Verify Code     │
     │                         │                      │ Get Host EP     │
     │                         │                      │<────────────────┤
     │                         │                      │ JoinAllocation  │
     │                         │                      │ (Host IP:Port)  │
     │                         │<─────────────────────┤                 │
     │                         │                      │                 │
     │             SetRelayServerData()               │                 │
     │                         │ (Configure to Host)  │                 │
     │                         │                      │                 │
     │            NetworkManager.StartClient()        │                 │
     │                         │ (Connect to Host)    │                 │
     │                         ├─────────────────────────────────────> │
     │                         │                      │                 │
     │<────────────────────────┤                      │                 │
     │ ✅ Connected!           │                      │                 │
     │ Waiting for game load   │                      │                 │
```

## 5️⃣ RPC Call Flow (Gameplay)

```
Player Action Timeline:

HOST SIDE:                      RELAY NETWORK              CLIENT SIDE:
(Server)                        (Netcode P2P)              (Client)

    Board UI                                                Board UI
       │                                                        │
       │ Player marks cell                    Player sees
       │ (4, 3)                              pending state
       │                                         (grey cell)
       │
  GameplayController
       │
  EOnlinePlayer
  .MarkCell((4,3))
       │
  EGameMatch
  .MarkCell_ServerRPC
  (4, 3)
       │
       ├─ [RPC Serialized]      ═════════════════════════════════════>
       │  (row: 4, col: 3)
       │                                                        Receive
       │                                                        RPC
       │                                                           │
       │◄════════════════════════════════════════════════════════┤
       │ [Broadcast: MarkCell_ClientsAndHostRPC]                 │
       │ (4, 3)                                                   │
       │                                                        Update
       │                                                        Board
    Update Board
    Refresh UI
    Switch Turn
       │
    ✅ Done

    Total Latency: ~100-200ms (depends on internet)
```

## 6️⃣ Network State Synchronization

```
┌──────────────────────────────────────────────┐
│         NetworkBehaviour (EGameMatch)        │
├──────────────────────────────────────────────┤
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │    Option 1: NetworkVariable           │  │
│  │    (Auto-sync on value change)         │  │
│  │                                        │  │
│  │  private NetworkVariable<GameState>    │  │
│  │         CurrentGameState = new()       │  │
│  │                                        │  │
│  │  When: CurrentGameState.Value = ...    │  │
│  │  Then: Auto broadcast to all clients   │  │
│  │                                        │  │
│  │  ✅ Pros: Simple, automatic           │  │
│  │  ❌ Cons: Full state sync (bandwidth)  │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │    Option 2: RPC (used here)           │  │
│  │    (Manual, per-action)                │  │
│  │                                        │  │
│  │  [Rpc(SendTo.Server)]                  │  │
│  │  public void MarkCell_ServerRPC(...)   │  │
│  │                                        │  │
│  │  [Rpc(SendTo.Everyone)]                │  │
│  │  private void                          │  │
│  │    MarkCell_ClientsAndHostRPC(...)     │  │
│  │                                        │  │
│  │  ✅ Pros: Bandwidth efficient         │  │
│  │  ❌ Cons: No auto-sync on reconnect   │  │
│  └────────────────────────────────────────┘  │
│                                              │
└──────────────────────────────────────────────┘

Current Implementation: Hybrid RPC Approach
- RPC for user actions (MarkCell)
- (Commented) NetworkVariable for state sync
- Manual sync needed on client reconnection
```

## 7️⃣ Error Handling & Recovery

```
                    Connection Error
                          │
            ┌─────────────┴─────────────┐
            ▼                           ▼
    Authentication Failed    Network Connection Failed
            │                           │
            ├─ Check credentials        ├─ Check internet
            ├─ Retry signing in         ├─ Verify Relay
            └─ Show error msg           └─ Retry connection
                                              │
                                              ├─ Attempt 1
                                              ├─ Attempt 2
                                              ├─ Attempt 3
                                              │
                                        Max attempts exceeded
                                              │
                                        Return to Menu
```

## 8️⃣ Lifecycle Timeline

```
Game Session Lifecycle:

[MENU SCENE]
    │
    ├─ CreateMatch ─────────────────┐
    │                               ▼
    │                      [LOADING RELAY]
    │                               │
    │                      UnityServices Init
    │                      Relay Allocation
    │                               │
    │                        ✅ MatchId ready
    └───────────────────────────────┤
         (Share MatchId)             │
                                     ▼
ConnectToMatch ──────────> [LOADING RELAY]
         │                           │
         │                  Join Relay with Code
         │                           │
         └──────────────────────────>✅ Connected
                                     │
                                     ▼
                            [GAME SCENE LOAD]
                            OnNetworkSpawn()
                                     │
         ┌──────────────────────────┴────────────────┐
         ▼                                           ▼
    [GAMEPLAY]                             [WAITING FOR OTHER]
    - Send RPC                                     │
    - Receive RPC                        Wait for all clients
    - Update Board                       connected
    - Switch Turn                               │
         │                                     ▼
         └──────────────────────────────>[GAMEPLAY START]
                                              │
                                    ┌────────┴────────┐
                                    ▼                 ▼
                            Player Clicks      Other Player
                            "Leave Match"      Disconnects
                                    │                 │
                                    └────────┬────────┘
                                             ▼
                                  [CLEANUP & DISCONNECT]
                                  NetworkManager.Shutdown()
                                             │
                                             ▼
                                      [MENU SCENE]
```

## 9️⃣ Component Interaction Map

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  GAMEPLAY LAYER                                            │
│  ┌────────────────────────────────────────────────────┐   │
│  │ GameBoardUI, GameplayController, PlayerInputHandler  │   │
│  └────────┬─────────────────────────────────────────────┘   │
│           │                                                 │
│  ONLINE API LAYER                                          │
│  ┌────────▼─────────────────────────────────────────────┐   │
│  │              EOnlinePlayer (Static)                    │   │
│  │  ├─ MarkCell((row, col))                            │   │
│  │  └─ LeaveMatch()                                    │   │
│  └────────┬─────────────────────────────────────────────┘   │
│           │                                                 │
│  GAME STATE LAYER                                          │
│  ┌────────▼─────────────────────────────────────────────┐   │
│  │         EGameMatch (NetworkBehaviour)                 │   │
│  │  ├─ MarkCell_ServerRPC() [Rpc(SendTo.Server)]       │   │
│  │  ├─ MarkCell_ClientsAndHostRPC() [Broadcast]        │   │
│  │  ├─ Singleton                                       │   │
│  │  └─ AllPlayerClientIds                             │   │
│  └────────┬─────────────────────────────────────────────┘   │
│           │                                                 │
│  NETWORK CONTROL LAYER                                     │
│  ┌────────▼─────────────────────────────────────────────┐   │
│  │          EConnection (Static Facade)                  │   │
│  │  ├─ ReadyToConnect()                                │   │
│  │  ├─ StartConnection_Async()                         │   │
│  │  ├─ StartConnection_Async(joinString)               │   │
│  │  ├─ Disconnect()                                    │   │
│  │  └─ StillConnectedToServer()                        │   │
│  └──┬────┬────────────────────────────────────┬────┬──────┘   │
│     │    │                                    │    │          │
│  ROLE-SPECIFIC LAYERS                                       │
│  ┌──▼─┐ ┌────────────────────────────┐  ┌───▼┐  │          │
│  │HOST│ │CONNECTION ORCHESTRATION    │  │CLI │  │          │
│  ├──┬─┤ ├────────────────────────────┤  ├───┬┘  │          │
│  │  │ │  EUnityMultiplayerServices   │  │   │   │          │
│  │  │ │  ├─ Initialize              │  │   │   │          │
│  │  │ │  └─ SignIn                  │  │   │   │          │
│  │  │ │                              │  │   │   │          │
│  │  │ │  UnityRelay                 │  │   │   │          │
│  │  │ │  ├─ CreateAllocation()      │  │   │   │          │
│  │  │ │  ├─ JoinAllocation()        │  │   │   │          │
│  │  │ │  └─ SetRelayServerData()    │  │   │   │          │
│  │  │ │                              │  │   │   │          │
│  │  │ │  Relay Service (Cloud)      │  │   │   │          │
│  └──┴─┴──────────────────────────────┴──┴───┴───┘          │
│                                                             │
│  TRANSPORT LAYER                                           │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  NetworkManager.NetworkConfig.NetworkTransport       │  │
│  │  └─ UnityTransport (UDP over UTP + DTLS)            │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  CLOUD INFRASTRUCTURE                                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Unity Relay Server                                  │  │
│  │  Unity Authentication Service                        │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 🔟 State Machine - Match Lifecycle

```
┌─────────────────┐
│   MENU STATE    │
└────────┬────────┘
         │ Click "Create" or "Join"
         ▼
    ┌─────────────────────┐
    │  CONNECTING STATE   │
    │ ┌─────────────────┐ │
    │ │ Init Services   │ │
    │ ├─────────────────┤ │
    │ │ Auth Sign-in    │ │
    │ ├─────────────────┤ │
    │ │ Relay Init      │ │
    │ ├─────────────────┤ │
    │ │ Set Transport   │ │
    │ ├─────────────────┤ │
    │ │ Start Host/Client │
    │ └─────────────────┘ │
    └─────────┬───────────┘
              │ Success
              ▼
    ┌─────────────────────┐
    │  WAITING STATE      │
    │ (Clients Connected) │
    │ Waiting for all     │
    │ players to spawn    │
    └─────────┬───────────┘
              │ All players spawned
              ▼
    ┌─────────────────────┐
    │  PLAYING STATE      │
    │ ┌─────────────────┐ │
    │ │ Turn-based loop │ │
    │ ├─────────────────┤ │
    │ │ Send RPC        │ │
    │ ├─────────────────┤ │
    │ │ Sync board      │ │
    │ ├─────────────────┤ │
    │ │ Check win cond  │ │
    │ └─────────────────┘ │
    └─────────┬───────────┘
              │ Match ends (win/draw/disconnect)
              ▼
    ┌─────────────────────┐
    │  CLEANUP STATE      │
    │ ├─────────────────┤ │
    │ │ NetworkManager  │ │
    │ │  .Shutdown()    │ │
    │ └─────────────────┘ │
    └─────────┬───────────┘
              │
              ▼
    ┌─────────────────────┐
    │   MENU STATE        │ ← Back to start
    └─────────────────────┘
```

---

**Diagram Completed** ✅

Tất cả các sơ đồ này giúp bạn hiểu rõ:
- Kiến trúc tổng thể
- Quan hệ giữa các class
- Luồng tạo phòng và tham gia
- Luồng gọi RPC
- Đồng bộ trạng thái
- Xử lý lỗi
- Vòng đời của một phiên chơi
