using Cerberus.Builder.Data;
using Cerberus.IoC;
using System;
using System.Collections.Generic;

namespace Cerberus.Builder
{
    public class StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT> : StateBuilder<StateT, StateIdT, EventIdT, EndReturnT>
            where StateT : State
            where StateIdT : Enum
            where EventIdT : Enum
            where SubStateIdT : Enum
    {
        protected readonly Dictionary<Type, List<Type>> _stateHandlerTypes = new Dictionary<Type, List<Type>>();
        protected readonly IStateMachineContainer _stateMachineContainer;
        private readonly StateData<StateT, StateIdT, EventIdT, SubStateIdT> _stateData;

        internal StateBuilderWithSubStates(EndReturnT endReturnObject, StateData<StateT, StateIdT, EventIdT, SubStateIdT> stateData, IStateMachineContainer stateMachineContainer, Dictionary<Type, List<Type>> stateHandlerTypes) : base(endReturnObject, stateData)
        {
            _stateData = stateData;
            _stateMachineContainer = stateMachineContainer;
            _stateHandlerTypes = stateHandlerTypes;
        }

        public new StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT> AddEvent(EventIdT eventId, Action<StateEvent<StateT, StateIdT>> action)
        {
            base.AddEvent(eventId, action);
            return this;
        }

        public StateBuilder<SubStateT, SubStateIdT, SubEventIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>> State<SubStateT, SubEventIdT>(SubStateIdT subStateId)
            where SubStateT : State
            where SubEventIdT : Enum
        {
            var stateData = new StateData<SubStateT, SubStateIdT, SubEventIdT>(subStateId, _stateMachineContainer, _stateHandlerTypes);
            _stateData.AddSubState(subStateId, stateData);
            return new StateBuilder<SubStateT, SubStateIdT, SubEventIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>>(this, stateData);
        }

        public StateBuilderWithSubStates<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>> State<SubStateT, SubEventIdT, SubSubStateIdT>(SubStateIdT subStateId)
            where SubStateT : State
            where SubEventIdT : Enum
            where SubSubStateIdT : Enum
        {
            var stateData = new StateData<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT>(subStateId, _stateMachineContainer, _stateHandlerTypes);
            _stateData.AddSubState(subStateId, stateData);
            return new StateBuilderWithSubStates<SubStateT, SubStateIdT, SubEventIdT, SubSubStateIdT, StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, EndReturnT>>(this, stateData, _stateMachineContainer, _stateHandlerTypes);
        }
    }
}
