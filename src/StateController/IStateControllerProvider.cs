using System;
using System.Collections.Generic;

namespace Cerberus.StateController
{
    /// <summary>
    /// Looks up controllers bound to individual states.
    /// </summary>
    /// <remarks>
    /// Deprecated. Use <see cref="IStateMachine{StateIdT}.StateController"/> instead, which needs no lookup, see docs/DEPRECATED.md.
    /// </remarks>
    [Obsolete("Deprecated: use IStateMachine<StateIdT>.StateController.TriggerEvent(eventId) instead, it triggers the event on whichever states are active. See docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
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

    /// <summary>
    /// Describes a per-state controller instance and the interfaces it implements.
    /// </summary>
    /// <remarks>
    /// Deprecated along with <see cref="IStateControllerProvider"/>, see docs/DEPRECATED.md.
    /// </remarks>
    [Obsolete("Deprecated: only used by the deprecated IStateControllerProvider. Use IStateMachine<StateIdT>.StateController instead, see docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
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

    /// <summary>
    /// A <see cref="BindInfo"/> that also records the state id the controller is bound to.
    /// </summary>
    /// <remarks>
    /// Deprecated along with <see cref="IStateControllerProvider"/>, see docs/DEPRECATED.md.
    /// </remarks>
    [Obsolete("Deprecated: only used by the deprecated IStateControllerProvider. Use IStateMachine<StateIdT>.StateController instead, see docs/DEPRECATED.md in the CerberusStateMachine repository for a migration guide.")]
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
