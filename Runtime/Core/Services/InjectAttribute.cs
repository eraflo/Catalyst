using System;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Marks a field to be automatically populated by <see cref="ServiceInjector"/>
    /// with the matching service from the <see cref="ServiceLocator"/>.
    /// Works on any class — no base class required.
    /// <para>
    /// Injection timing:
    /// <list type="bullet">
    ///   <item>
    ///     <b>MonoBehaviour in a scene</b> — injected automatically before <c>Start</c>
    ///     (via <c>SceneManager.sceneLoaded</c>), no setup required.
    ///   </item>
    ///   <item>
    ///     <b>Any other object</b> (plain C# class, runtime-instantiated component, etc.)
    ///     — call <c>ServiceInjector.Inject(this)</c> once after construction.
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // MonoBehaviour — fully automatic, no inheritance needed
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [Inject] private Timer _timer;
    ///     [Inject] private INetworkService _network;
    ///
    ///     private void Start()
    ///     {
    ///         // _timer and _network are already injected here
    ///     }
    /// }
    ///
    /// // Plain C# class — call Inject manually once
    /// public class PlayerModel
    /// {
    ///     [Inject] private EventBus _eventBus;
    ///
    ///     public PlayerModel() => ServiceInjector.Inject(this);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class InjectAttribute : Attribute { }
}
