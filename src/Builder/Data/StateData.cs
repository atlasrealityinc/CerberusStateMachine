using Cerberus.IoC;
using Cerberus.Runner;
using System;
using System.Collections.Generic;

namespace Cerberus.Builder.Data
{
    internal abstract class StateData<StateIdT>
            where StateIdT : Enum
    {
        internal IStateMachineContainer StateMachineContainer { get; }
        internal Dictionary<Type, List<Type>> StateHandlerTypes { get; }

        public StateIdT StateId { get; }

        protected StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, Dictionary<Type, List<Type>> stateHandlerTypes)
        {
            StateId = stateId;
            StateMachineContainer = stateMachineContainer ?? throw new ArgumentNullException(nameof(stateMachineContainer));
            StateHandlerTypes = stateHandlerTypes ?? new Dictionary<Type, List<Type>>();
        }

        public abstract StateRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger);
    }

    internal abstract class StateData<StateT, StateIdT> : StateData<StateIdT>
        where StateT : State
        where StateIdT : Enum
    {
        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, Dictionary<Type, List<Type>> stateHandlerTypes) : base(stateId, stateMachineContainer, stateHandlerTypes)
        {

        }
    }

    internal class StateData<StateT, StateIdT, EventIdT> : StateData<StateT, StateIdT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
    {
        public Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>> StateEvents { get; } = new Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>>();

        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, Dictionary<Type, List<Type>> stateHandlerTypes) : base(stateId, stateMachineContainer, stateHandlerTypes)
        {

        }

        public void AddEvent(EventIdT eventId, Action<IStateEvent<StateT, StateIdT>> action)
        {
            if (StateEvents.ContainsKey(eventId))
            {
                throw new ArgumentException($"Could not add event with id {eventId}. An event with the same id already exists");
            }

            StateEvents.Add(eventId, action);
        }

        public override StateRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateRunner<StateT, StateIdT, EventIdT>(this, stateChanger);
        }
    }

    internal class StateData<StateT, StateIdT, EventIdT, SubStateIdT> : StateData<StateT, StateIdT, EventIdT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
        where SubStateIdT : Enum
    {
        public Dictionary<SubStateIdT, StateData<SubStateIdT>> SubStateData { get; } = new Dictionary<SubStateIdT, StateData<SubStateIdT>>();

        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, Dictionary<Type, List<Type>> stateHandlerTypes) : base(stateId, stateMachineContainer, stateHandlerTypes)
        {

        }

        public void AddSubState(SubStateIdT subStateId, StateData<SubStateIdT> stateData)
        {
            if (SubStateData.TryGetValue(subStateId, out var existingStateData))
            {
                throw new ArgumentException($"Could not create sub-state with id {subStateId}. {existingStateData.StateId} already found!");
            }

            SubStateData.Add(subStateId, stateData);
        }

        public override StateRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateRunner<StateT, StateIdT, EventIdT, SubStateIdT>(this, stateChanger);
        }
    }
}
