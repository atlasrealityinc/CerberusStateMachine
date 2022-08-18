using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.StateController;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Runner
{
    internal abstract class StateRunner<StateIdT>
            where StateIdT : Enum
    {
        protected readonly IStateMachineContainer _container;
        protected readonly Dictionary<Type, List<Type>> _stateHandlerTypes;

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
        public StateIdT StateId { get; }

        protected StateRunner(StateData<StateIdT> stateData)
        {
            StateId = stateData.StateId;
            _container = stateData.StateMachineContainer;
            _stateHandlerTypes = stateData.StateHandlerTypes;
        }

        public abstract void Start(StateIdT previousStateId);

        public abstract void Stop();

        protected abstract object CreateStateController();
    }

    internal abstract class StateRunner<StateT, StateIdT> : StateRunner<StateIdT>
        where StateT : State
        where StateIdT : Enum
    {
        protected readonly IStateChanger<StateIdT> _stateChanger;
        protected readonly Stack<object> _stateHandlerInstances = new Stack<object>();

        protected StateIdT _previousStateId;

        protected object[] _activeInstance = new object[1];
        public StateT ActiveInstance
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
        }

        public override void Start(StateIdT previousStateId)
        {
            if (ActiveInstance != null)
            {
                return;
            }

            _previousStateId = previousStateId;
            ActiveInstance = _container.Resolve<StateT>();
            foreach (var stateHandlerTypeKvp in _stateHandlerTypes)
            {
                if (stateHandlerTypeKvp.Key.IsAssignableFrom(typeof(StateT)))
                {
                    foreach (var stateHandler in stateHandlerTypeKvp.Value)
                    {
                        var stateHandlerInstance = _container.Resolve(stateHandler);
                        _stateHandlerInstances.Push(stateHandlerInstance);
                        stateHandlerInstance.GetType().GetMethod("OnEnterState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Invoke(stateHandlerInstance, _activeInstance);
                        if(ActiveInstance == null)
                        {
                            //Calling the OnEnterState can result in the state changing, so if the state changes and Stop() is called (And the ActiveInstance becomes null) we stop
                            return;
                        }
                    }
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
    }

    internal class StateRunner<StateT, StateIdT, EventIdT> : StateRunner<StateT, StateIdT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
    {
        protected readonly Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>> _events;

        public StateRunner(StateData<StateT, StateIdT, EventIdT> stateData, IStateChanger<StateIdT> stateChanger) : base(stateData, stateChanger)
        {
            _events = stateData.StateEvents ?? new Dictionary<EventIdT, Action<IStateEvent<StateT, StateIdT>>>();
        }

        public bool TriggerEvent(EventIdT eventId)
        {
            if (ActiveInstance == null)
                return false;

            if (_events.TryGetValue(eventId, out var action))
            {
                action?.Invoke(new StateEvent<StateT, StateIdT>(this, _previousStateId));
                return true;
            }
            return false;
        }

        protected override object CreateStateController()
        {
            return new StateController<EventIdT>(TriggerEvent);
        }
    }

    internal class StateRunner<StateT, StateIdT, EventIdT, SubStateIdT> : StateRunner<StateT, StateIdT, EventIdT>, IStateChanger<SubStateIdT>
        where StateT : State
        where StateIdT : Enum
        where EventIdT : Enum
        where SubStateIdT : Enum
    {
        protected readonly Dictionary<SubStateIdT, StateRunner<SubStateIdT>> _subStateRunners;

        public SubStateIdT DefaultSubStateId { get; }
        public StateRunner<SubStateIdT> ActiveSubState { get; private set; }

        public override BindInfo[] BindInfo
        {
            get
            {
                if (_bindInfo == null)
                {
                    var instance = CreateStateController();
                    var instanceType = instance.GetType();
                    _bindInfo = new BindInfo[] { new StateControllerBindInfo<StateIdT>(StateId, instance, instanceType.GetInterfaces()) };
                    _bindInfo = ConcatArrays(_bindInfo, _subStateRunners.SelectMany(sr => sr.Value.BindInfo).ToArray());
                }
                return _bindInfo;
            }
        }

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

            if (ActiveSubState == null)
            {
                return;
            }

            ActiveSubState.Stop();
            ActiveSubState = null;

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
