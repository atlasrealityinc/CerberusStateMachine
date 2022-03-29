using System;

namespace Cerberus.Runner
{
    internal interface IStateChanger<StateIdT>
        where StateIdT : Enum
    {
        void ChangeState(StateIdT stateId);
    }
}
