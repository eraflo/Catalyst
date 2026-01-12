using NUnit.Framework;
using Eraflo.Catalyst;
using Eraflo.Catalyst.InputSystem;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.Tests.InputSystem
{
    [TestFixture]
    public class InputSmokeTests
    {
        [Test]
        public void InputManager_AutoDiscovery_Succeeds()
        {
            // Reset locator for clean test
            ServiceLocator.Shutdown();
            
            // Ensure dependencies are registered
            if (App.Get<ChronosManager>() == null) App.Register(new ChronosManager());
            if (App.Get<Timer>() == null) App.Register(new Timer());

            // Discovery
            ServiceLocator.Initialize();
            
            var inputManager = App.Get<InputManager>();
            Assert.IsNotNull(inputManager, "InputManager should be auto-discovered by ServiceLocator");
        }
    }
}
