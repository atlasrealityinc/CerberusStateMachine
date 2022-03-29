using Cerberus.Builder.Data;
using Cerberus.StateController;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Runner
{
    internal class StateMachineRunner<StateIdT>
    {
        public virtual BindInfo BindInfo => null;
    }

    internal class StateMachineRunner<StateIdT, EventIdT> : StateMachineRunner<StateIdT>
        where StateIdT : Enum
        where EventIdT : Enum
    {
        private readonly IStateChanger<StateIdT> _stateChanger;
        protected readonly Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>> _events;

        private BindInfo _bindInfo = null;
        public override BindInfo BindInfo
        {
            get
            {
                if (_bindInfo == null)
                {
                    var instance = CreateStateController();
                    var instanceType = instance.GetType();
                    _bindInfo = new BindInfo(instance, instanceType.GetInterfaces());
                }
                return _bindInfo;
            }
        }

        public StateMachineRunner(StateMachineData<StateIdT> stateMachineData, IStateChanger<StateIdT> stateChanger)
        {
            _events = stateMachineData.StateMachineEvents?.ToDictionary(kvp => (EventIdT)kvp.Key, kvp => kvp.Value) ?? new Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>>();
            _stateChanger = stateChanger;
        }

        private bool TriggerEvent(EventIdT eventId)
        {
            if (_events.TryGetValue(eventId, out var action))
            {
                action?.Invoke(new StateMachineEvent<StateIdT>(_stateChanger));
                return true;
            }
            return false;
        }

        protected object CreateStateController()
        {
            return new StateController<EventIdT>(TriggerEvent);
        }
    }
}
