using System.Collections.Generic;

namespace Cerberus.Tests.TestHelpers
{
    public interface ICustomState : IState { }

    public class TrackingState : State
    {
        private readonly List<string> _callLog;
        private readonly string _name;

        public bool Entered { get; private set; }
        public bool Exited { get; private set; }

        public TrackingState(List<string> callLog, string name)
        {
            _callLog = callLog;
            _name = name;
        }

        public override void OnEnter()
        {
            Entered = true;
            _callLog.Add($"{_name}:Enter");
        }

        public override void OnExit()
        {
            Exited = true;
            _callLog.Add($"{_name}:Exit");
        }
    }

    public class TrackingState1 : TrackingState
    {
        public TrackingState1(List<string> callLog) : base(callLog, "State1") { }
    }

    public class TrackingState2 : TrackingState
    {
        public TrackingState2(List<string> callLog) : base(callLog, "State2") { }
    }

    public class TrackingState3 : TrackingState
    {
        public TrackingState3(List<string> callLog) : base(callLog, "State3") { }
    }

    public class TrackingSubState1 : TrackingState
    {
        public TrackingSubState1(List<string> callLog) : base(callLog, "SubState1") { }
    }

    public class TrackingSubState2 : TrackingState
    {
        public TrackingSubState2(List<string> callLog) : base(callLog, "SubState2") { }
    }

    public class TrackingGrandchildState1 : TrackingState
    {
        public TrackingGrandchildState1(List<string> callLog) : base(callLog, "GrandchildState1") { }
    }

    public class TrackingGrandchildState2 : TrackingState
    {
        public TrackingGrandchildState2(List<string> callLog) : base(callLog, "GrandchildState2") { }
    }

    public class CustomTrackingState : TrackingState, ICustomState
    {
        public CustomTrackingState(List<string> callLog) : base(callLog, "CustomState") { }
    }

    public class TypedTrackingState : State<string>
    {
        public string EnterResult { get; private set; }
        public string ExitResult { get; private set; }

        public override string OnEnter()
        {
            EnterResult = "entered";
            return EnterResult;
        }

        public override string OnExit()
        {
            ExitResult = "exited";
            return ExitResult;
        }
    }

    public class DualTypedTrackingState : State<string, int>
    {
        public string EnterResult { get; private set; }
        public int ExitResult { get; private set; }

        public override string OnEnter()
        {
            EnterResult = "entered";
            return EnterResult;
        }

        public override int OnExit()
        {
            ExitResult = 42;
            return ExitResult;
        }
    }

    public class NoOpState : IState
    {
        public void OnEnter() { }
        public void OnExit() { }
    }
}
