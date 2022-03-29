using System;
using System.Collections.Generic;

namespace Cerberus.StateController
{
    public interface IStateControllerProvider
    {
        T GetStateController<T, StateIdT, EventIdT>(StateIdT stateId)
            where T : IStateController<EventIdT>
            where StateIdT : Enum
            where EventIdT : Enum;

        IEnumerable<BindInfo> StateControllers { get; }
        IEnumerable<StateControllerBindInfo<StateIdT>> GetStateControllers<StateIdT>()
            where StateIdT : Enum;
    }

    public class BindInfo
    {
        public Type[] ContractTypes { get; }
        public object Instance { get; }

        public BindInfo(object instance, Type[] contractTypes)
        {
            ContractTypes = contractTypes;
            Instance = instance;
        }
    }

    public class StateControllerBindInfo<StateIdT> : BindInfo
        where StateIdT : Enum
    {
        public StateIdT State { get; }

        public StateControllerBindInfo(StateIdT stateId, object instance, Type[] contractTypes) : base(instance, contractTypes)
        {
            State = stateId;
        }
    }
}
