using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eraflo.UnityImportPackage.Tests
{
    using Eraflo.UnityImportPackage.Timers;

    public class TimerTests
    {
        [TearDown]
        public void TearDown()
        {
            TimerManager.Clear();
        }

        #region CountdownTimer Tests

        [Test]
        public void CountdownTimer_StartsWithCorrectTime()
        {
            var timer = new CountdownTimer(5f);
            Assert.AreEqual(5f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Tick_DecrementsTime()
        {
            var timer = new CountdownTimer(5f);
            timer.Tick(1f);
            Assert.AreEqual(4f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_IsFinished_WhenZero()
        {
            var timer = new CountdownTimer(1f);
            Assert.IsFalse(timer.IsFinished);
            timer.Tick(1f);
            Assert.IsTrue(timer.IsFinished);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_DoesNotGoBelowZero()
        {
            var timer = new CountdownTimer(1f);
            timer.Tick(5f);
            Assert.AreEqual(0f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Reset_RestoresToInitialTime()
        {
            var timer = new CountdownTimer(5f);
            timer.Tick(3f);
            timer.Reset();
            Assert.AreEqual(5f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_ResetWithNewTime_ChangesInitialTime()
        {
            var timer = new CountdownTimer(5f);
            timer.Reset(10f);
            Assert.AreEqual(10f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void CountdownTimer_Progress_CalculatesCorrectly()
        {
            var timer = new CountdownTimer(10f);
            Assert.AreEqual(1f, timer.Progress);
            timer.Tick(5f);
            Assert.AreEqual(0.5f, timer.Progress);
            timer.Tick(5f);
            Assert.AreEqual(0f, timer.Progress);
            timer.Dispose();
        }

        #endregion

        #region StopwatchTimer Tests

        [Test]
        public void StopwatchTimer_StartsAtZero()
        {
            var timer = new StopwatchTimer();
            Assert.AreEqual(0f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void StopwatchTimer_Tick_IncrementsTime()
        {
            var timer = new StopwatchTimer();
            timer.Tick(1f);
            Assert.AreEqual(1f, timer.CurrentTime);
            timer.Tick(2.5f);
            Assert.AreEqual(3.5f, timer.CurrentTime);
            timer.Dispose();
        }

        [Test]
        public void StopwatchTimer_IsNeverFinished()
        {
            var timer = new StopwatchTimer();
            timer.Tick(100f);
            Assert.IsFalse(timer.IsFinished);
            timer.Dispose();
        }

        [Test]
        public void StopwatchTimer_ElapsedTime_ReturnsCurrentTime()
        {
            var timer = new StopwatchTimer();
            timer.Tick(5f);
            Assert.AreEqual(5f, timer.ElapsedTime);
            timer.Dispose();
        }

        [Test]
        public void StopwatchTimer_Reset_SetsToZero()
        {
            var timer = new StopwatchTimer();
            timer.Tick(10f);
            timer.Reset();
            Assert.AreEqual(0f, timer.CurrentTime);
            timer.Dispose();
        }

        #endregion

        #region FrequencyTimer Tests

        [Test]
        public void FrequencyTimer_TicksAtCorrectFrequency()
        {
            int tickCount = 0;
            var timer = new FrequencyTimer(10); // 10 ticks per second
            timer.OnTick += () => tickCount++;
            
            // Simulate 1 second
            timer.Tick(1f);
            
            Assert.AreEqual(10, tickCount);
            timer.Dispose();
        }

        [Test]
        public void FrequencyTimer_AccumulatesPartialTicks()
        {
            int tickCount = 0;
            var timer = new FrequencyTimer(2); // 2 ticks per second (every 0.5s)
            timer.OnTick += () => tickCount++;
            
            timer.Tick(0.3f); // Not enough for a tick
            Assert.AreEqual(0, tickCount);
            
            timer.Tick(0.3f); // Now we have 0.6s, should tick once
            Assert.AreEqual(1, tickCount);
            
            timer.Dispose();
        }

        [Test]
        public void FrequencyTimer_TickCount_TracksCorrectly()
        {
            var timer = new FrequencyTimer(5);
            timer.Tick(1f);
            Assert.AreEqual(5, timer.TickCount);
            timer.Tick(1f);
            Assert.AreEqual(10, timer.TickCount);
            timer.Dispose();
        }

        [Test]
        public void FrequencyTimer_Reset_ClearsTickCount()
        {
            var timer = new FrequencyTimer(5);
            timer.Tick(1f);
            timer.Reset();
            Assert.AreEqual(0, timer.TickCount);
            timer.Dispose();
        }

        [Test]
        public void FrequencyTimer_SetTicksPerSecond_ChangesFrequency()
        {
            int tickCount = 0;
            var timer = new FrequencyTimer(1);
            timer.OnTick += () => tickCount++;
            
            timer.SetTicksPerSecond(10);
            timer.Tick(1f);
            
            Assert.AreEqual(10, tickCount);
            timer.Dispose();
        }

        #endregion

        #region Timer Lifecycle Tests

        [Test]
        public void Timer_Start_SetsIsRunningTrue()
        {
            var timer = new CountdownTimer(5f);
            Assert.IsFalse(timer.IsRunning);
            timer.Start();
            Assert.IsTrue(timer.IsRunning);
            timer.Dispose();
        }

        [Test]
        public void Timer_Stop_SetsIsRunningFalse()
        {
            var timer = new CountdownTimer(5f);
            timer.Start();
            timer.Stop();
            Assert.IsFalse(timer.IsRunning);
            timer.Dispose();
        }

        [Test]
        public void Timer_Pause_SetsIsRunningFalse()
        {
            var timer = new CountdownTimer(5f);
            timer.Start();
            timer.Pause();
            Assert.IsFalse(timer.IsRunning);
            timer.Dispose();
        }

        [Test]
        public void Timer_Resume_StartsAgain()
        {
            var timer = new CountdownTimer(5f);
            timer.Start();
            timer.Pause();
            timer.Resume();
            Assert.IsTrue(timer.IsRunning);
            timer.Dispose();
        }

        [Test]
        public void Timer_OnTimerStart_FiresOnStart()
        {
            bool eventFired = false;
            var timer = new CountdownTimer(5f);
            timer.OnTimerStart += () => eventFired = true;
            
            timer.Start();
            
            Assert.IsTrue(eventFired);
            timer.Dispose();
        }

        [Test]
        public void Timer_OnTimerStop_FiresOnStop()
        {
            bool eventFired = false;
            var timer = new CountdownTimer(5f);
            timer.OnTimerStop += () => eventFired = true;
            
            timer.Start();
            timer.Stop();
            
            Assert.IsTrue(eventFired);
            timer.Dispose();
        }

        #endregion

        #region TimerManager Tests

        [Test]
        public void TimerManager_RegisterTimer_IncrementsCount()
        {
            int initialCount = TimerManager.TimerCount;
            var timer = new CountdownTimer(5f);
            Assert.AreEqual(initialCount + 1, TimerManager.TimerCount);
            timer.Dispose();
        }

        [Test]
        public void TimerManager_UnregisterTimer_DecrementsCount()
        {
            var timer = new CountdownTimer(5f);
            int countAfterRegister = TimerManager.TimerCount;
            timer.Dispose();
            Assert.AreEqual(countAfterRegister - 1, TimerManager.TimerCount);
        }

        [Test]
        public void TimerManager_Clear_RemovesAllTimers()
        {
            new CountdownTimer(5f);
            new StopwatchTimer();
            new FrequencyTimer(10);
            
            TimerManager.Clear();
            Assert.AreEqual(0, TimerManager.TimerCount);
        }

        #endregion
    }
}
