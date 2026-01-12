namespace Eraflo.Catalyst.InputSystem
{
    /// <summary>
    /// Abstraction for input data retrieval.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Returns true if the action was pressed this frame.
        /// </summary>
        bool GetButtonDown(string actionId);

        /// <summary>
        /// Returns the raw axis value.
        /// </summary>
        float GetAxis(string axisId);

        /// <summary>
        /// Triggers haptic feedback.
        /// </summary>
        void Vibrate(float intensity, float duration);
    }
}
