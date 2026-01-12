using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Command;
using Eraflo.Catalyst.Command.Examples;
using Eraflo.Catalyst.Core.Save;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Tests.Runtime.Command
{
    public class ReplayTests
    {
        private CommandManager _commandManager;
        private EventBus _eventBus;
        private SaveManager _saveManager;
        private GameObject _testObject;
        private GameObject _coroutineHost;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestObject");
            _coroutineHost = new GameObject("CoroutineHost");
            
            // Setup services manually since App.Get might fail in unit tests if not initialized
            _eventBus = new EventBus();
            _saveManager = new SaveManager { Serializer = new JsonSerializer(), Storage = new LocalDiskStorage() };
            _commandManager = new CommandManager();
            
            // Manual registration for this test instance context
            // In a real Catalyst environment, App.Initialize handles this.
            App.Register<EventBus>(_eventBus);
            App.Register<SaveManager>(_saveManager);
            App.Register<CommandManager>(_commandManager);
            
            _commandManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testObject);
            Object.DestroyImmediate(_coroutineHost);
            App.Shutdown();
        }

        [UnityTest]
        public IEnumerator TestRecordAndPlayback() => RunAsync(async () =>
        {
            var recorder = new ReplayRecorder("TestReplay");
            recorder.Start();

            // Record a few moves
            await _commandManager.Execute(new MoveCommand(_testObject, Vector3.one));
            await Task.Delay(100); // Small delay to test timing
            await _commandManager.Execute(new MoveCommand(_testObject, Vector3.up));
            
            recorder.Stop();
            Assert.AreEqual(2, recorder.Track.Frames.Count);

            // Reset object
            _testObject.transform.position = Vector3.zero;

            // Playback
            var player = new ReplayPlayer(recorder.Track, _coroutineHost.AddComponent<MonoBehaviourStub>(), _testObject);
            bool finished = false;
            player.OnPlaybackFinished += () => finished = true;
            
            player.Play();

            float timeout = Time.time + 2f;
            while (!finished && Time.time < timeout) await Task.Yield();

            Assert.IsTrue(finished, "Playback did not finish in time");
            Assert.AreEqual(Vector3.up, _testObject.transform.position);
        });

        private IEnumerator RunAsync(System.Func<Task> task)
        {
            var t = task();
            while (!t.IsCompleted) yield return null;
            if (t.IsFaulted) throw t.Exception;
        }

        private class MonoBehaviourStub : MonoBehaviour { }
    }
}
