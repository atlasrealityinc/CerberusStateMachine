using Cerberus.Runner;
using System;

namespace Cerberus.Builder.Data
{
    public interface IStateEvent<StateT, StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        StateIdT PreviousStateId { get; }
        StateT StateInstance { get; }

        void ChangeState(StateIdT stateId);
    }

    /// <summary>
    /// The context passed to state-level handlers registered with a predicate. Adds the event id value that
    /// satisfied the predicate, which a key-based handler never needs because its key is fixed at registration.
    /// </summary>
    public interface IStateEvent<StateT, StateIdT, EventIdT> : IStateEvent<StateT, StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        EventIdT EventId { get; }
    }

    internal class StateEvent<StateT, StateIdT> : IStateEvent<StateT, StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        private readonly StateRunner<StateT, StateIdT> _stateRunner;

        public StateIdT PreviousStateId { get; }

        public StateT StateInstance { get { return (StateT)_stateRunner.ActiveInstance; } }

        internal StateEvent(StateRunner<StateT, StateIdT> stateRunner, StateIdT previousStateId)
        {
            _stateRunner = stateRunner;
            PreviousStateId = previousStateId;
        }

        public void ChangeState(StateIdT stateId)
        {
            _stateRunner.StateChangerChangeState(stateId);
        }
    }

    internal class StateEvent<StateT, StateIdT, EventIdT> : StateEvent<StateT, StateIdT>, IStateEvent<StateT, StateIdT, EventIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        public EventIdT EventId { get; }

        internal StateEvent(StateRunner<StateT, StateIdT> stateRunner, StateIdT previousStateId, EventIdT eventId) : base(stateRunner, previousStateId)
        {
            EventId = eventId;
        }
    }
}
