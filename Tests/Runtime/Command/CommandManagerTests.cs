using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Command;
using Eraflo.Catalyst.Command.Examples;

namespace Eraflo.Catalyst.Tests.Runtime.Command
{
    public class CommandManagerTests
    {
        private CommandManager _manager;
        private GameObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _manager = new CommandManager();
            // Mocking App.Get<CommandManager>() logic might be needed if services aren't registered
            // but for unit tests we can use the instance directly.
            _testObject = new GameObject("TestObject");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testObject);
        }

        [UnityTest]
        public IEnumerator TestExecuteAndUndo() => RunAsync(async () =>
        {
            var command = new MoveCommand(_testObject, Vector3.one);
            Vector3 startPos = _testObject.transform.position;

            await _manager.Execute(command);
            Assert.AreEqual(Vector3.one, _testObject.transform.position);
            Assert.AreEqual(1, _manager.UndoCount);

            await _manager.Undo();
            Assert.AreEqual(startPos, _testObject.transform.position);
            Assert.AreEqual(0, _manager.UndoCount);
            Assert.AreEqual(1, _manager.RedoCount);
        });

        [UnityTest]
        public IEnumerator TestRedo() => RunAsync(async () =>
        {
            var command = new MoveCommand(_testObject, Vector3.one);
            await _manager.Execute(command);
            await _manager.Undo();
            await _manager.Redo();

            Assert.AreEqual(Vector3.one, _testObject.transform.position);
            Assert.AreEqual(1, _manager.UndoCount);
            Assert.AreEqual(0, _manager.RedoCount);
        });

        [UnityTest]
        public IEnumerator TestHistoryLimit() => RunAsync(async () =>
        {
            _manager.MaxHistorySize = 2;
            
            await _manager.Execute(new MoveCommand(_testObject, Vector3.one));
            await _manager.Execute(new MoveCommand(_testObject, Vector3.up));
            await _manager.Execute(new MoveCommand(_testObject, Vector3.right));

            Assert.AreEqual(2, _manager.UndoCount);
        });

        [UnityTest]
        public IEnumerator TestCompositeCommand() => RunAsync(async () =>
        {
            var composite = new CompositeCommand();
            composite.Add(new MoveCommand(_testObject, Vector3.one));
            composite.Add(new MoveCommand(_testObject, Vector3.up));

            await _manager.Execute(composite);
            Assert.AreEqual(Vector3.up, _testObject.transform.position);

            await _manager.Undo();
            Assert.AreEqual(Vector3.zero, _testObject.transform.position);
        });

        private IEnumerator RunAsync(System.Func<Task> task)
        {
            var t = task();
            while (!t.IsCompleted) yield return null;
            if (t.IsFaulted) throw t.Exception;
        }
    }
}
