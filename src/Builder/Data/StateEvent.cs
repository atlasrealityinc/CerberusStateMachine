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
}
