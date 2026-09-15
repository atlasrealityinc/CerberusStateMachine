namespace Cerberus.Runner
{
    /// <summary>
    /// A node in the hierarchy of currently running states. Allows walking from the active top-level
    /// state down to the innermost active sub-state without knowing the state id types involved.
    /// </summary>
    internal interface IStateRunner
    {
        /// <summary>
        /// The currently active sub-state runner, or null when this runner has no sub-states or is not running.
        /// </summary>
        IStateRunner ActiveSubStateRunner { get; }
    }
}
