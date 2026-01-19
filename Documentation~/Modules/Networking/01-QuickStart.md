# Quick Start

Get multiplayer running in 5 minutes.

---

## Prerequisites

- Unity 2022.3+
- Catalyst package installed
- A network backend configured (see [PackageSettings](../../Infrastructure/PackageSettings.md))

---

## Step 1: Create a NetworkController

```csharp
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;

public class NetworkController : MonoBehaviour
{
    private NetworkManager _network;
    private LobbyManager _lobby;

    void Start()
    {
        // Get services
        _network = App.Get<NetworkManager>();
        _lobby = App.Get<LobbyManager>();
        
        // Subscribe to events
        _lobby.OnServerFound += OnServerFound;
        _network.OnConnected += () => Debug.Log("Connected!");
        _network.OnDisconnected += () => Debug.Log("Disconnected");
    }

    private void OnServerFound(DiscoveryInfo info)
    {
        Debug.Log($"Server: {info.Name} ({info.PlayerCount}/{info.MaxPlayers})");
    }
}
```

---

## Step 2: Host a Game

```csharp
public async void HostGame()
{
    await _lobby.CreateLobby(new LobbyOptions
    {
        Name = "My Awesome Game",
        MaxPlayers = 4,
        Password = null  // No password
    });
    
    Debug.Log("Now hosting!");
}
```

---

## Step 3: Find and Join Games

```csharp
public void SearchForGames()
{
    _lobby.SearchForLobbies();
}

public async void JoinGame(string address)
{
    var result = await _lobby.JoinLobby(address);
    
    if (result.Success)
        Debug.Log("Joined successfully!");
    else
        Debug.LogError($"Failed: {result.Error}");
}
```

---

## Step 4: Send Messages

Define a message:

```csharp
public struct ChatMessage : INetworkMessage
{
    public string Text;
    public ulong SenderId;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Text ?? "");
        writer.Write(SenderId);
    }
    
    public void Deserialize(BinaryReader reader)
    {
        Text = reader.ReadString();
        SenderId = reader.ReadUInt64();
    }
}
```

Send it:

```csharp
_network.Send(new ChatMessage 
{ 
    Text = "Hello everyone!", 
    SenderId = _network.LocalClientId 
}, NetworkTarget.Clients);
```

Receive it:

```csharp
_network.On<ChatMessage>(msg => 
{
    Debug.Log($"Player {msg.SenderId}: {msg.Text}");
});
```

---

## Step 5: Configure PackageSettings

Open **Tools > Catalyst > Settings** and configure:

| Setting | Recommended Value |
|---------|-------------------|
| Network Backend ID | `netcode` |
| Enable Secure Connections | ✅ |
| Discovery Transport | `UdpBroadcast` (LAN) |

---

## What's Next?

- [Architecture](./02-Architecture.md) - Understand the system design
- [Communication](./04-Communication.md) - Deep dive into messaging
- [Tutorials](./13-Tutorials.md) - Complete project walkthrough
