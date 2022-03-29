using Cerberus.Builder.Data;
using System;

namespace Cerberus.Builder
{
    public class StateBuilder<StateT, StateIdT, EventIdT, EndReturnT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
    {
        protected readonly EndReturnT _endReturnObject;
        private readonly StateData<StateT, StateIdT, EventIdT> _stateData;

        internal StateBuilder(EndReturnT endReturnObject, StateData<StateT, StateIdT, EventIdT> stateData)
        {
            _endReturnObject = endReturnObject;
            _stateData = stateData;
        }

        public StateBuilder<StateT, StateIdT, EventIdT, EndReturnT> AddEvent(EventIdT eventId, Action<StateEvent<StateT, StateIdT>> action)
        {
            _stateData.AddEvent(eventId, action);
            return this;
        }

        public EndReturnT End()
        {
            return _endReturnObject;
        }
    }
}
