using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests
{
    [TestClass]
    public class StateMachineTests
    {
        private IStateMachineContainer CreateContainer(List<string> callLog)
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingState2>().Returns(_ => new TrackingState2(callLog));
            container.Resolve<TrackingState3>().Returns(_ => new TrackingState3(callLog));
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            return container;
        }

        private StateMachine<TestStateId> BuildStateMachine(
            Dictionary<TestStateId, StateData<TestStateId>> stateData,
            StateMachineData<TestStateId> machineData = null)
        {
            return new StateMachine<TestStateId>(stateData, machineData);
        }

        [TestMethod]
        public void Test_Constructor_WithNoStates_ThrowsArgumentException()
        {
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>();

            Assert.ThrowsException<ArgumentException>(() => BuildStateMachine(stateData));
        }

        [TestMethod]
        public void Test_Constructor_SetsStateControllerProvider()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) }
            };

            var sm = BuildStateMachine(stateData);

            Assert.IsNotNull(sm.StateControllerProvider);
        }

        [TestMethod]
        public void Test_Start_EntersDefaultState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) },
                { TestStateId.State2, new StateData<TrackingState2, TestStateId, TestEventId>(TestStateId.State2, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);

            sm.Start();

            Assert.AreEqual(1, callLog.Count);
            Assert.AreEqual("State1:Enter", callLog[0]);
        }

        [TestMethod]
        public void Test_Start_CalledMultipleTimes_IsIdempotent()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);

            sm.Start();
            sm.Start();
            sm.Start();

            Assert.AreEqual(1, callLog.Count);
        }

        [TestMethod]
        public void Test_ChangeState_TransitionsToNewState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) },
                { TestStateId.State2, new StateData<TrackingState2, TestStateId, TestEventId>(TestStateId.State2, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);
            sm.Start();
            callLog.Clear();

            sm.ChangeState(TestStateId.State2);

            CollectionAssert.AreEqual(new[] { "State1:Exit", "State2:Enter" }, callLog);
        }

        [TestMethod]
        public void Test_ChangeState_ToSameState_IsNoOp()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);
            sm.Start();
            callLog.Clear();

            sm.ChangeState(TestStateId.State1);

            Assert.AreEqual(0, callLog.Count);
        }

        [TestMethod]
        public void Test_ChangeState_StopsActiveStateBeforeStartingNew()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) },
                { TestStateId.State2, new StateData<TrackingState2, TestStateId, TestEventId>(TestStateId.State2, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);
            sm.Start();
            callLog.Clear();

            sm.ChangeState(TestStateId.State2);

            Assert.AreEqual("State1:Exit", callLog[0]);
            Assert.AreEqual("State2:Enter", callLog[1]);
        }

        [TestMethod]
        public void Test_ChangeState_InvalidStateId_DoesNothing()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);
            sm.Start();
            callLog.Clear();

            sm.ChangeState(TestStateId.State2);

            Assert.AreEqual(1, callLog.Count);
            Assert.AreEqual("State1:Exit", callLog[0]);
        }

        [TestMethod]
        public void Test_Constructor_DefaultStateIsFirstRegistered()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new Dictionary<TestStateId, StateData<TestStateId>>
            {
                { TestStateId.State2, new StateData<TrackingState2, TestStateId, TestEventId>(TestStateId.State2, container, handlerTypes) },
                { TestStateId.State1, new StateData<TrackingState1, TestStateId, TestEventId>(TestStateId.State1, container, handlerTypes) }
            };
            var sm = BuildStateMachine(stateData);

            sm.Start();

            Assert.AreEqual("State2:Enter", callLog[0]);
        }
    }
}
