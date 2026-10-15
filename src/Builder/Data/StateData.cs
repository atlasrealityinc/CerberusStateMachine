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
        //Shared by reference with the builder, which appends the built-in enter/exit handler at Build(), after this StateData exists
        internal StateHandlerRegistrations StateHandlerRegistrations { get; }

        public StateIdT StateId { get; }

        protected StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, StateHandlerRegistrations stateHandlerRegistrations)
        {
            StateId = stateId;
            StateMachineContainer = stateMachineContainer ?? throw new ArgumentNullException(nameof(stateMachineContainer));
            StateHandlerRegistrations = stateHandlerRegistrations ?? new StateHandlerRegistrations();
        }

        public abstract StateRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger);
    }

    internal abstract class StateData<StateT, StateIdT> : StateData<StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, StateHandlerRegistrations stateHandlerRegistrations) : base(stateId, stateMachineContainer, stateHandlerRegistrations)
        {

        }
    }

    internal class StateData<StateT, StateIdT, EventIdT> : StateData<StateT, StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        public Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>> StateEvents { get; } = new Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>>();
        //Checked in registration order when no key-based event matches, see StateRunner.TriggerEvent
        public List<PredicateEvent<EventIdT, IStateEvent<StateT, StateIdT, EventIdT>>> PredicateEvents { get; } = new List<PredicateEvent<EventIdT, IStateEvent<StateT, StateIdT, EventIdT>>>();

        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, StateHandlerRegistrations stateHandlerRegistrations) : base(stateId, stateMachineContainer, stateHandlerRegistrations)
        {

        }

        public void AddEvent(EventIdT eventId, Action<IStateEvent<StateT, StateIdT>> action)
        {
            if (eventId == null)
            {
                throw new ArgumentNullException(nameof(eventId));
            }

            if (StateEvents.ContainsKey(eventId))
            {
                throw new ArgumentException($"Could not add event with id {eventId}. An event with the same id already exists");
            }

            StateEvents.Add(eventId, action);
        }

        public void AddEvent(Func<EventIdT, bool> predicate, Action<IStateEvent<StateT, StateIdT, EventIdT>> action)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            //Delegates cannot be compared meaningfully, so unlike key-based events there is no duplicate check
            PredicateEvents.Add(new PredicateEvent<EventIdT, IStateEvent<StateT, StateIdT, EventIdT>>(predicate, action));
        }

        public override StateRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateRunner<StateT, StateIdT, EventIdT>(this, stateChanger);
        }
    }

    internal class StateData<StateT, StateIdT, EventIdT, SubStateIdT> : StateData<StateT, StateIdT, EventIdT>
        where StateT : IState
        where StateIdT : Enum
        where SubStateIdT : Enum
    {
        public Dictionary<SubStateIdT, StateData<SubStateIdT>> SubStateData { get; } = new Dictionary<SubStateIdT, StateData<SubStateIdT>>();

        public StateData(StateIdT stateId, IStateMachineContainer stateMachineContainer, StateHandlerRegistrations stateHandlerRegistrations) : base(stateId, stateMachineContainer, stateHandlerRegistrations)
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
