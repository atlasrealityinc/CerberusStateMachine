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
        private IStateMachineContainer CreateContainer(
            List<string> callLog,
            Dictionary<Type, object> overrides = null)
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingState2>().Returns(_ => new TrackingState2(callLog));
            container.Resolve<TrackingState3>().Returns(_ => new TrackingState3(callLog));
            container.Resolve<CustomTrackingState>().Returns(_ => new CustomTrackingState(callLog));
            container.Resolve<NoOpState>().Returns(_ => new NoOpState());
            container.Resolve(Arg.Any<Type>()).Returns(ci =>
            {
                var type = (Type)ci[0];
                if (overrides != null && overrides.TryGetValue(type, out var instance))
                    return instance;
                return Activator.CreateInstance(type);
            });
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
        public void Test_AddStateHandler_HandlersWrapStateOnEnterAndOnExit()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(TrackingStateHandlerA), new TrackingStateHandlerA(callLog) },
                { typeof(TrackingStateHandlerB), new TrackingStateHandlerB(callLog) }
            });

            var sm = new StateMachineBuilder<TestStateId>(container)
                .AddStateHandler<IState, TrackingStateHandlerA>()
                .AddStateHandler<IState, TrackingStateHandlerB>()
                .State<TrackingState1, TestEventId>(TestStateId.State1)
                    .AddEvent(TestEventId.Event1, e => e.ChangeState(TestStateId.State2))
                    .End()
                .State<TrackingState2, TestEventId>(TestStateId.State2).End()
                .Build();

            sm.Start();
            sm.StateController.TriggerEvent(TestEventId.Event1);

            // Handlers enter before the state's OnEnter() and exit after its OnExit(), in reverse order
            CollectionAssert.AreEqual(new[]
            {
                "HandlerA:Enter:TrackingState1",
                "HandlerB:Enter:TrackingState1",
                "State1:Enter",
                "State1:Exit",
                "HandlerB:Exit:TrackingState1",
                "HandlerA:Exit:TrackingState1",
                "HandlerA:Enter:TrackingState2",
                "HandlerB:Enter:TrackingState2",
                "State2:Enter"
            }, callLog);
        }

        [TestMethod]
        public void Test_AddStateHandler_NarrowerHandlerRegisteredAfterIStateHandler_StillWrapsState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(TrackingStateHandlerA), new TrackingStateHandlerA(callLog) },
                { typeof(CustomStateHandler), new CustomStateHandler(callLog) }
            });

            // IState handler first, narrower handler second: Build() appends the built-in enter/exit handler to the
            // IState bucket, which must not put it ahead of the handler registered for ICustomState
            var sm = new StateMachineBuilder<TestStateId>(container)
                .AddStateHandler<IState, TrackingStateHandlerA>()
                .AddStateHandler<ICustomState, CustomStateHandler>()
                .State<CustomTrackingState, TestEventId>(TestStateId.State1)
                    .AddEvent(TestEventId.Event1, e => e.ChangeState(TestStateId.State2))
                    .End()
                .State<TrackingState2, TestEventId>(TestStateId.State2).End()
                .Build();

            sm.Start();
            sm.StateController.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[]
            {
                "HandlerA:Enter:CustomTrackingState",
                "CustomHandler:Enter:CustomTrackingState",
                "CustomState:Enter",
                "CustomState:Exit",
                "CustomHandler:Exit:CustomTrackingState",
                "HandlerA:Exit:CustomTrackingState",
                "HandlerA:Enter:TrackingState2",
                "State2:Enter"
            }, callLog);
        }

        [TestMethod]
        public void Test_Build_CalledTwice_SecondMachineCallsOnEnterOnce()
        {
            var callLog = new List<string>();
            var builder = new StateMachineBuilder<TestStateId>(CreateContainer(callLog))
                .State<TrackingState1, TestEventId>(TestStateId.State1).End();
            builder.Build();
            var sm = builder.Build();

            sm.Start();

            Assert.AreEqual(1, callLog.Count(c => c == "State1:Enter"));
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

        [TestMethod]
        public void Test_MachineLevelBuilder_AddEvent_DuplicateEventId_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId, TestMachineEventId>()
                .AddEvent(TestMachineEventId.MachineEvent1, e => { });

            Assert.ThrowsExactly<ArgumentException>(() => builder.AddEvent(TestMachineEventId.MachineEvent1, e => { }));
        }

        [TestMethod]
        public void Test_Builders_NonEnumEventType_RegistersAndBuilds()
        {
            var sm = new StateMachineBuilder<TestStateId, string>()
                .AddEvent("machine", e => { })
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent("state", e => { })
                    .End()
                .State<NoOpState, string, TestSubStateId>(TestStateId.State2)
                    .AddEvent("parent", e => { })
                    .State<NoOpState, string>(TestSubStateId.SubState1)
                        .AddEvent("sub", e => { })
                        .End()
                    .End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_MachineLevelBuilder_AddEvent_Predicate_RegistersEvent()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var received = default(TestFlagEventId);

            var sm = new StateMachineBuilder<TestStateId, TestFlagEventId>(container)
                .AddEvent(e => e.IsOn, e =>
                {
                    received = e.EventId;
                    e.ChangeState(TestStateId.State2);
                })
                .State<TrackingState1, TestEventId>(TestStateId.State1).End()
                .State<TrackingState2, TestEventId>(TestStateId.State2).End()
                .Build();
            sm.Start();

            var triggered = new TestFlagEventId(true, 3);
            var handled = sm.StateController.TriggerEvent(triggered);

            Assert.IsTrue(handled);
            Assert.AreEqual(triggered, received);
            Assert.IsTrue(callLog.Contains("State2:Enter"));
        }

        [TestMethod]
        public void Test_MachineLevelBuilder_AddEvent_Predicate_FluentChain()
        {
            var sm = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(e => e.IsOn, e => { })
                .AddEvent(TestFlagEventId.Off, e => { })
                .State<NoOpState, TestEventId>(TestStateId.State1).End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_MachineLevelBuilder_AddEvent_Predicate_NullPredicate_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId, TestFlagEventId>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));

            Assert.AreEqual("predicate", ex.ParamName);
        }
    }
}
