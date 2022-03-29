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
}
