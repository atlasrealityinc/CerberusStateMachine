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

    /// <summary>
    /// Non-generic access to the state id a controller belongs to, so controllers for different state id types can be indexed together.
    /// </summary>
    internal interface IStateControllerBindInfo
    {
        Enum StateId { get; }
    }

    public class StateControllerBindInfo<StateIdT> : BindInfo, IStateControllerBindInfo
        where StateIdT : Enum
    {
        public StateIdT State { get; }

        Enum IStateControllerBindInfo.StateId => State;

        public StateControllerBindInfo(StateIdT stateId, object instance, Type[] contractTypes) : base(instance, contractTypes)
        {
            State = stateId;
        }
    }
}
