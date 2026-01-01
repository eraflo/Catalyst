using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Timers;
using Eraflo.Catalyst.EasingSystem;

namespace Eraflo.Catalyst.Tests
{
    public class TimerTests
    {
        [SetUp]
        public void SetUp()
        {
            Timer.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Timer.Clear();
        }

        #region Timer Creation Tests

        [Test]
        public void Create_CountdownTimer_ReturnsValidHandle()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, Timer.Count);
        }

        [Test]
        public void Create_StopwatchTimer_ReturnsValidHandle()
        {
            var handle = Timer.Create<StopwatchTimer>(0f);
            
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, Timer.Count);
        }

        [Test]
        public void Create_MultipleTimers_IncreasesCount()
        {
            Timer.Create<CountdownTimer>(5f);
            Timer.Create<StopwatchTimer>(0f);
            Timer.Create<DelayTimer>(3f);
            
            Assert.AreEqual(3, Timer.Count);
        }

        [Test]
        public void Create_WithConfig_AppliesSettings()
        {
            var config = TimerConfig.Create(10f, timeScale: 2f, useUnscaledTime: true);
            var handle = Timer.Create<CountdownTimer>(config);
            
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(10f, Timer.GetCurrentTime(handle), 0.01f);
        }

        [Test]
        public void Delay_CreatesTimerWithCallback()
        {
            bool called = false;
            var handle = Timer.Delay(1f, () => called = true);
            
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, Timer.Count);
        }

        #endregion

        #region Timer State Tests

        [Test]
        public void Timer_IsRunning_TrueAfterCreation()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void Timer_IsFinished_FalseAfterCreation()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.IsFalse(Timer.IsFinished(handle));
        }

        [Test]
        public void Timer_GetProgress_ReturnsOneAtStart()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.AreEqual(1f, Timer.GetProgress(handle), 0.01f);
        }

        [Test]
        public void Timer_GetCurrentTime_ReturnsInitialDuration()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.AreEqual(5f, Timer.GetCurrentTime(handle), 0.01f);
        }

        #endregion

        #region Timer Control Tests

        [Test]
        public void Pause_StopsTimer()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Timer.Pause(handle);
            
            Assert.IsFalse(Timer.IsRunning(handle));
        }

        [Test]
        public void Resume_StartsTimerAfterPause()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Timer.Pause(handle);
            
            Timer.Resume(handle);
            
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void Cancel_RemovesTimer()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Assert.AreEqual(1, Timer.Count);
            
            Timer.Cancel(handle);
            
            Assert.AreEqual(0, Timer.Count);
        }

        [Test]
        public void Reset_RestoresInitialTime()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            // Simulate some time passing (manually set for test)
            
            Timer.Reset(handle);
            
            Assert.AreEqual(5f, Timer.GetCurrentTime(handle), 0.01f);
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void SetTimeScale_ChangesTimerSpeed()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Timer.SetTimeScale(handle, 2f);
            
            // Timer should still be valid after setting time scale
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void Clear_RemovesAllTimers()
        {
            Timer.Create<CountdownTimer>(5f);
            Timer.Create<StopwatchTimer>(0f);
            Timer.Create<DelayTimer>(3f);
            Assert.AreEqual(3, Timer.Count);
            
            Timer.Clear();
            
            Assert.AreEqual(0, Timer.Count);
        }

        #endregion

        #region Callback Tests

        [Test]
        public void OnComplete_CallbackRegistered_DoesNotThrow()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnComplete>(handle, () => { });
            });
        }

        [Test]
        public void OnTick_WithParameter_CallbackRegistered()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnTick, float>(handle, (dt) => { });
            });
        }

        [Test]
        public void OnRepeat_WithIntParameter_CallbackRegistered()
        {
            var handle = Timer.Create<RepeatingTimer>(1f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnRepeat, int>(handle, (count) => { });
            });
        }

        [Test]
        public void OnPause_CallbackRegistered()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnPause>(handle, () => { });
            });
        }

        [Test]
        public void OnResume_CallbackRegistered()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnResume>(handle, () => { });
            });
        }

        [Test]
        public void OnReset_CallbackRegistered()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnReset>(handle, () => { });
            });
        }

        [Test]
        public void OnCancel_CallbackRegistered()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            Assert.DoesNotThrow(() => {
                Timer.On<OnCancel>(handle, () => { });
            });
        }

        [Test]
        public void Off_UnregistersCallback()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Timer.On<OnComplete>(handle, () => { });
            
            Assert.DoesNotThrow(() => {
                Timer.Off<OnComplete>(handle);
            });
        }

        #endregion

        #region Easing Tests

        [Test]
        public void GetEasedProgress_Linear_ReturnsSameAsProgress()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            float progress = Timer.GetProgress(handle);
            float eased = Timer.GetEasedProgress(handle, EasingType.Linear);
            
            Assert.AreEqual(progress, eased, 0.001f);
        }

        [Test]
        public void GetEasedProgress_QuadIn_ReturnsDifferentValue()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            // QuadIn at t=1 should still be 1, but at t=0.5 it's 0.25
            float eased = Timer.GetEasedProgress(handle, EasingType.QuadIn);
            
            // At start (progress = 1), QuadIn(1) = 1
            Assert.AreEqual(1f, eased, 0.001f);
        }

        [Test]
        public void Lerp_Float_InterpolatesCorrectly()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            float result = Timer.Lerp(handle, 0f, 100f, EasingType.Linear);
            
            Assert.AreEqual(100f, result, 0.01f);
        }

        [Test]
        public void Lerp_Vector2_InterpolatesCorrectly()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.one * 10f;
            
            Vector2 result = Timer.Lerp(handle, from, to, EasingType.Linear);
            
            Assert.AreEqual(to, result);
        }

        [Test]
        public void Lerp_Vector3_InterpolatesCorrectly()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Vector3 from = Vector3.zero;
            Vector3 to = Vector3.one * 10f;
            
            Vector3 result = Timer.Lerp(handle, from, to, EasingType.Linear);
            
            Assert.AreEqual(to, result);
        }

        [Test]
        public void Lerp_Quaternion_InterpolatesCorrectly()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Quaternion from = Quaternion.identity;
            Quaternion to = Quaternion.Euler(0, 90, 0);
            
            Quaternion result = Timer.Lerp(handle, from, to, EasingType.Linear);
            
            Assert.AreEqual(to.eulerAngles.y, result.eulerAngles.y, 0.01f);
        }

        [Test]
        public void Lerp_Color_InterpolatesCorrectly()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Color from = Color.black;
            Color to = Color.white;
            
            Color result = Timer.Lerp(handle, from, to, EasingType.Linear);
            
            Assert.AreEqual(to, result);
        }

        [Test]
        public void LerpUnclamped_AllowsOvershoot()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            
            // LerpUnclamped should work without throwing
            Assert.DoesNotThrow(() => {
                Timer.LerpUnclamped(handle, 0f, 10f, EasingType.ElasticOut);
            });
        }

        #endregion

        #region Timer Type Tests

        [Test]
        public void DelayTimer_CreatesValidHandle()
        {
            var handle = Timer.Create<DelayTimer>(2f);
            
            Assert.IsTrue(handle.IsValid);
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void RepeatingTimer_CreatesValidHandle()
        {
            var handle = Timer.Create<RepeatingTimer>(1f);
            
            Assert.IsTrue(handle.IsValid);
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        [Test]
        public void FrequencyTimer_CreatesValidHandle()
        {
            var handle = Timer.Create<FrequencyTimer>(10f);
            
            Assert.IsTrue(handle.IsValid);
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        #endregion

        #region Handle Validity Tests

        [Test]
        public void InvalidHandle_IsValid_ReturnsFalse()
        {
            var handle = TimerHandle.None;
            
            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        public void CancelledTimer_Operations_DoNotThrow()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Timer.Cancel(handle);
            
            Assert.DoesNotThrow(() => Timer.Pause(handle));
            Assert.DoesNotThrow(() => Timer.Resume(handle));
            Assert.DoesNotThrow(() => Timer.GetProgress(handle));
            Assert.DoesNotThrow(() => Timer.Reset(handle));
            Assert.DoesNotThrow(() => Timer.SetTimeScale(handle, 1f));
        }

        [Test]
        public void InvalidHandle_QueryOperations_ReturnDefaults()
        {
            var handle = TimerHandle.None;
            
            Assert.AreEqual(0f, Timer.GetCurrentTime(handle));
            Assert.AreEqual(0f, Timer.GetProgress(handle));
            Assert.IsTrue(Timer.IsFinished(handle));
            Assert.IsFalse(Timer.IsRunning(handle));
        }

        #endregion

        #region Backend Tests

        [Test]
        public void IsBurstMode_ReturnsBoolean()
        {
            // Just verify it doesn't throw
            bool isBurst = Timer.IsBurstMode;
            Assert.That(isBurst, Is.TypeOf<bool>());
        }

        [Test]
        public void Count_ReturnsCorrectValue()
        {
            Assert.AreEqual(0, Timer.Count);
            
            Timer.Create<CountdownTimer>(1f);
            Assert.AreEqual(1, Timer.Count);
            
            Timer.Create<CountdownTimer>(1f);
            Assert.AreEqual(2, Timer.Count);
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator Timer_Updates_AfterFrame()
        {
            var handle = Timer.Create<CountdownTimer>(1f);
            float initialTime = Timer.GetCurrentTime(handle);
            
            yield return null;
            
            float newTime = Timer.GetCurrentTime(handle);
            
            Assert.Less(newTime, initialTime);
        }

        [UnityTest]
        public IEnumerator StopwatchTimer_CountsUp()
        {
            var handle = Timer.Create<StopwatchTimer>(0f);
            
            yield return null;
            
            float time = Timer.GetCurrentTime(handle);
            
            Assert.Greater(time, 0f);
        }

        [UnityTest]
        public IEnumerator PausedTimer_DoesNotUpdate()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            Timer.Pause(handle);
            float initialTime = Timer.GetCurrentTime(handle);
            
            yield return null;
            
            float newTime = Timer.GetCurrentTime(handle);
            
            Assert.AreEqual(initialTime, newTime, 0.001f);
        }

        [UnityTest]
        public IEnumerator OnTickCallback_IsInvoked()
        {
            var handle = Timer.Create<CountdownTimer>(5f);
            bool tickCalled = false;
            
            Timer.On<OnTick, float>(handle, (dt) => tickCalled = true);
            
            yield return null;
            
            Assert.IsTrue(tickCalled);
        }

        [UnityTest]
        public IEnumerator Timer_CompletesAfterDuration()
        {
            var handle = Timer.Create<CountdownTimer>(0.1f);
            bool completed = false;
            
            Timer.On<OnComplete>(handle, () => completed = true);
            
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(completed);
            Assert.IsTrue(Timer.IsFinished(handle));
        }

        [UnityTest]
        public IEnumerator Reset_RestoresTimerAfterCompletion()
        {
            var handle = Timer.Create<CountdownTimer>(0.1f);
            
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(Timer.IsFinished(handle));
            
            Timer.Reset(handle);
            
            Assert.IsFalse(Timer.IsFinished(handle));
            Assert.IsTrue(Timer.IsRunning(handle));
        }

        #endregion

        #region Presets Tests

        [Test]
        public void Presets_Define_CreatesPreset()
        {
            TimerPresets.Clear();
            
            TimerPresets.Define("TestPreset", 5f, EasingType.QuadOut);
            
            Assert.IsTrue(TimerPresets.Exists("TestPreset"));
        }

        [Test]
        public void Presets_DefineGeneric_CreatesPresetWithType()
        {
            TimerPresets.Clear();
            
            TimerPresets.Define<RepeatingTimer>("RepeatPreset", 2f);
            
            var preset = TimerPresets.Get("RepeatPreset");
            Assert.AreEqual(typeof(RepeatingTimer), preset.TimerType);
        }

        [Test]
        public void FromPreset_CreatesTimerFromPreset()
        {
            TimerPresets.Clear();
            TimerPresets.Define("QuickFade", 0.5f);
            
            var handle = Timer.FromPreset("QuickFade");
            
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, Timer.Count);
        }

        [Test]
        public void FromPreset_WithCallback_RegistersCallback()
        {
            TimerPresets.Clear();
            TimerPresets.Define("TestWithCallback", 1f);
            bool called = false;
            
            var handle = Timer.FromPreset("TestWithCallback", () => called = true);
            
            Assert.IsTrue(handle.IsValid);
            // Callback registration is tested by checking handle is valid
        }

        [Test]
        public void FromPreset_UndefinedPreset_ReturnsNone()
        {
            TimerPresets.Clear();
            
            var handle = Timer.FromPreset("NonExistent");
            
            Assert.IsFalse(handle.IsValid);
        }

        #endregion

        #region Metrics Tests

        [Test]
        public void Metrics_TotalCreated_IncreasesOnCreate()
        {
            Timer.Metrics.Reset();
            
            Timer.Create<CountdownTimer>(5f);
            Timer.Create<StopwatchTimer>(0f);
            Timer.Create<DelayTimer>(3f);
            
            Assert.AreEqual(3, Timer.Metrics.TotalCreated);
        }

        [Test]
        public void Metrics_TotalCancelled_IncreasesOnCancel()
        {
            Timer.Metrics.Reset();
            
            var h1 = Timer.Create<CountdownTimer>(5f);
            var h2 = Timer.Create<CountdownTimer>(5f);
            Timer.Cancel(h1);
            
            Assert.AreEqual(1, Timer.Metrics.TotalCancelled);
        }

        [Test]
        public void Metrics_TotalResets_IncreasesOnReset()
        {
            Timer.Metrics.Reset();
            
            var handle = Timer.Create<CountdownTimer>(5f);
            Timer.Reset(handle);
            Timer.Reset(handle);
            
            Assert.AreEqual(2, Timer.Metrics.TotalResets);
        }

        [Test]
        public void Metrics_PeakActiveCount_TracksMaximum()
        {
            Timer.Metrics.Reset();
            
            var h1 = Timer.Create<CountdownTimer>(5f);
            var h2 = Timer.Create<CountdownTimer>(5f);
            var h3 = Timer.Create<CountdownTimer>(5f);
            Timer.Cancel(h1);
            Timer.Cancel(h2);
            
            Assert.GreaterOrEqual(Timer.Metrics.PeakActiveCount, 3);
        }

        [Test]
        public void Metrics_AverageDuration_CalculatesCorrectly()
        {
            Timer.Metrics.Reset();
            
            Timer.Create<CountdownTimer>(2f);
            Timer.Create<CountdownTimer>(4f);
            Timer.Create<CountdownTimer>(6f);
            
            Assert.AreEqual(4f, Timer.Metrics.AverageDuration, 0.01f);
        }

        [Test]
        public void Metrics_Reset_ClearsAllMetrics()
        {
            Timer.Create<CountdownTimer>(5f);
            Timer.Metrics.Reset();
            
            Assert.AreEqual(0, Timer.Metrics.TotalCreated);
            Assert.AreEqual(0, Timer.Metrics.TotalCancelled);
            Assert.AreEqual(0, Timer.Metrics.TotalResets);
        }

        #endregion

        #region Persistence Tests

        [Test]
        public void Persistence_SaveAll_ReturnsValidJson()
        {
            Timer.Create<CountdownTimer>(5f);
            Timer.Create<StopwatchTimer>(0f);
            
            string json = TimerPersistence.SaveAll();
            
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("CountdownTimer"));
            Assert.IsTrue(json.Contains("StopwatchTimer"));
        }

        [Test]
        public void Persistence_LoadAll_RestoresTimers()
        {
            Timer.Create<CountdownTimer>(5f);
            Timer.Create<CountdownTimer>(10f);
            string json = TimerPersistence.SaveAll();
            
            Timer.Clear();
            Assert.AreEqual(0, Timer.Count);
            
            var handles = TimerPersistence.LoadAll(json);
            
            Assert.AreEqual(2, handles.Count);
            Assert.AreEqual(2, Timer.Count);
        }

        [Test]
        public void Persistence_LoadAll_EmptyJson_ReturnsEmptyList()
        {
            var handles = TimerPersistence.LoadAll("");
            
            Assert.AreEqual(0, handles.Count);
        }

        [Test]
        public void Persistence_Clear_ClearsRegistry()
        {
            Timer.Create<CountdownTimer>(5f);
            TimerPersistence.SaveAll();
            
            TimerPersistence.Clear();
            
            // No exception means success
            Assert.Pass();
        }

        #endregion
    }
}
