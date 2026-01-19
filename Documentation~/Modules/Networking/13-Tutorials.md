# Tutorials

Step-by-step guides for common scenarios.

---

## Tutorial 1: Basic LAN Multiplayer

Build a simple 2-4 player LAN game.

### Prerequisites

- Unity 2022.3+
- Catalyst package installed
- Unity Netcode for GameObjects (for production)

### Step 1: Create NetworkController

Create a new script `NetworkController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using System.Collections.Generic;

public class NetworkController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _searchButton;
    [SerializeField] private Transform _serverListContent;
    [SerializeField] private GameObject _serverItemPrefab;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _gamePanel;

    private LobbyManager _lobby;
    private NetworkManager _network;
    private List<DiscoveryInfo> _servers = new();

    void Start()
    {
        // Get services
        _lobby = App.Get<LobbyManager>();
        _network = App.Get<NetworkManager>();

        // Setup buttons
        _hostButton.onClick.AddListener(OnHostClicked);
        _searchButton.onClick.AddListener(OnSearchClicked);

        _network.OnConnected += OnConnected;
        _network.OnDisconnected += OnDisconnected;
    }

    private async void OnHostClicked()
    {
        await _lobby.CreateLobby(new LobbyOptions
        {
            Name = $"Player's Game",
            MaxPlayers = 4
        });
    }

    private async void OnSearchClicked()
    {
        ClearServerList();
        var lobbies = await _lobby.SearchLobbies();
        foreach (var info in lobbies)
        {
            AddLobbyItem(info);
        }
    }

    private void AddLobbyItem(LobbyInfo info)
    {
        _servers.Add(info); // Using LobbyInfo list
        
        var item = Instantiate(_serverItemPrefab, _serverListContent);
        var button = item.GetComponent<Button>();
        var text = item.GetComponentInChildren<Text>();
        
        text.text = $"{info.Name} ({info.PlayerCount}/{info.MaxPlayers})";
        
        int index = _servers.Count - 1;
        button.onClick.AddListener(() => OnJoinClicked(index));
    }

    private async void OnJoinClicked(int index)
    {
        var server = _servers[index];
        var result = await _lobby.JoinLobby(server.JoinCode);
        
        if (!result.Success)
        {
            Debug.LogError($"Failed to join: {result.Error}");
        }
    }

    private void OnConnected()
    {
        _lobbyPanel.SetActive(false);
        _gamePanel.SetActive(true);
        Debug.Log("Connected to game!");
    }

    private void OnDisconnected()
    {
        _lobbyPanel.SetActive(true);
        _gamePanel.SetActive(false);
        ClearServerList();
    }

    private void ClearServerList()
    {
        _servers.Clear();
        foreach (Transform child in _serverListContent)
            Destroy(child.gameObject);
    }
}
```

### Step 2: Create UI

1. Create Canvas with two panels: Lobby and Game
2. Add Host and Search buttons to Lobby panel
3. Add ScrollView with Content for server list
4. Create ServerItem prefab with Button and Text

### Step 3: Configure PackageSettings

1. Open **Tools > Catalyst > Settings**
2. Set Network Backend ID: `netcode`
3. Set Discovery Transport: `UdpBroadcast`

### Step 4: Test

1. Build and run two instances
2. Click "Host" in one instance
3. Click "Search" in the other
4. Click the server entry to join

---

## Tutorial 2: Password-Protected Lobbies

Add password protection to your lobbies.

### Step 1: Update Host UI

Add password input field:

```csharp
[SerializeField] private InputField _passwordInput;

private async void OnHostClicked()
{
    string password = _passwordInput.text;
    
    await _lobby.CreateLobby(new LobbyOptions
    {
        Name = "Private Game",
        MaxPlayers = 4,
        Password = string.IsNullOrEmpty(password) ? null : password
    });
}
```

### Step 2: Update Join Logic

```csharp
private async void OnJoinClicked(int index)
{
    var server = _servers[index];
    
    if (server.IsPasswordProtected)
    {
        string password = await ShowPasswordDialog();
        if (string.IsNullOrEmpty(password)) return;
        
        var result = await _lobby.JoinLobby(server.JoinCode, password);
        HandleJoinResult(result);
    }
    else
    {
        var result = await _lobby.JoinLobby(server.JoinCode);
        HandleJoinResult(result);
    }
}

private void HandleJoinResult(LobbyResult result)
{
    if (!result.Success)
    {
        if (result.Message.Contains("password"))
            ShowError("Incorrect password");
        else
            ShowError(result.Message);
    }
}
```

### Step 3: Show Lock Icon

```csharp
private void OnServerFound(DiscoveryInfo info)
{
    var item = Instantiate(_serverItemPrefab, _serverListContent);
    var lockIcon = item.transform.Find("LockIcon");
    
    lockIcon.gameObject.SetActive(info.HasPassword);
}
```

---

## Tutorial 3: Chat System

Implement networked chat.

### Step 1: Define Message

