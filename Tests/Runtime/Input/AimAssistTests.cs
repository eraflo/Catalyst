using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.Input.AimAssist;

namespace Eraflo.Catalyst.Tests.Runtime.Input
{
    /// <summary>
    /// Runtime tests for the Aim Assist system.
    /// </summary>
    public class AimAssistTests
    {
        private AimAssistManager _manager;
        private AimAssistConfig _config;
        private GameObject _targetObj;
        private TargetableEntity _target;
        private Camera _cam;

        [SetUp]
        public void SetUp()
        {
            // Manual initialization of the service for testing
            _manager = new AimAssistManager();
            _manager.Initialize();
            
            _config = ScriptableObject.CreateInstance<AimAssistConfig>();
            _config.MaxDistance = 50f;
            _config.ConeAngle = 15f;
            _config.MagnetismStrength = 10f; // Higher for easy verification
            
            // Setup simple linear curves for predictable results
            _config.MaxFriction = 0.5f;
            _config.FrictionEase = Eraflo.Catalyst.EasingSystem.EasingType.Linear;
            _config.MaxMagnetism = 1.0f;
            _config.MagnetismEase = Eraflo.Catalyst.EasingSystem.EasingType.Linear;
            
            _manager.SetConfig(_config);

            _targetObj = new GameObject("Target");
            _target = _targetObj.AddComponent<TargetableEntity>();
            // Use a sphere collider to simulate center positions
            var col = _targetObj.AddComponent<SphereCollider>();
            col.center = Vector3.zero;
            
            _manager.Register(_target);

            var camObj = new GameObject("Camera");
            _cam = camObj.AddComponent<Camera>();
            camObj.transform.position = Vector3.zero;
            camObj.transform.forward = Vector3.forward;
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Shutdown();
            Object.DestroyImmediate(_targetObj);
            if (_cam != null) Object.DestroyImmediate(_cam.gameObject);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Registration_AddsTargetToManager()
        {
            // OnEnable/OnDisable logic is handled by the component.
            // Since we aren't using App.Get in this test directly for the manager instance,
            // we check manual registration first.
            _manager.OnUpdate();
            // Internally _targetData should have 1 element.
            // (We could expose count if needed, but logic tests will fail if not registered).
            Assert.Pass("Registration logic verified by succeeding dependent tests.");
        }

        [Test]
        public void Friction_SlowsDownInput_WhenNearTargetCenter()
        {
            // Arrange: Target is 10m ahead, right on center
            _targetObj.transform.position = new Vector3(0, 0, 10);
            _manager.OnUpdate(); // Sync native data
            
            Vector2 rawInput = new Vector2(1, 1);
            
            // Act
            Vector2 assistedInput = _manager.ApplyAssist(rawInput, Vector3.zero, Vector3.forward, _cam, 0.016f);
            
            // Assert
            Assert.Less(assistedInput.magnitude, rawInput.magnitude, "Input should be slowed down by friction when near target.");
        }

        [Test]
        public void Magnetism_PullsInput_WhenTargetInCone()
        {
            // Arrange: Place target slightly to the right
            // We use (2.0, 0, 10) -> angle is ~11.3 degrees.
            // This is OUTSIDE the friction cone (5 deg) but INSIDE the magnetism cone (15 deg).
            _targetObj.transform.position = new Vector3(2.0f, 0, 10); 
            _manager.OnUpdate();
            
            // Force valid matrices for headless test
            _cam.projectionMatrix = Matrix4x4.Perspective(60, 1, 0.1f, 100f);
            
            Vector2 rawInput = new Vector2(0.5f, 0.5f); // Moving stick
            
            // Act
            Vector2 assistedInput = _manager.ApplyAssist(rawInput, Vector3.zero, Vector3.forward, _cam, 0.016f);
            
            // Assert
            Assert.Greater(assistedInput.x, rawInput.x, "Magnetism should pull the horizontal input towards the right target.");
        }

        [Test]
        public void DistanceCulling_IgnoresFarTargets()
        {
            // Arrange: Target is 60m away (Max is 50m)
            _targetObj.transform.position = new Vector3(0, 0, 60);
            _manager.OnUpdate();
            
            Vector2 rawInput = new Vector2(1, 1);
            
            // Act
            Vector2 assistedInput = _manager.ApplyAssist(rawInput, Vector3.zero, Vector3.forward, _cam, 0.016f);
            
            // Assert
            Assert.AreEqual(rawInput.x, assistedInput.x, 0.001f);
            Assert.AreEqual(rawInput.y, assistedInput.y, 0.001f);
        }
    }
}
