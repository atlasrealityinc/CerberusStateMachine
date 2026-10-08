using Cerberus.Runner;
using System;
using System.Collections.Generic;

namespace Cerberus.Builder.Data
{
    internal class StateMachineData<StateIdT>
            where StateIdT : Enum
    {
        public virtual StateMachineRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateMachineRunner<StateIdT>();
        }
    }

    internal class StateMachineData<StateIdT, EventIdT> : StateMachineData<StateIdT>
        where StateIdT : Enum
    {
        public Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>> StateMachineEvents { get; } = new Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>>();

        public void AddEvent(EventIdT eventId, Action<StateMachineEvent<StateIdT>> action)
        {
            if (eventId == null)
            {
                throw new ArgumentNullException(nameof(eventId));
            }

            if (StateMachineEvents.ContainsKey(eventId))
            {
                throw new ArgumentException($"Could not add machine-level event with id {eventId}. An event with the same id already exists");
            }

            StateMachineEvents.Add(eventId, action);
        }

        public override StateMachineRunner<StateIdT> Build(IStateChanger<StateIdT> stateChanger)
        {
            return new StateMachineRunner<StateIdT, EventIdT>(this, stateChanger);
        }
    }
}
