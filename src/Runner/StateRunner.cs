using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.StateController;
using Cerberus.StateHandlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Runner
{
    internal abstract class StateRunner<StateIdT> : IStateRunner
            where StateIdT : Enum
    {
        protected readonly IStateMachineContainer _container;

#pragma warning disable CS0618 // Backs the deprecated per-state controller API, see docs/DEPRECATED.md
        protected BindInfo[] _bindInfo = null;
        public virtual BindInfo[] BindInfo
        {
            get
            {
                if (_bindInfo == null)
                {
                    var instance = CreateStateController();
                    var instanceType = instance.GetType();
                    _bindInfo = new BindInfo[] { new StateControllerBindInfo<StateIdT>(StateId, instance, instanceType.GetInterfaces()) };
                }
                return _bindInfo;
            }
        }
#pragma warning restore CS0618
        public StateIdT StateId { get; }

        public virtual IStateRunner ActiveSubStateRunner => null;

        protected StateRunner(StateData<StateIdT> stateData)
        {
            StateId = stateData.StateId;
            _container = stateData.StateMachineContainer;
        }

        public abstract void Start(StateIdT previousStateId);

        public abstract void Stop();

        protected abstract object CreateStateController();
    }

    internal abstract class StateRunner<StateT, StateIdT> : StateRunner<StateIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        protected readonly IStateChanger<StateIdT> _stateChanger;
        protected readonly Stack<object> _stateHandlerInstances = new Stack<object>();
        //Flattened at Build() into the handler types that apply to StateT, in registration order, so Start() can walk it without re-checking IsAssignableFrom
        protected readonly Type[] _stateHandlerTypes;

        protected StateIdT _previousStateId;

        protected object[] _activeInstance = new object[1];
        public IState ActiveInstance
        {
            get { return (StateT)_activeInstance[0]; }
            protected set
            {
                _activeInstance[0] = value;
            }
        }

        public StateRunner(StateData<StateT, StateIdT> stateData, IStateChanger<StateIdT> stateChanger) : base(stateData)
        {
            _stateChanger = stateChanger;
            _stateHandlerTypes = FlattenStateHandlerTypes(stateData.StateHandlerRegistrations);
        }

        public override void Start(StateIdT previousStateId)
        {
            if (ActiveInstance != null)
            {
                return;
            }

            _previousStateId = previousStateId;
            ActiveInstance = _container.Resolve<StateT>();
            foreach (var stateHandlerType in _stateHandlerTypes)
            {
                var stateHandlerInstance = _container.Resolve(stateHandlerType);
                _stateHandlerInstances.Push(stateHandlerInstance);
                stateHandlerInstance.GetType().GetMethod("OnEnterState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(stateHandlerInstance, _activeInstance);
                if (ActiveInstance == null)
                {
                    //Calling the OnEnterState can result in the state changing, so if the state changes and Stop() is called (And the ActiveInstance becomes null) we stop
                    return;
                }
            }
        }

        public override void Stop()
        {
            if (ActiveInstance == null)
            {
                return;
            }

            while (_stateHandlerInstances.Count > 0)
            {
                var stateHandlerInstance = _stateHandlerInstances.Pop();
                stateHandlerInstance.GetType().GetMethod("OnExitState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(stateHandlerInstance, _activeInstance);
            }
            ActiveInstance = null;
        }

        public void StateChangerChangeState(StateIdT stateId)
        {
            _stateChanger?.ChangeState(stateId);
        }

        private static Type[] FlattenStateHandlerTypes(StateHandlerRegistrations stateHandlerRegistrations)
        {
            var flattened = new List<Type>();
            var hasEnterAndExitStateHandler = false;
            foreach (var registration in stateHandlerRegistrations)
            {
                if (!registration.StateType.IsAssignableFrom(typeof(StateT)))
                {
                    continue;
                }

                if (registration.HandlerType == typeof(EnterAndExitStateHandler))
                {
                    hasEnterAndExitStateHandler = true;
                    continue;
                }
                flattened.Add(registration.HandlerType);
            }

            //The built-in handler is what calls the state's own OnEnter()/OnExit(), so it always goes last: user handlers enter before the state and, because Stop() pops a stack, exit after it.
            //Build() already appends it last, this also covers registrations built by hand and a repeated Build()
            if (hasEnterAndExitStateHandler)
            {
                flattened.Add(typeof(EnterAndExitStateHandler));
            }
            return flattened.ToArray();
        }
    }

    internal class StateRunner<StateT, StateIdT, EventIdT> : StateRunner<StateT, StateIdT>, IEventTrigger<EventIdT>
        where StateT : IState
        where StateIdT : Enum
    {
        protected readonly Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>> _events;
        //Snapshotted at Build() into an array so TriggerEvent can index it without allocating
        protected readonly PredicateEvent<EventIdT, IStateEvent<StateT, StateIdT, EventIdT>>[] _predicateEvents;

        public StateRunner(StateData<StateT, StateIdT, EventIdT> stateData, IStateChanger<StateIdT> stateChanger) : base(stateData, stateChanger)
        {
            _events = stateData.StateEvents ?? new Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>>();
            _predicateEvents = stateData.PredicateEvents.ToArray();
        }

        public bool TriggerEvent(EventIdT eventId)
        {
            if (ActiveInstance == null)
                return false;

            //A key-based registration takes precedence over every predicate
            if (_events.TryGetValue(eventId, out var action))
            {
                action?.Invoke(new StateEvent<StateT, StateIdT>(this, _previousStateId));
                return true;
            }

            //First predicate to match wins, so at most one handler runs per trigger. The plain indexed loop keeps this allocation-free
            for (var i = 0; i < _predicateEvents.Length; i++)
            {
                if (_predicateEvents[i].Predicate.Invoke(eventId))
                {
                    _predicateEvents[i].Action?.Invoke(new StateEvent<StateT, StateIdT, EventIdT>(this, _previousStateId, eventId));
                    return true;
                }
            }
            return false;
        }

        protected override object CreateStateController()
        {
            return new StateController<EventIdT>(TriggerEvent);
        }
    }

    internal class StateRunner<StateT, StateIdT, EventIdT, SubStateIdT> : StateRunner<StateT, StateIdT, EventIdT>, IStateChanger<SubStateIdT>
        where StateT : IState
        where StateIdT : Enum
        where SubStateIdT : Enum
    {
        protected readonly Dictionary<SubStateIdT, StateRunner<SubStateIdT>> _subStateRunners;

        public SubStateIdT DefaultSubStateId { get; }
        public StateRunner<SubStateIdT> ActiveSubState { get; private set; }

        public override IStateRunner ActiveSubStateRunner => ActiveSubState;

#pragma warning disable CS0618 // Backs the deprecated per-state controller API, see docs/DEPRECATED.md
        public override BindInfo[] BindInfo
        {
            get
            {
                if (_bindInfo == null)
                {
                    var instance = CreateStateController();
                    var instanceType = instance.GetType();
                    _bindInfo = new BindInfo[] { new StateControllerBindInfo<StateIdT>(StateId, instance, instanceType.GetInterfaces()) };
                    if (_subStateRunners.Count > 0)
                    {
                        _bindInfo = ConcatArrays(_bindInfo, _subStateRunners.SelectMany(sr => sr.Value.BindInfo).ToArray());
                    }
                }
                return _bindInfo;
            }
        }
#pragma warning restore CS0618

        public StateRunner(StateData<StateT, StateIdT, EventIdT, SubStateIdT> stateData, IStateChanger<StateIdT> stateChanger) : base(stateData, stateChanger)
        {
            _subStateRunners = stateData.SubStateData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build(this));
            DefaultSubStateId = _subStateRunners.First().Key;
        }

        public override void Start(StateIdT previousStateId)
        {
            if (ActiveInstance != null)
            {
                return;
            }

            base.Start(previousStateId);

            if (ActiveInstance == null)
            {
                //A handler's OnEnterState changed state and Stop() ran mid-enter, so this state is no longer active and must not enter a sub-state
                return;
            }

            if (ActiveSubState != null)
            {
                //Shouldnt ever get here
                ActiveSubState.Start(DefaultSubStateId);
                return;
            }

            ActiveSubState = _subStateRunners[DefaultSubStateId];
            ActiveSubState.Start(DefaultSubStateId);
        }

        public override void Stop()
        {
            if (ActiveInstance == null)
            {
                return;
            }

            //ActiveSubState is still null while this state's own handlers are entering, so a handler-triggered change must still fall through to base.Stop()
            if (ActiveSubState != null)
            {
                ActiveSubState.Stop();
                ActiveSubState = null;
            }

            base.Stop();
        }

        public void ChangeState(SubStateIdT stateId)
        {
            var previousSubStateId = DefaultSubStateId;
            if (ActiveSubState != null)
            {
                if (ActiveSubState.StateId.Equals(stateId))
                {
                    return;
                }

                previousSubStateId = ActiveSubState.StateId;
                ActiveSubState.Stop();
            }

            //If calling Stop() on ActiveSubState causes us to exit this state, we do not want to call Start() on the next substate
            if (ActiveInstance == null)
            {
                return;
            }

            if (_subStateRunners.TryGetValue(stateId, out var nextSubStateRunner))
            {
                ActiveSubState = nextSubStateRunner;
                ActiveSubState.Start(previousSubStateId);
            }
        }

        protected override object CreateStateController()
        {
            return new StateController<StateT, StateIdT, EventIdT, SubStateIdT>(TriggerEvent, this);
        }

        private static T[] ConcatArrays<T>(params T[][] p)
        {
            var position = 0;
            var outputArray = new T[p.Sum(a => a.Length)];
            foreach (var curr in p)
            {
                Array.Copy(curr, 0, outputArray, position, curr.Length);
                position += curr.Length;
            }
            return outputArray;
        }
    }
}
