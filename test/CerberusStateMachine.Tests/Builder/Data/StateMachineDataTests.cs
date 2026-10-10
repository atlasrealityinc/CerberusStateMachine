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

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => data.AddEvent((string)null, e => { }));

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

        [TestMethod]
        public void Test_AddEvent_Predicate_AddsToPredicateEvents()
        {
            var data = new StateMachineData<TestStateId, TestFlagEventId>();
            Func<TestFlagEventId, bool> predicate = e => e.IsOn;
            Action<StateMachineEvent<TestStateId, TestFlagEventId>> action = e => { };

            data.AddEvent(predicate, action);

            Assert.AreEqual(1, data.PredicateEvents.Count);
            Assert.AreSame(predicate, data.PredicateEvents[0].Predicate);
            Assert.AreSame(action, data.PredicateEvents[0].Action);
            Assert.AreEqual(0, data.StateMachineEvents.Count, "Predicate events are stored separately from key-based events");
        }

        [TestMethod]
        public void Test_AddEvent_Predicate_NullPredicate_ThrowsArgumentNullException()
        {
            var data = new StateMachineData<TestStateId, TestFlagEventId>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => data.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));

            Assert.AreEqual("predicate", ex.ParamName);
            Assert.AreEqual(0, data.PredicateEvents.Count);
        }

        [TestMethod]
        public void Test_AddEvent_Predicate_SamePredicateTwice_IsAllowedAndKeepsRegistrationOrder()
        {
            var data = new StateMachineData<TestStateId, TestFlagEventId>();
            Func<TestFlagEventId, bool> predicate = e => e.IsOn;
            Action<StateMachineEvent<TestStateId, TestFlagEventId>> first = e => { };
            Action<StateMachineEvent<TestStateId, TestFlagEventId>> second = e => { };

            data.AddEvent(predicate, first);
            data.AddEvent(predicate, second);

            Assert.AreEqual(2, data.PredicateEvents.Count);
            Assert.AreSame(first, data.PredicateEvents[0].Action);
            Assert.AreSame(second, data.PredicateEvents[1].Action);
        }

        [TestMethod]
        public void Test_TypedBuild_SnapshotsPredicateEvents_SoLaterRegistrationsDoNotReachTheRunner()
        {
            var data = new StateMachineData<TestStateId, TestFlagEventId>();
            data.AddEvent(e => e.IsOn, e => { });
            var runner = (IEventTrigger<TestFlagEventId>)data.Build(Substitute.For<IStateChanger<TestStateId>>());

            data.AddEvent(e => !e.IsOn, e => { });

            Assert.IsTrue(runner.TriggerEvent(TestFlagEventId.On));
            Assert.IsFalse(runner.TriggerEvent(TestFlagEventId.Off));
        }
    }
}
