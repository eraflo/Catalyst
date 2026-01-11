using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.EasingSystem;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.Tests.Runtime.Chronos
{
    public class ChronosTests
    {
        private ChronosManager _chronos;

        [SetUp]
        public void SetUp()
        {
            _chronos = new ChronosManager();
            App.Register(_chronos);
            ((IGameService)_chronos).Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _chronos.ResumeGame(); // Reset time scale
            App.Shutdown();
        }

        [Test]
        public void GlobalScale_Updates_UnityTime()
        {
            _chronos.GlobalScale = 0.5f;
            Assert.AreEqual(0.5f, Time.timeScale);
            Assert.AreEqual(0.02f * 0.5f, Time.fixedDeltaTime, 0.0001f);
        }

        [Test]
        public void PauseResume_Works()
        {
            _chronos.PauseGame();
            Assert.AreEqual(0f, Time.timeScale);
            
            _chronos.ResumeGame();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ChannelScale_Transition_Smoothly()
        {
            string channel = "SlowMotion";
            _chronos.RegisterChannel(channel);
            
            // Transition from 1 to 0.5 in 0.2 seconds
            _chronos.SetTimeScale(channel, 0.5f, 0.2f, EasingType.Linear);
            
            // Start State
            Assert.AreEqual(1.0f, _chronos.GetChannelScale(channel));

            // Wait a bit
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Should be roughly halfway (0.75)
            float scaleMid = _chronos.GetChannelScale(channel);
            Assert.IsTrue(scaleMid < 1.0f && scaleMid > 0.5f);

            // Wait until finished
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(0.5f, _chronos.GetChannelScale(channel), 0.01f);
        }

        [Test]
        public void UIChannel_Remains_Unscaled_During_Pause()
        {
            _chronos.PauseGame();
            
            // World channel should have zero delta time
            Assert.AreEqual(0f, _chronos.GetDeltaTime(ChronosManager.DefaultChannel));
            
            // UI channel should have unscaled delta time
            Assert.AreEqual(Time.unscaledDeltaTime, _chronos.GetDeltaTime(ChronosManager.UIChannel));
        }

        [UnityTest]
        public IEnumerator ChronosIdentity_Returns_Correct_DeltaTime()
        {
            GameObject go = new GameObject("TestIdentity");
            var identity = go.AddComponent<ChronosIdentity>();
            identity.Channel = "Custom";
            _chronos.RegisterChannel("Custom");
            _chronos.SetTimeScale("Custom", 2.0f);

            yield return null; // Wait for component Start()
            
            // Identity should return 2x normal delta time
            Assert.AreEqual(Time.deltaTime * 2.0f, identity.DeltaTime, 0.001f);
            
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Timer_Respects_Channel_Scale()
        {
            var timerService = new Timer();
            App.Register(timerService);
            ((IGameService)timerService).Initialize();

            string channel = "SlowTimer";
            _chronos.RegisterChannel(channel);
            _chronos.SetTimeScale(channel, 0.5f);

            bool finished = false;
            // 0.1s duration on a 0.5x channel should take 0.2s
            timerService.CreateDelay(0.1f, () => finished = true).SetChannel(channel);

            yield return new WaitForSecondsRealtime(0.12f);
            Assert.IsFalse(finished, "Timer should not be finished yet due to 0.5x scale");

            yield return new WaitForSecondsRealtime(0.12f);
            Assert.IsTrue(finished, "Timer should be finished now");
        }

        [Test]
        public void Timer_Channel_Persists()
        {
            var timerService = new Timer();
            App.Register(timerService);
            ((IGameService)timerService).Initialize();

            var handle = timerService.CreateDelay(10f, () => { }).SetChannel("CustomChannel");
            
            string json = TimerPersistence.SaveAll();
            timerService.Clear();
            TimerPersistence.Clear();

            var restoredHandles = TimerPersistence.LoadAll(json);
            Assert.AreEqual(1, restoredHandles.Count);
            
            var info = timerService.GetActiveTimers()[0];
            Assert.AreEqual("CustomChannel", info.Channel);
        }
    }
}
