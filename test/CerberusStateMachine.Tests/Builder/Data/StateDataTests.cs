using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateDataTests
    {
        [TestMethod]
        public void Test_Constructor_SetsStateId()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();

            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);

            Assert.AreEqual(TestStateId.State1, stateData.StateId);
        }

        [TestMethod]
        public void Test_Constructor_NullContainer_ThrowsArgumentNullException()
        {
            var handlerTypes = new Dictionary<Type, List<Type>>();

            Assert.ThrowsException<ArgumentNullException>(() =>
                new StateData<NoOpState, TestStateId, TestEventId>(
                    TestStateId.State1, null, handlerTypes));
        }

        [TestMethod]
        public void Test_AddEvent_AddsSuccessfully()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);

            stateData.AddEvent(TestEventId.Event1, e => { });

            Assert.IsTrue(stateData.StateEvents.ContainsKey(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_AddEvent_DuplicateEventId_ThrowsArgumentException()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            stateData.AddEvent(TestEventId.Event1, e => { });

            Assert.ThrowsException<ArgumentException>(() =>
                stateData.AddEvent(TestEventId.Event1, e => { }));
        }

        [TestMethod]
        public void Test_Build_ReturnsStateRunner()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = stateData.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsInstanceOfType(runner, typeof(StateRunner<NoOpState, TestStateId, TestEventId>));
        }

        [TestMethod]
        public void Test_SubState_AddSubState_AddsSuccessfully()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var subStateData = new StateData<NoOpState, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);

            stateData.AddSubState(TestSubStateId.SubState1, subStateData);

            Assert.IsTrue(stateData.SubStateData.ContainsKey(TestSubStateId.SubState1));
        }

        [TestMethod]
        public void Test_SubState_AddSubState_DuplicateId_ThrowsArgumentException()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<NoOpState, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            var sub2 = new StateData<NoOpState, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);

            Assert.ThrowsException<ArgumentException>(() =>
                stateData.AddSubState(TestSubStateId.SubState1, sub2));
        }

        [TestMethod]
        public void Test_SubState_Build_ReturnsStateRunnerWithSubStates()
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<NoOpState>().Returns(new NoOpState());
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var subStateData = new StateData<NoOpState, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, subStateData);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = stateData.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsInstanceOfType(runner, typeof(StateRunner<NoOpState, TestStateId, TestEventId, TestSubStateId>));
        }

        [TestMethod]
        public void Test_AddEvent_Predicate_AddsToPredicateEvents()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var stateData = new StateData<NoOpState, TestStateId, TestFlagEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            Func<TestFlagEventId, bool> predicate = e => e.IsOn;
            Action<IStateEvent<NoOpState, TestStateId, TestFlagEventId>> action = e => { };

            stateData.AddEvent(predicate, action);

            Assert.AreEqual(1, stateData.PredicateEvents.Count);
            Assert.AreSame(predicate, stateData.PredicateEvents[0].Predicate);
            Assert.AreSame(action, stateData.PredicateEvents[0].Action);
            Assert.AreEqual(0, stateData.StateEvents.Count, "Predicate events are stored separately from key-based events");
        }

        [TestMethod]
        public void Test_AddEvent_Predicate_NullPredicate_ThrowsArgumentNullException()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var stateData = new StateData<NoOpState, TestStateId, TestFlagEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => stateData.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));

            Assert.AreEqual("predicate", ex.ParamName);
            Assert.AreEqual(0, stateData.PredicateEvents.Count);
        }

        [TestMethod]
        public void Test_AddEvent_Predicate_SamePredicateTwice_IsAllowedAndKeepsRegistrationOrder()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var stateData = new StateData<NoOpState, TestStateId, TestFlagEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            Func<TestFlagEventId, bool> predicate = e => e.IsOn;
            Action<IStateEvent<NoOpState, TestStateId, TestFlagEventId>> first = e => { };
            Action<IStateEvent<NoOpState, TestStateId, TestFlagEventId>> second = e => { };

            //Delegates cannot be compared, so unlike key-based events there is no duplicate check
            stateData.AddEvent(predicate, first);
            stateData.AddEvent(predicate, second);

            Assert.AreEqual(2, stateData.PredicateEvents.Count);
            Assert.AreSame(first, stateData.PredicateEvents[0].Action);
            Assert.AreSame(second, stateData.PredicateEvents[1].Action);
        }
    }
}
