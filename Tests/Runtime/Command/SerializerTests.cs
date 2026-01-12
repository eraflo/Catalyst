using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.Command;
using Eraflo.Catalyst.Command.Examples;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Tests.Runtime.Command
{
    public class SerializerTests
    {
        private JsonSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _serializer = new JsonSerializer();
        }

        [Test]
        public void TestMoveCommandSerialization()
        {
            var original = new MoveCommand(null, new Vector3(1, 2, 3));
            byte[] data = _serializer.Serialize(original);
            
            var deserialized = new MoveCommand();
            _serializer.Populate(data, deserialized);

            Assert.AreEqual(original.NewPosition, deserialized.NewPosition);
        }

        [Test]
        public void TestCompositeCommandSerialization()
        {
            var composite = new CompositeCommand();
            composite.Add(new MoveCommand(null, Vector3.one));
            composite.Add(new MoveCommand(null, Vector3.up));

            byte[] data = _serializer.Serialize(composite);
            
            var deserialized = new CompositeCommand();
            _serializer.Populate(data, deserialized);
            
            Assert.AreEqual(2, System.Linq.Enumerable.Count(deserialized.Commands));
        }
        [Test]
        public void TestGameObjectSerialization()
        {
            var go = new GameObject("SerializedGo");
            var original = new MoveCommand(go, Vector3.one);
            
            byte[] data = _serializer.Serialize(original);
            
            // Cleanup original
            Object.DestroyImmediate(go);
            
            // Re-create it (to simulate reloading a scene or finding it)
            var recreated = new GameObject("SerializedGo");
            
            var deserialized = new MoveCommand();
            _serializer.Populate(data, deserialized);
            
            Assert.IsNotNull(deserialized.Target);
            Assert.AreEqual("SerializedGo", deserialized.Target.name);
            
            Object.DestroyImmediate(recreated);
        }
    }
}
