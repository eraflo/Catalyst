# Serialization System

The Catalyst serialization system provides a unified, high-performance way to convert between C# objects and raw data (byte arrays). It is designed to handle Unity-specific types and support optimized partial deserialization.

## ISerializer Interface

The contract for all serializers in the framework:

```csharp
public interface ISerializer
{
    byte[] Serialize<T>(T obj);
    T Deserialize<T>(byte[] data);
    void Populate(byte[] data, object target);
    bool TryReadHeader<T>(byte[] data, string fieldName, out T value);
}
```

### Serialization Flow

```mermaid
graph LR
    Input[C# Object] --> C{Converters}
    C -->|Unity Types| V[Custom Converters]
    C -->|POCOs| S[Standard Serialization]
    V --> JSON[JSON String]
    S --> JSON
    JSON --> B[UTF8 Byte Array]
```

## JsonSerializer

The default implementation uses `Newtonsoft.Json` (Json.NET) and is optimized for Unity development.

### Unity Type Support
Standard JSON serializers often struggle with Unity types like `Vector3`, `Quaternion`, and `Color` because they contain internal fields or circular references. Catalyst's `JsonSerializer` includes custom converters for:
- `Vector2` / `Vector3` / `Vector4`
- `Quaternion`
- `Color` / `Color32`

### Optimized Header Reading (`TryReadHeader`)
For performance-critical operations (like reading save metadata without loading the entire state), the `JsonSerializer` supports **Partial Deserialization**.

It uses a `JsonTextReader` to scan the JSON stream and only deserialize the requested field, stopping as soon as it's found.

```mermaid
sequenceDiagram
    participant App as App Code
    participant S as JsonSerializer
    participant R as JsonTextReader
    participant B as Byte Array / Stream

    App->>S: TryReadHeader("Metadata")
    S->>R: Initialize Stream
    loop Token Scan
        R->>B: Read Next Token
        alt is PropertyName == "Metadata"
            R-->>S: Found!
            S->>R: Deserialize Current Index
            R-->>S: SaveMetadata object
            S-->>App: true (value provided)
            Note over S,R: Early Exit (Stream Closed)
        else end of object
             S-->>App: false
        end
    end
```

```csharp
// Fast metadata recovery
if (serializer.TryReadHeader<SaveMetadata>(saveData, "Metadata", out var meta))
{
    Debug.Log($"Loading save: {meta.Name}");
}
```

## Adding Custom Converters

You can extend the `JsonSerializer` by adding custom `JsonConverter` implementations to handles specialized types or third-party classes.

## Usage in Persistence

The `SaveManager` uses `ISerializer` to handle game state. You can swap the default `JsonSerializer` for a binary or compressed serializer by setting the `Serializer` property on the `SaveManager` service.

```csharp
App.Get<SaveManager>().Serializer = new MyCustomBinarySerializer();
```
