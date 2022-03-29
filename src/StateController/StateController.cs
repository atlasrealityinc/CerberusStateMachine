using Cerberus.Runner;
using System;

namespace Cerberus.StateController
{
    internal class StateController<EventIdT> : IStateController<EventIdT>
            where EventIdT : Enum
    {
        private readonly Func<EventIdT, bool> _onTriggerEvent;

        public StateController(Func<EventIdT, bool> onTriggerEvent)
        {
            _onTriggerEvent = onTriggerEvent;
        }

        public bool TriggerEvent(EventIdT eventId)
        {
            return _onTriggerEvent?.Invoke(eventId) ?? false;
        }
    }

    internal class StateController<StateT, StateIdT, EventIdT, SubStateIdT> : StateController<EventIdT>, IStateController<EventIdT, SubStateIdT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
        where SubStateIdT : Enum
    {
        private readonly StateRunner<StateT, StateIdT, EventIdT, SubStateIdT> _stateRunner;

        public SubStateIdT CurrentSubState
        {
            get
            {
                if (_stateRunner.ActiveSubState != null)
                {
                    return _stateRunner.ActiveSubState.StateId;
                }
                return _stateRunner.DefaultSubStateId;
            }
        }

        public StateController(Func<EventIdT, bool> onTriggerEvent, StateRunner<StateT, StateIdT, EventIdT, SubStateIdT> stateRunner) : base(onTriggerEvent)
        {
            _stateRunner = stateRunner;
        }
    }
}
