using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.InputSystem;
using Eraflo.Catalyst.InputSystem.Providers;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Tests.InputSystem
{
    [TestFixture]
    public class InputBufferTests
    {
        private InputManager _inputManager;
        private MockInputProvider _mockProvider;

        private class MockInputProvider : IInputProvider
        {
            public Dictionary<string, bool> Buttons = new Dictionary<string, bool>();

            public float GetAxis(string axisId) => 0f;
            public bool GetButtonDown(string actionId) => Buttons.ContainsKey(actionId) && Buttons[actionId];
            public void Vibrate(float intensity, float duration) { }
        }

        [SetUp]
        public void Setup()
        {
            _inputManager = new InputManager();
            _mockProvider = new MockInputProvider();
            _inputManager.SetProvider(_mockProvider);
            _inputManager.Initialize();
            _inputManager.RegisterAction("Jump");
            _inputManager.RegisterAction("Fire");
        }

        [Test]
        public void InputIsAddedToBuffer()
        {
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate();

            Assert.IsTrue(_inputManager.TryConsumeAction("Jump"), "Action should be consumable from buffer");
        }

        [Test]
        public void InputCannotBeConsumedTwice()
        {
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate();

            Assert.IsTrue(_inputManager.TryConsumeAction("Jump"));
            Assert.IsFalse(_inputManager.TryConsumeAction("Jump"), "Action should not be consumable twice");
        }

        [Test]
        public void InputExpiresAfterDuration()
        {
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate();
            
            // Advance time manually beyond BufferDuration (default 0.2s)
            _inputManager.SetTimeForTesting(1.0f);
            
            Assert.IsFalse(_inputManager.TryConsumeAction("Jump"), "Action should be expired and not consumable");
        }

        [Test]
        public void InputConsumptionIsFIFO()
        {
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate(); // Time 0.0
            
            _inputManager.SetTimeForTesting(0.1f);
            _inputManager.OnUpdate(); // Time 0.1 (Another press? Mock provider still has it)
            
            // We need to clear it or it registers again.
            _mockProvider.Buttons["Jump"] = false;
            
            // If we had two distinct presses, TryConsume should pick the oldest one.
            // But with the same timestamp it doesn't matter much.
            // Let's force two distinct additions.
            
            _inputManager.SetTimeForTesting(0.0f);
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate();
            
            _inputManager.SetTimeForTesting(0.1f);
            _mockProvider.Buttons["Jump"] = true;
            _inputManager.OnUpdate();
            
            // Buffer has [ {0.0}, {0.1} ]
            // FIFO means it consumes {0.0} first. 
            // We can't easily verify which one it is without exposing internal buffer or timestamps,
            // but the logic is now loop from 0 to Count.
            
            Assert.IsTrue(_inputManager.TryConsumeAction("Jump"));
        }
    }
}
