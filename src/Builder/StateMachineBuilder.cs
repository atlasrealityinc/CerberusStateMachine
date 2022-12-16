using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.StateHandlers;
using System;
using System.Collections.Generic;

namespace Cerberus.Builder
{
    public class StateMachineBuilder<StateIdT, EventIdT> : StateMachineBuilder<StateIdT>
            where StateIdT : Enum
            where EventIdT : Enum
    {
        public StateMachineBuilder() : this(new DefaultStateMachineContainer())
        {

        }

        public StateMachineBuilder(IStateMachineContainer stateMachineContainer) : base(stateMachineContainer, () => new StateMachineData<StateIdT, EventIdT>())
        {

        }

        public StateMachineBuilder<StateIdT, EventIdT> AddEvent(EventIdT eventId, Action<StateMachineEvent<StateIdT>> action)
        {
            _stateMachineData.StateMachineEvents.Add(eventId, action);
            return this;
        }
    }

    public class StateMachineBuilder<StateIdT> : StateMachineBuilderWithStates<StateIdT>
        where StateIdT : Enum
    {
        public StateMachineBuilder() : this(new DefaultStateMachineContainer())
        {

        }

        public StateMachineBuilder(IStateMachineContainer stateMachineContainer) : base(stateMachineContainer)
        {

        }

        private protected StateMachineBuilder(IStateMachineContainer stateMachineContainer, Func<StateMachineData<StateIdT>> stateMachineDataBuilder) : base(stateMachineContainer, stateMachineDataBuilder)
        {

        }

        public StateMachineBuilder<StateIdT> AddStateHandler<StateT, StateHandlerT>()
            where StateHandlerT : IStateHandler<StateT>
        {
            BaseAddStateHandler<StateT, StateHandlerT>();
            return this;
        }
    }

    public abstract class StateMachineBuilderWithStates<StateIdT>
        where StateIdT : Enum
    {
        protected readonly IStateMachineContainer _stateMachineContainer;
        private protected readonly Dictionary<StateIdT, StateData<StateIdT>> _states = new Dictionary<StateIdT, StateData<StateIdT>>();
        private protected readonly Dictionary<Type, List<Type>> _stateHandlerTypes = new Dictionary<Type, List<Type>>();
        private protected readonly StateMachineData<StateIdT> _stateMachineData = null;

        private protected StateMachineBuilderWithStates(IStateMachineContainer stateMachineContainer, Func<StateMachineData<StateIdT>> stateMachineDataBuilder) : this(stateMachineContainer)
        {
            _stateMachineData = stateMachineDataBuilder.Invoke();
        }

        protected StateMachineBuilderWithStates(IStateMachineContainer stateMachineContainer)
        {
            _stateMachineContainer = stateMachineContainer;
        }

        public StateBuilder<StateT, StateIdT, EventIdT, StateMachineBuilderWithStates<StateIdT>> State<StateT, EventIdT>(StateIdT stateId)
            where StateT : IState
            where EventIdT : Enum
        {
            if (_states.TryGetValue(stateId, out var exisitingStateRunner))
            {
                throw new ArgumentException($"Could not create state {typeof(StateT)} with id {stateId}. {exisitingStateRunner.StateId} already found!");
            }

            var stateData = new StateData<StateT, StateIdT, EventIdT>(stateId, _stateMachineContainer, _stateHandlerTypes);
            _states.Add(stateId, stateData);
            return new StateBuilder<StateT, StateIdT, EventIdT, StateMachineBuilderWithStates<StateIdT>>(this, stateData);
        }

        public StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, StateMachineBuilderWithStates<StateIdT>> State<StateT, EventIdT, SubStateIdT>(StateIdT stateId)
            where StateT : IState
            where EventIdT : Enum
            where SubStateIdT : Enum
        {
            if (_states.TryGetValue(stateId, out var existingStateData))
            {
                throw new ArgumentException($"Could not create state {typeof(StateT)} with id {stateId}. {existingStateData.StateId} already found!");
            }

            var stateData = new StateData<StateT, StateIdT, EventIdT, SubStateIdT>(stateId, _stateMachineContainer, _stateHandlerTypes);
            _states.Add(stateId, stateData);
            return new StateBuilderWithSubStates<StateT, StateIdT, EventIdT, SubStateIdT, StateMachineBuilderWithStates<StateIdT>>(this, stateData, _stateMachineContainer, _stateHandlerTypes);
        }

        public IStateMachine<StateIdT> Build()
        {
            BaseAddStateHandler<IState, EnterAndExitStateHandler>();
            return new StateMachine<StateIdT>(_states, _stateMachineData);
        }

        protected void BaseAddStateHandler<StateT, StateHandlerT>()
            where StateHandlerT : IStateHandler<StateT>
        {
            if (_stateHandlerTypes.TryGetValue(typeof(StateT), out var list))
            {
                list.Add(typeof(StateHandlerT));
            }
            else
            {
                _stateHandlerTypes.Add(typeof(StateT), new List<Type>() { typeof(StateHandlerT) });
            }
        }
    }
}
