using Cerberus.StateController;
using System;

namespace Cerberus
{
    public interface IStateMachine<StateIdT>
            where StateIdT : Enum
    {
        /// <summary>
        /// Looks up a controller bound to one specific state.
        /// </summary>
        /// <remarks>
        /// Deprecated. Use <see cref="StateController"/> instead, which needs no lookup, see docs/DEPRECATED.md.
        /// </remarks>
        [Obsolete("Deprecated: use IStateMachine<StateIdT>.StateController.TriggerEvent(eventId) instead, it triggers the event on whichever states are active. See docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
        IStateControllerProvider StateControllerProvider { get; }

        /// <summary>
        /// Triggers events on whichever states are currently active, innermost sub-state first, then machine-level events.
        /// </summary>
        IStateController StateController { get; }

        void Start();
    }
}
