using System;
using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateMachineDataTests
    {
        [TestMethod]
        public void Test_Build_ReturnsStateMachineRunner()
        {
            var data = new StateMachineData<TestStateId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsNull(runner.BindInfo);
        }

        [TestMethod]
        public void Test_TypedBuild_ReturnsTypedStateMachineRunner()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsInstanceOfType(runner, typeof(StateMachineRunner<TestStateId, TestMachineEventId>));
        }

        [TestMethod]
        public void Test_AddEvent_RegistersEvent()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();

            data.AddEvent(TestMachineEventId.MachineEvent1, e => { });

            Assert.IsTrue(data.StateMachineEvents.ContainsKey(TestMachineEventId.MachineEvent1));
        }

        [TestMethod]
        public void Test_AddEvent_DuplicateEventId_ThrowsArgumentException()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            data.AddEvent(TestMachineEventId.MachineEvent1, e => { });

            Assert.ThrowsExactly<ArgumentException>(() => data.AddEvent(TestMachineEventId.MachineEvent1, e => { }));
        }

        [TestMethod]
        public void Test_AddEvent_NullEventId_ThrowsArgumentNullException()
        {
            var data = new StateMachineData<TestStateId, string>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => data.AddEvent(null, e => { }));

            Assert.AreEqual("eventId", ex.ParamName);
        }

        [TestMethod]
        public void Test_TypedBuild_SnapshotsEvents_SoLaterRegistrationsDoNotReachTheRunner()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            data.AddEvent(TestMachineEventId.MachineEvent1, e => { });
            var runner = (IEventTrigger<TestMachineEventId>)data.Build(Substitute.For<IStateChanger<TestStateId>>());

            data.AddEvent(TestMachineEventId.MachineEvent2, e => { });

            Assert.IsTrue(runner.TriggerEvent(TestMachineEventId.MachineEvent1));
            Assert.IsFalse(runner.TriggerEvent(TestMachineEventId.MachineEvent2));
        }
    }
}
