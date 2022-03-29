using Cerberus.Runner;
using System;

namespace Cerberus.Builder.Data
{
    public class StateEvent<StateT, StateIdT>
        where StateT : State
        where StateIdT : Enum
    {
        private readonly StateRunner<StateT, StateIdT> _stateRunner;

        public StateT StateInstance { get { return _stateRunner.ActiveInstance; } }

        internal StateEvent(StateRunner<StateT, StateIdT> stateRunner)
        {
            _stateRunner = stateRunner;
        }

        public void ChangeState(StateIdT stateId)
        {
            _stateRunner.StateChangerChangeState(stateId);
        }
    }
}
