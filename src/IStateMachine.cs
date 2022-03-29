using Cerberus.StateController;
using System;

namespace Cerberus
{
    public interface IStateMachine<StateIdT>
            where StateIdT : Enum
    {
        IStateControllerProvider StateControllerProvider { get; }

        void Start();
    }
}
