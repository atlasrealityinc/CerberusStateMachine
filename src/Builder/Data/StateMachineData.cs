using Cerberus.Runner;
using System;
using System.Collections.Generic;

namespace Cerberus.Builder.Data
{
    internal class StateMachineData<StateIdT>
            where StateIdT : Enum
    {
        internal Dictionary<Enum, Action<StateMachineEvent<StateIdT>>> StateMachineEvents { get; } = new Dictionary<Enum, Action<StateMachineEvent<StateIdT>>>();

        public virtual StateMachineRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateMachineRunner<StateIdT>();
        }
    }

    internal class StateMachineData<StateIdT, EventIdT> : StateMachineData<StateIdT>
        where StateIdT : Enum
        where EventIdT : Enum
    {
        public override StateMachineRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateMachineRunner<StateIdT, EventIdT>(this, stateChanger);
        }
    }
}