```csharp
public struct ChatMessage : INetworkMessage
{
    [MaxLength(256)]
    public string Text;
    public ulong SenderId;
    public float Timestamp;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Text ?? "");
        writer.Write(SenderId);
        writer.Write(Timestamp);
    }

    public void Deserialize(BinaryReader reader)
    {
        Text = reader.ReadSafeString(256);
        SenderId = reader.ReadUInt64();
        Timestamp = reader.ReadSingle();
    }
}
```

### Step 2: Create Chat Handler

```csharp
[RateLimit(maxMessages: 5, windowSeconds: 1.0f)]
public class ChatHandler : INetworkMessageHandler<ChatMessage>
{
    public event Action<string, ulong> OnChatReceived;
    
    public void Handle(ChatMessage msg)
    {
        OnChatReceived?.Invoke(msg.Text, msg.SenderId);
    }
}
```

### Step 3: Create Chat UI

```csharp
public class ChatUI : MonoBehaviour
{
    [SerializeField] private InputField _inputField;
    [SerializeField] private Text _chatLog;
    [SerializeField] private Button _sendButton;

    private NetworkManager _network;
    private ChatHandler _handler;

    void Start()
    {
        _network = App.Get<NetworkManager>();
        _handler = new ChatHandler();
        
        _network.On<ChatMessage>(_handler.Handle);
        _handler.OnChatReceived += AddMessage;
        
        _sendButton.onClick.AddListener(SendMessage);
        _inputField.onEndEdit.AddListener(text => 
        {
            if (Input.GetKeyDown(KeyCode.Return)) SendMessage();
        });
    }

    private void SendMessage()
    {
        if (string.IsNullOrEmpty(_inputField.text)) return;
        
        var msg = new ChatMessage
        {
            Text = _inputField.text,
            SenderId = _network.LocalClientId,
            Timestamp = Time.time
        };
        
        // Send to all (via server)
        if (_network.IsServer)
            _network.Send(msg, NetworkTarget.Clients);
        else
            _network.Send(msg, NetworkTarget.Server);
        
        // Show locally
        AddMessage(msg.Text, msg.SenderId);
        _inputField.text = "";
    }

    private void AddMessage(string text, ulong senderId)
    {
        string playerName = GetPlayerName(senderId);
        _chatLog.text += $"\n[{playerName}]: {text}";
    }

    void OnDestroy()
    {
        _network.Off<ChatMessage>(_handler.Handle);
    }
}
```

### Step 4: Server Relay

On server, relay chat to all clients:

```csharp
// In your server logic
_network.On<ChatMessage>(msg =>
{
    // Relay to OTHER clients
    _network.Send(msg, NetworkTarget.Others);
});
```

---

## Tutorial 4: Secure Authentication

Implement token-based authentication.

### Step 1: Define Auth Payload

```csharp
public struct AuthPayload : INetworkMessage
{
    [MaxLength(64)]
    public string Username;
    
    [MaxLength(512)]
    public string Token;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Username ?? "");
        writer.Write(Token ?? "");
    }

    public void Deserialize(BinaryReader reader)
    {
        Username = reader.ReadSafeString(64);
        Token = reader.ReadSafeString(512);
    }
}
```

### Step 2: Client Login

```csharp
public class AuthManager : MonoBehaviour
{
    private ConnectionManager _connection;

    void Start()
    {
        _connection = App.Get<ConnectionManager>();
    }

    public void Login(string username, string token)
    {
        _connection.SetPayload(new AuthPayload
        {
            Username = username,
            Token = token
        });
    }
}
```

### Step 3: Server Validation

```csharp
public class ServerAuth : MonoBehaviour
{
    private ConnectionManager _connection;
    private HashSet<string> _validTokens = new();

    void Start()
    {
        _connection = App.Get<ConnectionManager>();
        _connection.OnValidateConnection += ValidateClient;
    }

    private ConnectionResponse ValidateClient(ConnectionRequest request)
    {
        var auth = request.GetPayload<AuthPayload>();
        
        // Validate username
        if (string.IsNullOrEmpty(auth.Username))
        {
            return ConnectionResponse.Reject("Username required");
        }
        
        if (auth.Username.Length < 3)
        {
            return ConnectionResponse.Reject("Username too short");
        }
        
        // Validate token (implement your auth logic)
        if (!ValidateToken(auth.Username, auth.Token))
        {
            return ConnectionResponse.Reject("Invalid credentials");
        }
        
        // Check if already connected
        if (IsUsernameOnline(auth.Username))
        {
            return ConnectionResponse.Reject("Already connected");
        }
        
        // Store player data
        PlayerRegistry.Add(request.ClientId, auth.Username);
        
        Debug.Log($"Player {auth.Username} connected!");
        return ConnectionResponse.Success();
    }

    private bool ValidateToken(string username, string token)
    {
        // Implement your authentication logic
        // e.g., validate JWT, check database, etc.
        return _validTokens.Contains(token);
    }
}
```

---

## Summary
You've now seen how to build basic lobbies, handle discovery, and implement secure synchronized communication.

---

## See Also

- [Quick Start](./01-QuickStart.md) - 5-minute setup
- [Security Guide](./11-SecurityGuide.md) - Security best practices
- [API Reference](./12-API.md) - Complete API
