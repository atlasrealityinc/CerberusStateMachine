using System;

namespace Cerberus
{
    /// <summary>
    /// Triggers events on a state machine. Exposed as <see cref="IStateMachine{StateIdT}.StateController"/>.
    /// The event is offered to every currently active state, from the innermost active sub-state out to the
    /// top-level state, and finally to the machine-level events. Triggering an event does not allocate.
    /// </summary>
    public interface IStateController
    {
        /// <summary>
        /// Offers <paramref name="eventId"/> to every active state whose event id type is <typeparamref name="EventIdT"/>,
        /// innermost sub-state first, then to machine-level events of that type.
        /// </summary>
        /// <returns>True if any level handled the event.</returns>
        bool TriggerEvent<EventIdT>(EventIdT eventId);
    }

    /// <summary>
    /// A controller bound to a single state, obtained through the deprecated <c>IStateMachine.StateControllerProvider</c>.
    /// </summary>
    /// <remarks>
    /// Deprecated. Use <see cref="IStateMachine{StateIdT}.StateController"/> instead, see docs/DEPRECATED.md.
    /// </remarks>
    [Obsolete("Deprecated: per-state controllers are replaced by IStateMachine<StateIdT>.StateController, which triggers events on the active state hierarchy. See docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
    public interface IStateController<EventIdT>
    {
        bool TriggerEvent(EventIdT eventId);
    }

    /// <summary>
    /// A controller bound to a single state that has sub-states, obtained through the deprecated
    /// <c>IStateMachine.StateControllerProvider</c>. Also exposes that state's current sub-state.
    /// </summary>
    /// <remarks>
    /// Deprecated. Use <see cref="IStateMachine{StateIdT}.StateController"/> to trigger events. There is no
    /// replacement for <see cref="CurrentSubState"/> yet, see docs/DEPRECATED.md.
    /// </remarks>
    [Obsolete("Deprecated: per-state controllers are replaced by IStateMachine<StateIdT>.StateController. CurrentSubState has no direct replacement, see docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
    public interface IStateController<EventIdT, SubStateIdT> : IStateController<EventIdT>
        where SubStateIdT : Enum
    {
        SubStateIdT CurrentSubState { get; }
    }
}
