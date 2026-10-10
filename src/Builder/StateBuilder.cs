using Cerberus.Builder.Data;
using System;

namespace Cerberus.Builder
{
    public class StateBuilder<StateT, StateIdT, EventIdT, EndReturnT>
        where StateT : IState
        where StateIdT : Enum
    {
        protected readonly EndReturnT _endReturnObject;
        private readonly StateData<StateT, StateIdT, EventIdT> _stateData;

        internal StateBuilder(EndReturnT endReturnObject, StateData<StateT, StateIdT, EventIdT> stateData)
        {
            _endReturnObject = endReturnObject;
            _stateData = stateData;
        }

        public StateBuilder<StateT, StateIdT, EventIdT, EndReturnT> AddEvent(EventIdT eventId, Action<IStateEvent<StateT, StateIdT>> action)
        {
            _stateData.AddEvent(eventId, action);
            return this;
        }

        /// <summary>
        /// Registers a handler that runs when a triggered event id satisfies <paramref name="predicate"/>.
        /// A key-based event for the same value wins over predicates, otherwise the first registered
        /// predicate that returns true wins. The handler can read the matched value from <c>e.EventId</c>.
        /// </summary>
        public StateBuilder<StateT, StateIdT, EventIdT, EndReturnT> AddEvent(Func<EventIdT, bool> predicate, Action<IStateEvent<StateT, StateIdT, EventIdT>> action)
        {
            _stateData.AddEvent(predicate, action);
            return this;
        }

        public EndReturnT End()
        {
            return _endReturnObject;
        }
    }
}
