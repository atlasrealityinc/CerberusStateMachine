using Cerberus.Runner;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.StateController
{
    internal class StateMachineStateControllerProvider<MainStateIdT> : IStateControllerProvider
    {
        public IEnumerable<BindInfo> StateControllers
        {
            get
            {
                if (_stateMachineRunner != null && _stateMachineRunner.BindInfo != null)
                {
                    return _stateControllers.SelectMany(sc => sc.Value.Values).Prepend(_stateMachineRunner.BindInfo);
                }
                return _stateControllers.SelectMany(sc => sc.Value.Values);
            }
        }

        //First key: StateId
        //Second Key: EventId Type
        private readonly Dictionary<Enum, Dictionary<Type, BindInfo>> _stateControllers = new Dictionary<Enum, Dictionary<Type, BindInfo>>();
        private readonly StateMachineRunner<MainStateIdT> _stateMachineRunner;

        public StateMachineStateControllerProvider(Dictionary<Enum, Dictionary<Type, BindInfo>> stateControllerBindInfo, StateMachineRunner<MainStateIdT> stateMachineRunner)
        {
            _stateControllers = stateControllerBindInfo;
            _stateMachineRunner = stateMachineRunner;
        }

        public T GetStateController<T, StateIdT, EventIdT>(StateIdT stateId)
            where T : IStateController<EventIdT>
            where StateIdT : Enum
            where EventIdT : Enum
        {
            if (_stateControllers.TryGetValue(stateId, out var stateControllersByEventId))
            {
                if (stateControllersByEventId.TryGetValue(typeof(EventIdT), out var bindInfo))
                {
                    if (bindInfo.Instance is T expectedInstance)
                    {
                        return expectedInstance;
                    }
                    throw new ArgumentException($"State controller found with event id type {typeof(EventIdT)} for state id {stateId} not of expected type {typeof(T)}");
                }
                throw new ArgumentException($"No state controllers with event id type {typeof(EventIdT)} for state id {stateId}");
            }
            throw new ArgumentException($"No state controllers found for state id {stateId}");
        }

        public IEnumerable<StateControllerBindInfo<StateIdT>> GetStateControllers<StateIdT>()
            where StateIdT : Enum
        {
            foreach (var stateControllerByStateId in _stateControllers)
            {
                if (stateControllerByStateId.Key is StateIdT)
                {
                    foreach (var bindInfo in _stateControllers[stateControllerByStateId.Key])
                    {
                        if (bindInfo.Value is StateControllerBindInfo<StateIdT> stateControllerBindInfo)
                        {
                            yield return stateControllerBindInfo;
                        }
                    }
                }
            }
        }
    }
}
