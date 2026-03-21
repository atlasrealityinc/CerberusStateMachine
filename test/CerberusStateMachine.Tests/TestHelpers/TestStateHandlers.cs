using System;
using System.Collections.Generic;

namespace Cerberus.Tests.TestHelpers
{
    public class TrackingStateHandler : IStateHandler<IState>
    {
        private readonly List<string> _callLog;
        private readonly string _name;

        public TrackingStateHandler(List<string> callLog, string name)
        {
            _callLog = callLog;
            _name = name;
        }

        public void OnEnterState(IState stateInstance)
        {
            _callLog.Add($"{_name}:Enter:{stateInstance.GetType().Name}");
        }

        public void OnExitState(IState stateInstance)
        {
            _callLog.Add($"{_name}:Exit:{stateInstance.GetType().Name}");
        }
    }

    public class TrackingStateHandlerA : TrackingStateHandler
    {
        public TrackingStateHandlerA(List<string> callLog) : base(callLog, "HandlerA") { }
    }

    public class TrackingStateHandlerB : TrackingStateHandler
    {
        public TrackingStateHandlerB(List<string> callLog) : base(callLog, "HandlerB") { }
    }

    public class StateChangingHandler : IStateHandler<IState>
    {
        private readonly Action<IState> _onEnterAction;

        public StateChangingHandler(Action<IState> onEnterAction)
        {
            _onEnterAction = onEnterAction;
        }

        public void OnEnterState(IState stateInstance)
        {
            _onEnterAction?.Invoke(stateInstance);
        }

        public void OnExitState(IState stateInstance) { }
    }

    public class SubStateExitHandler : IStateHandler<IState>
    {
        private readonly Action<IState> _onExitAction;

        public SubStateExitHandler(Action<IState> onExitAction)
        {
            _onExitAction = onExitAction;
        }

        public void OnEnterState(IState stateInstance) { }

        public void OnExitState(IState stateInstance)
        {
            _onExitAction?.Invoke(stateInstance);
        }
    }

    public class CustomStateHandler : IStateHandler<ICustomState>
    {
        private readonly List<string> _callLog;

        public CustomStateHandler(List<string> callLog)
        {
            _callLog = callLog;
        }

        public void OnEnterState(ICustomState stateInstance)
        {
            _callLog.Add($"CustomHandler:Enter:{stateInstance.GetType().Name}");
        }

        public void OnExitState(ICustomState stateInstance)
        {
            _callLog.Add($"CustomHandler:Exit:{stateInstance.GetType().Name}");
        }
    }
}
