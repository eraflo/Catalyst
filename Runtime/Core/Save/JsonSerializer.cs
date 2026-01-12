using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Core.Save
{
    /// <summary>
    /// JSON implementation of ISerializer using Newtonsoft.Json.
    /// Handles Unity types (Vector3, Quaternion, etc.) via converters.
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            
            _settings.Converters.Add(new UnityTypeConverter());
        }

        public byte[] Serialize<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        public void Populate(byte[] data, object target)
        {
            string json = Encoding.UTF8.GetString(data);
            JsonConvert.PopulateObject(json, target, _settings);
        }

        public bool TryReadHeader<T>(byte[] data, string fieldName, out T value)
        {
            value = default;
            try
            {
                using var stream = new MemoryStream(data);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var jsonReader = new JsonTextReader(reader);

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == fieldName)
                    {
                        jsonReader.Read(); // Move to the value
                        var serializer = Newtonsoft.Json.JsonSerializer.Create(_settings);
                        value = serializer.Deserialize<T>(jsonReader);
                        return true;
                    }

                    // Stop if we exit the root object before finding the field
                    if (jsonReader.Depth > 1) continue; 
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonSerializer] Failed to read header field '{fieldName}': {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Custom converter for common Unity types to ensure they are serialized correctly.
        /// </summary>
        private class UnityTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3) || 
                       objectType == typeof(Vector2) || 
                       objectType == typeof(Quaternion) ||
                       objectType == typeof(Color) ||
                       objectType == typeof(GameObject);
            }

            public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value is Vector3 v3)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("x"); writer.WriteValue(v3.x);
                    writer.WritePropertyName("y"); writer.WriteValue(v3.y);
                    writer.WritePropertyName("z"); writer.WriteValue(v3.z);
                    writer.WriteEndObject();
                }
                else if (value is Vector2 v2)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("x"); writer.WriteValue(v2.x);
                    writer.WritePropertyName("y"); writer.WriteValue(v2.y);
                    writer.WriteEndObject();
                }
                else if (value is Quaternion q)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("x"); writer.WriteValue(q.x);
                    writer.WritePropertyName("y"); writer.WriteValue(q.y);
                    writer.WritePropertyName("z"); writer.WriteValue(q.z);
                    writer.WritePropertyName("w"); writer.WriteValue(q.w);
                    writer.WriteEndObject();
                }
                else if (value is Color c)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("r"); writer.WriteValue(c.r);
                    writer.WritePropertyName("g"); writer.WriteValue(c.g);
                    writer.WritePropertyName("b"); writer.WriteValue(c.b);
                    writer.WritePropertyName("a"); writer.WriteValue(c.a);
                    writer.WriteEndObject();
                }
                else if (value is GameObject go)
                {
                    writer.WriteStartObject();
                    var entity = go.GetComponent<SaveableEntity>();
                    if (entity != null)
                    {
                        writer.WritePropertyName("guid");
                        writer.WriteValue(entity.Guid);
                    }
                    writer.WritePropertyName("path");
                    writer.WriteValue(GetGameObjectPath(go));
                    writer.WriteEndObject();
                }
            }

            private string GetGameObjectPath(GameObject go)
            {
                string path = "/" + go.name;
                while (go.transform.parent != null)
                {
                    go = go.transform.parent.gameObject;
                    path = "/" + go.name + path;
                }
                return path;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                JObject jo = JObject.Load(reader);
                if (objectType == typeof(Vector3))
                    return new Vector3((float)jo["x"], (float)jo["y"], (float)jo["z"]);
                if (objectType == typeof(Vector2))
                    return new Vector2((float)jo["x"], (float)jo["y"]);
                if (objectType == typeof(Quaternion))
                    return new Quaternion((float)jo["x"], (float)jo["y"], (float)jo["z"], (float)jo["w"]);
                if (objectType == typeof(Color))
                    return new Color((float)jo["r"], (float)jo["g"], (float)jo["b"], (float)jo["a"]);
                
                if (objectType == typeof(GameObject))
                {
                    string guid = (string)jo["guid"];
                    if (!string.IsNullOrEmpty(guid))
                    {
                        var entity = App.Get<SaveManager>()?.GetEntity(guid);
                        if (entity != null) return entity.gameObject;
                    }

                    string path = (string)jo["path"];
                    if (!string.IsNullOrEmpty(path))
                    {
                        return GameObject.Find(path);
                    }
                }

                return null;
            }
        }
    }
}
