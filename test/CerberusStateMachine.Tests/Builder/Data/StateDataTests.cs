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
    }
}
