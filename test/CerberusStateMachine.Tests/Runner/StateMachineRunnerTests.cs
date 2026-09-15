using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

namespace Cerberus.Tests.Runner
{
    [TestClass]
    public class StateMachineRunnerTests
    {
        [TestMethod]
        public void Test_Base_BindInfo_ReturnsNull()
        {
            var data = new StateMachineData<TestStateId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNull(runner.BindInfo);
        }

        [TestMethod]
        public void Test_Typed_BindInfo_ReturnsNonNull()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            data.StateMachineEvents.Add(TestMachineEventId.MachineEvent1, e => { });
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNotNull(runner.BindInfo);
        }

        [TestMethod]
        public void Test_TriggerEvent_RegisteredEvent_InvokesAction()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var eventInvoked = false;
            data.StateMachineEvents.Add(TestMachineEventId.MachineEvent1, e => { eventInvoked = true; });
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = (StateMachineRunner<TestStateId, TestMachineEventId>)data.Build(stateChanger);

            // TriggerEvent is private on StateMachineRunner, so test indirectly via controller
            var bindInfo = runner.BindInfo;
            var controller = (IStateController<TestMachineEventId>)bindInfo.Instance;
            var result = controller.TriggerEvent(TestMachineEventId.MachineEvent1);

            Assert.IsTrue(result);
            Assert.IsTrue(eventInvoked);
        }

        [TestMethod]
        public void Test_TriggerEvent_UnregisteredEvent_ReturnsFalse()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = (StateMachineRunner<TestStateId, TestMachineEventId>)data.Build(stateChanger);

            var bindInfo = runner.BindInfo;
            var controller = (IStateController<TestMachineEventId>)bindInfo.Instance;
            var result = controller.TriggerEvent(TestMachineEventId.MachineEvent1);

            Assert.IsFalse(result);
        }
        [TestMethod]
        public void Test_Base_ActiveSubStateRunner_ReturnsNull()
        {
            var runner = new StateMachineData<TestStateId>().Build(Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsNull(runner.ActiveSubStateRunner);
        }

        [TestMethod]
        public void Test_Typed_ActiveSubStateRunner_ReturnsNull()
        {
            var runner = new StateMachineData<TestStateId, TestMachineEventId>().Build(Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsNull(runner.ActiveSubStateRunner);
        }

        [TestMethod]
        public void Test_Base_DoesNotImplementEventTrigger()
        {
            var runner = new StateMachineData<TestStateId>().Build(Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsInstanceOfType<IStateRunner>(runner);
            Assert.IsNotInstanceOfType<IEventTrigger<TestMachineEventId>>(runner);
        }

        [TestMethod]
        public void Test_Typed_ImplementsEventTriggerOnlyForItsEventIdType()
        {
            var runner = new StateMachineData<TestStateId, TestMachineEventId>().Build(Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsInstanceOfType<IEventTrigger<TestMachineEventId>>(runner);
            Assert.IsNotInstanceOfType<IEventTrigger<TestEventId>>(runner);
        }

        [TestMethod]
        public void Test_EventTrigger_TriggerEvent_RegisteredEvent_InvokesActionAndReturnsTrue()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var eventInvoked = false;
            data.StateMachineEvents.Add(TestMachineEventId.MachineEvent1, e => { eventInvoked = true; });
            var runner = (IEventTrigger<TestMachineEventId>)data.Build(Substitute.For<IStateChanger<TestStateId>>());

            var result = runner.TriggerEvent(TestMachineEventId.MachineEvent1);

            Assert.IsTrue(result);
            Assert.IsTrue(eventInvoked);
        }

        [TestMethod]
        public void Test_EventTrigger_TriggerEvent_UnregisteredEvent_ReturnsFalse()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var runner = (IEventTrigger<TestMachineEventId>)data.Build(Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsFalse(runner.TriggerEvent(TestMachineEventId.MachineEvent1));
        }

    }
}
