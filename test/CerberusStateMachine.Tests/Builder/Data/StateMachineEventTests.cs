using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateMachineEventTests
    {
        [TestMethod]
        public void Test_ChangeState_TriggersStateChangerChangeState()
        {
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var machineEvent = new StateMachineEvent<TestStateId>(stateChanger);

            machineEvent.ChangeState(TestStateId.State2);

            stateChanger.Received(1).ChangeState(TestStateId.State2);
        }

        [TestMethod]
        public void Test_Typed_EventId_ReturnsConstructorValue()
        {
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var machineEvent = new StateMachineEvent<TestStateId, TestMachineEventId>(stateChanger, TestMachineEventId.MachineEvent2);

            Assert.AreEqual(TestMachineEventId.MachineEvent2, machineEvent.EventId);
        }

        [TestMethod]
        public void Test_Typed_IsAlsoTheUntypedContext_AndChangeStateStillDelegates()
        {
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            StateMachineEvent<TestStateId> untyped = new StateMachineEvent<TestStateId, TestMachineEventId>(stateChanger, TestMachineEventId.MachineEvent1);

            untyped.ChangeState(TestStateId.State2);

            stateChanger.Received(1).ChangeState(TestStateId.State2);
        }
    }
}
