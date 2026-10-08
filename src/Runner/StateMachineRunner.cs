using Cerberus.Builder.Data;
using Cerberus.StateController;
using System;
using System.Collections.Generic;

namespace Cerberus.Runner
{
    internal class StateMachineRunner<StateIdT> : IStateRunner
    {
#pragma warning disable CS0618 // Backs the deprecated per-state controller API, see docs/DEPRECATED.md
        public virtual BindInfo BindInfo => null;
#pragma warning restore CS0618

        //Machine-level events have no sub-states, the walk ends here
        public IStateRunner ActiveSubStateRunner => null;
    }

    internal class StateMachineRunner<StateIdT, EventIdT> : StateMachineRunner<StateIdT>, IEventTrigger<EventIdT>
        where StateIdT : Enum
    {
        private readonly IStateChanger<StateIdT> _stateChanger;
        protected readonly Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>> _events;

#pragma warning disable CS0618 // Backs the deprecated per-state controller API, see docs/DEPRECATED.md
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
#pragma warning restore CS0618

        public StateMachineRunner(StateMachineData<StateIdT, EventIdT> stateMachineData, IStateChanger<StateIdT> stateChanger)
        {
            //Copied so that registrations made on the builder after Build() do not reach a running machine
            _events = new Dictionary<EventIdT, Action<StateMachineEvent<StateIdT>>>(stateMachineData.StateMachineEvents);
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

        bool IEventTrigger<EventIdT>.TriggerEvent(EventIdT eventId)
        {
            return TriggerEvent(eventId);
        }
    }
}
