using System;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Marks a plain C# class as relying on service injection via <see cref="ServiceInjector"/>.
    /// Instances must be created through <see cref="App.Create{T}"/> (or
    /// <see cref="ServiceInjector.Create{T}"/>) so that all <see cref="InjectAttribute"/>
    /// fields are populated automatically — no constructor call required.
    /// </summary>
    /// <example>
    /// <code>
    /// [Injectable]
    /// public class PlayerModel
    /// {
    ///     [Inject] private EventBus _eventBus;
    ///     [Inject] private SaveManager _save;
    ///
    ///     // No ServiceInjector.Inject(this) needed
    /// }
    ///
    /// // Create via factory — fields are injected before the instance is returned
    /// var model = App.Create&lt;PlayerModel&gt;();
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class InjectableAttribute : Attribute { }
}
