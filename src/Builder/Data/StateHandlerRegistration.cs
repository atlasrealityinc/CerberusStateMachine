using System;
using System.Collections;
using System.Collections.Generic;

namespace Cerberus.Builder.Data
{
    /// <summary>
    /// One AddStateHandler call: the handler type and the state type it applies to.
    /// </summary>
    internal readonly struct StateHandlerRegistration
    {
        public Type StateType { get; }
        public Type HandlerType { get; }

        public StateHandlerRegistration(Type stateType, Type handlerType)
        {
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }
    }

    /// <summary>
    /// A builder's handler registrations in the order AddStateHandler was called, so the runner invokes
    /// handlers in that order regardless of the state type each one targets. Shared by reference with every
    /// StateData the builder creates, so the built-in handler that Build() appends is seen by all of them.
    /// </summary>
    internal sealed class StateHandlerRegistrations : IEnumerable<StateHandlerRegistration>
    {
        private readonly List<StateHandlerRegistration> _registrations = new List<StateHandlerRegistration>();

        public int Count => _registrations.Count;

        public void Add<StateT, StateHandlerT>()
            where StateHandlerT : IStateHandler<StateT>
        {
            Add(typeof(StateT), typeof(StateHandlerT));
        }

        //Also what collection-initializer syntax calls: new StateHandlerRegistrations { { typeof(IState), typeof(X) } }
        public void Add(Type stateType, Type handlerType)
        {
            _registrations.Add(new StateHandlerRegistration(stateType, handlerType));
        }

        public IEnumerator<StateHandlerRegistration> GetEnumerator() => _registrations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
