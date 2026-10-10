using Cerberus.Runner;
using System;

namespace Cerberus.Builder.Data
{
    public class StateMachineEvent<StateIdT>
            where StateIdT : Enum
    {
        private readonly IStateChanger<StateIdT> _stateChanger;

        internal StateMachineEvent(IStateChanger<StateIdT> stateChanger)
        {
            _stateChanger = stateChanger;
        }

        public void ChangeState(StateIdT stateId)
        {
            _stateChanger.ChangeState(stateId);
        }
    }

    /// <summary>
    /// The context passed to machine-level handlers registered with a predicate. Adds the event id value that
    /// satisfied the predicate, which a key-based handler never needs because its key is fixed at registration.
    /// </summary>
    public class StateMachineEvent<StateIdT, EventIdT> : StateMachineEvent<StateIdT>
            where StateIdT : Enum
    {
        public EventIdT EventId { get; }

        internal StateMachineEvent(IStateChanger<StateIdT> stateChanger, EventIdT eventId) : base(stateChanger)
        {
            EventId = eventId;
        }
    }
}
