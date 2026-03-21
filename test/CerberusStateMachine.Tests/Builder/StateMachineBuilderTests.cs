using Cerberus.Builder;
using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Tests.Builder
{
    [TestClass]
    public class StateMachineBuilderTests
    {
        private IStateMachineContainer CreateContainer(List<string> callLog)
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingState2>().Returns(_ => new TrackingState2(callLog));
            container.Resolve<TrackingState3>().Returns(_ => new TrackingState3(callLog));
            container.Resolve<NoOpState>().Returns(_ => new NoOpState());
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            return container;
        }

        [TestMethod]
        public void Test_Build_WithNoStates_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId>();

            Assert.ThrowsException<ArgumentException>(() => builder.Build());
        }

        [TestMethod]
        public void Test_State_WithDuplicateStateId_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End();

            Assert.ThrowsException<ArgumentException>(() =>
                builder.State<NoOpState, TestEventId>(TestStateId.State1));
        }

        [TestMethod]
        public void Test_State_AddsStateSuccessfully()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_StateWithSubStates_DuplicateStateId_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End();

            Assert.ThrowsException<ArgumentException>(() =>
                builder.State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1));
        }

        [TestMethod]
        public void Test_Build_ReturnsNonNullStateMachine()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_Build_SetsStateControllerProvider()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            Assert.IsNotNull(sm.StateControllerProvider);
        }

        [TestMethod]
        public void Test_Build_AddsEnterAndExitStateHandler()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);

            var sm = new StateMachineBuilder<TestStateId>(container)
                .State<TrackingState1, TestEventId>(TestStateId.State1).End()
                .Build();

            sm.Start();

            Assert.AreEqual(1, callLog.Count);
            Assert.AreEqual("State1:Enter", callLog[0]);
        }

        [TestMethod]
        public void Test_AddStateHandler_RegistersHandler()
        {
            var callLog = new List<string>();
            var handlerA = new TrackingStateHandlerA(callLog);
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve(typeof(TrackingStateHandlerA)).Returns(handlerA);
            container.Resolve(Arg.Is<Type>(t => t != typeof(TrackingStateHandlerA)))
                .Returns(ci => Activator.CreateInstance((Type)ci[0]));

            var sm = new StateMachineBuilder<TestStateId>(container)
                .AddStateHandler<IState, TrackingStateHandlerA>()
                .State<TrackingState1, TestEventId>(TestStateId.State1).End()
                .Build();

            sm.Start();

            Assert.IsTrue(callLog.Contains("HandlerA:Enter:TrackingState1"));
        }

        [TestMethod]
        public void Test_Constructor_WithDefaultContainer()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            sm.Start();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_Constructor_WithCustomContainer()
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<NoOpState>().Returns(new NoOpState());
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));

            var sm = new StateMachineBuilder<TestStateId>(container)
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            sm.Start();

            container.Received().Resolve<NoOpState>();
        }

        [TestMethod]
        public void Test_MachineLevelBuilder_AddEvent_RegistersEvent()
        {
            var stateChanged = false;
            var callLog = new List<string>();
            var container = CreateContainer(callLog);

            var sm = new StateMachineBuilder<TestStateId, TestMachineEventId>(container)
                .AddEvent(TestMachineEventId.MachineEvent1, e =>
                {
                    stateChanged = true;
                    e.ChangeState(TestStateId.State2);
                })
                .State<TrackingState1, TestEventId>(TestStateId.State1).End()
                .State<TrackingState2, TestEventId>(TestStateId.State2).End()
                .Build();

            sm.Start();

            // Machine-level controller is available via StateControllers enumerable, not per-state lookup
            var machineController = sm.StateControllerProvider.StateControllers
                .Select(b => b.Instance)
                .OfType<IStateController<TestMachineEventId>>()
                .First();
            machineController.TriggerEvent(TestMachineEventId.MachineEvent1);

            Assert.IsTrue(stateChanged);
        }

        [TestMethod]
        public void Test_MachineLevelBuilder_FluentChain()
        {
            var sm = new StateMachineBuilder<TestStateId, TestMachineEventId>()
                .AddEvent(TestMachineEventId.MachineEvent1, e => { })
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            Assert.IsNotNull(sm);
        }
    }
}
