using Cerberus.Builder.Data;
using Cerberus.IoC;
using System;

namespace Cerberus.Builder
{
    public class StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT> : StateBuilder<StateT, StateIdT, EventIdT, EndReturnT>
            where StateT : IState
            where StateIdT : Enum
            where SubStateIdT : Enum
    {
        private protected readonly StateHandlerRegistrations _stateHandlerRegistrations;
        protected readonly IStateMachineContainer _stateMachineContainer;
        private readonly StateData<StateT, StateIdT, EventIdT, SubStateIdT> _stateData;

        internal StateBuilderWithSubStates(EndReturnT endReturnObject, StateData<StateT, StateIdT, EventIdT, SubStateIdT> stateData, IStateMachineContainer stateMachineContainer, StateHandlerRegistrations stateHandlerRegistrations) : base(endReturnObject, stateData)
        {
            _stateData = stateData;
            _stateMachineContainer = stateMachineContainer;
            _stateHandlerRegistrations = stateHandlerRegistrations;
        }

        public new StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT> AddEvent(EventIdT eventId, Action<IStateEvent<StateT, StateIdT>> action)
        {
            base.AddEvent(eventId, action);
            return this;
        }

        public new StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT> AddEvent(Func<EventIdT, bool> predicate, Action<IStateEvent<StateT, StateIdT, EventIdT>> action)
        {
            base.AddEvent(predicate, action);
            return this;
        }

        public StateBuilder<SubStateT, SubStateIdT, SubEventIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>> State<SubStateT, SubEventIdT>(SubStateIdT subStateId)
            where SubStateT : IState
        {
            var stateData = new StateData<SubStateT, SubStateIdT, SubEventIdT>(subStateId, _stateMachineContainer, _stateHandlerRegistrations);
            _stateData.AddSubState(subStateId, stateData);
            return new StateBuilder<SubStateT, SubStateIdT, SubEventIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>>(this, stateData);
        }

        public StateBuilderWithSubStates<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>> State<SubStateT, SubEventIdT, SubSubStateIdT>(SubStateIdT subStateId)
            where SubStateT : IState
            where SubSubStateIdT : Enum
        {
            var stateData = new StateData<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT>(subStateId, _stateMachineContainer, _stateHandlerRegistrations);
            _stateData.AddSubState(subStateId, stateData);
            return new StateBuilderWithSubStates<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>>(this, stateData, _stateMachineContainer, _stateHandlerRegistrations);
        }
    }
}
