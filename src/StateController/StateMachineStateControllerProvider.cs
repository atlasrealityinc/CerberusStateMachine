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
                    return _stateControllers.Prepend(_stateMachineRunner.BindInfo);
                }
                return _stateControllers;
            }
        }

        //Every state controller in the machine, including sub-state controllers, in hierarchy order
        private readonly BindInfo[] _stateControllers;
        //First key: StateId. Each controller is keyed by its own state id, so sub-state controllers are keyed by their sub-state id
        //Second key: EventId Type
        //A list with more than one entry means the same state id and event id type were registered more than once, for
        //example the same sub-state enum used under two different parent states, which makes a lookup for that pair ambiguous
        private readonly Dictionary<Enum, Dictionary<Type, List<BindInfo>>> _stateControllersByStateId = new Dictionary<Enum, Dictionary<Type, List<BindInfo>>>();
        private readonly StateMachineRunner<MainStateIdT> _stateMachineRunner;

        public StateMachineStateControllerProvider(IEnumerable<BindInfo> stateControllers, StateMachineRunner<MainStateIdT> stateMachineRunner)
        {
            _stateControllers = stateControllers?.ToArray() ?? throw new ArgumentNullException(nameof(stateControllers));
            _stateMachineRunner = stateMachineRunner;

            foreach (var bindInfo in _stateControllers)
            {
                if (!(bindInfo is IStateControllerBindInfo stateControllerBindInfo))
                {
                    continue;
                }
                var eventIdType = GetEventIdType(bindInfo);
                if (eventIdType == null)
                {
                    continue;
                }

                if (!_stateControllersByStateId.TryGetValue(stateControllerBindInfo.StateId, out var stateControllersByEventIdType))
                {
                    stateControllersByEventIdType = new Dictionary<Type, List<BindInfo>>();
                    _stateControllersByStateId.Add(stateControllerBindInfo.StateId, stateControllersByEventIdType);
                }
                if (!stateControllersByEventIdType.TryGetValue(eventIdType, out var bindInfos))
                {
                    bindInfos = new List<BindInfo>(1);
                    stateControllersByEventIdType.Add(eventIdType, bindInfos);
                }
                bindInfos.Add(bindInfo);
            }
        }

        public T GetStateController<T, StateIdT, EventIdT>(StateIdT stateId)
            where T : IStateController<EventIdT>
            where StateIdT : Enum
            where EventIdT : Enum
        {
            if (_stateControllersByStateId.TryGetValue(stateId, out var stateControllersByEventIdType))
            {
                if (stateControllersByEventIdType.TryGetValue(typeof(EventIdT), out var bindInfos))
                {
                    if (bindInfos.Count > 1)
                    {
                        throw new ArgumentException($"{bindInfos.Count} state controllers with event id type {typeof(EventIdT)} found for state id {stateId}. The same state id is used under more than one parent state so the lookup is ambiguous, use IStateMachine.StateController to trigger events on whichever is active");
                    }
                    if (bindInfos[0].Instance is T expectedInstance)
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
            return _stateControllers.OfType<StateControllerBindInfo<StateIdT>>();
        }

        private static Type GetEventIdType(BindInfo bindInfo)
        {
            foreach (var contractType in bindInfo.ContractTypes)
            {
                if (contractType.IsGenericType && contractType.GetGenericTypeDefinition() == typeof(IStateController<>))
                {
                    return contractType.GetGenericArguments()[0];
                }
            }
            return null;
        }
    }
}
