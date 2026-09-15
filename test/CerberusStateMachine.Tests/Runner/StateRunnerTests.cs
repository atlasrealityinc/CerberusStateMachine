using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Runner;
using Cerberus.StateHandlers;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Tests.Runner
{
    [TestClass]
    public class StateRunnerTests
    {
        private IStateMachineContainer CreateContainer(
            List<string> callLog,
            Dictionary<Type, object> overrides = null)
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingState2>().Returns(_ => new TrackingState2(callLog));
            container.Resolve<NoOpState>().Returns(_ => new NoOpState());
            container.Resolve<TrackingSubState1>().Returns(_ => new TrackingSubState1(callLog));
            container.Resolve<TrackingSubState2>().Returns(_ => new TrackingSubState2(callLog));
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
        public void Test_Start_ResolvesStateInstance()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            runner.Start(TestStateId.State1);

            Assert.IsNotNull(runner.ActiveInstance);
            Assert.IsInstanceOfType(runner.ActiveInstance, typeof(TrackingState1));
        }

        [TestMethod]
        public void Test_Start_CallsHandlersOnEnterState()
        {
            var callLog = new List<string>();
            var handler = new TrackingStateHandlerA(callLog);
            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(TrackingStateHandlerA), handler }
            });
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(TrackingStateHandlerA) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            runner.Start(TestStateId.State1);

            Assert.IsTrue(callLog.Any(c => c.Contains("HandlerA:Enter:TrackingState1")));
        }

        [TestMethod]
        public void Test_Start_WhenAlreadyStarted_IsNoOp()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            runner.Start(TestStateId.State1);
            runner.Start(TestStateId.State1);

            Assert.AreEqual(1, callLog.Count(c => c == "State1:Enter"));
        }

        [TestMethod]
        public void Test_Start_WhenHandlerCausesStateChange_StopsRemainingHandlers()
        {
            var callLog = new List<string>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            // StateChangingHandler triggers a state change, which will cause Stop() and null ActiveInstance
            var changingHandler = new StateChangingHandler(state =>
            {
                callLog.Add("ChangingHandler:Enter");
                // Simulate what happens when a state change occurs during handler processing
                // The state changer would call Stop() on this runner, nulling ActiveInstance
                stateChanger.When(x => x.ChangeState(Arg.Any<TestStateId>())).Do(_ => { });
                stateChanger.ChangeState(TestStateId.State2);
            });
            var trackingHandler = new TrackingStateHandlerB(callLog);
            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(StateChangingHandler), changingHandler },
                { typeof(TrackingStateHandlerB), trackingHandler }
            });
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(StateChangingHandler), typeof(TrackingStateHandlerB) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            // To properly test this, we need the state changer to call Stop() on the runner
            stateChanger.When(x => x.ChangeState(Arg.Any<TestStateId>())).Do(_ => runner.Stop());

            runner.Start(TestStateId.State1);

            // The changing handler fires first, triggers state change which calls Stop()
            // Stop() nulls ActiveInstance, so the next handler (TrackingStateHandlerB) should NOT fire
            Assert.IsTrue(callLog.Contains("ChangingHandler:Enter"));
            Assert.IsFalse(callLog.Any(c => c.Contains("HandlerB:Enter")));
        }

        [TestMethod]
        public void Test_Stop_CallsHandlersOnExitState_InReverseOrder()
        {
            var callLog = new List<string>();
            var handlerA = new TrackingStateHandlerA(callLog);
            var handlerB = new TrackingStateHandlerB(callLog);
            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(TrackingStateHandlerA), handlerA },
                { typeof(TrackingStateHandlerB), handlerB }
            });
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(TrackingStateHandlerA), typeof(TrackingStateHandlerB) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);
            callLog.Clear();

            runner.Stop();

            // Stack pop order: B exits first (LIFO), then A
            Assert.AreEqual("HandlerB:Exit:TrackingState1", callLog[0]);
            Assert.AreEqual("HandlerA:Exit:TrackingState1", callLog[1]);
        }

        [TestMethod]
        public void Test_Stop_WhenNotStarted_IsNoOp()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            runner.Stop();

            Assert.AreEqual(0, callLog.Count);
        }

        [TestMethod]
        public void Test_Stop_SetsActiveInstanceToNull()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);
            Assert.IsNotNull(runner.ActiveInstance);

            runner.Stop();

            Assert.IsNull(runner.ActiveInstance);
        }

        [TestMethod]
        public void Test_StateChangerChangeState_DelegatesToStateChanger()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            runner.StateChangerChangeState(TestStateId.State2);

            stateChanger.Received(1).ChangeState(TestStateId.State2);
        }

        [TestMethod]
        public void Test_TriggerEvent_RegisteredEvent_ReturnsTrue()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            stateData.AddEvent(TestEventId.Event1, e => { });
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);

            var result = runner.TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Test_TriggerEvent_UnregisteredEvent_ReturnsFalse()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);

            var result = runner.TriggerEvent(TestEventId.Event1);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Test_TriggerEvent_WhenNotActive_ReturnsFalse()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            stateData.AddEvent(TestEventId.Event1, e => { });
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            var result = runner.TriggerEvent(TestEventId.Event1);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Test_TriggerEvent_InvokesEventAction()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var eventInvoked = false;
            stateData.AddEvent(TestEventId.Event1, e => { eventInvoked = true; });
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);

            runner.TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(eventInvoked);
        }

        [TestMethod]
        public void Test_CreateStateController_ReturnsStateController()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, stateChanger);

            var bindInfo = runner.BindInfo;

            Assert.IsNotNull(bindInfo);
            Assert.AreEqual(1, bindInfo.Length);
            Assert.IsTrue(bindInfo[0].Instance is IStateController<TestEventId>);
        }

        [TestMethod]
        public void Test_SubState_Start_StartsDefaultSubState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var subStateData = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, subStateData);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);

            runner.Start(TestStateId.State1);

            Assert.IsNotNull(runner.ActiveSubState);
            Assert.IsTrue(callLog.Contains("SubState1:Enter"));
        }

        [TestMethod]
        public void Test_SubState_Stop_StopsActiveSubState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var subStateData = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, subStateData);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);
            callLog.Clear();

            runner.Stop();

            Assert.IsTrue(callLog.Contains("SubState1:Exit"));
            var subExitIdx = callLog.IndexOf("SubState1:Exit");
            var parentExitIdx = callLog.IndexOf("State1:Exit");
            Assert.IsTrue(subExitIdx < parentExitIdx, "Sub-state should exit before parent");
        }

        [TestMethod]
        public void Test_SubState_Stop_NullsActiveSubState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var subStateData = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, subStateData);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);

            runner.Stop();

            Assert.IsNull(runner.ActiveSubState);
        }

        [TestMethod]
        public void Test_SubState_ChangeState_TransitionsSubState()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            var sub2 = new StateData<TrackingSubState2, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState2, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);
            stateData.AddSubState(TestSubStateId.SubState2, sub2);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);
            callLog.Clear();

            runner.ChangeState(TestSubStateId.SubState2);

            CollectionAssert.AreEqual(new[] { "SubState1:Exit", "SubState2:Enter" }, callLog);
        }

        [TestMethod]
        public void Test_SubState_ChangeState_ToSameSubState_IsNoOp()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);
            runner.Start(TestStateId.State1);
            callLog.Clear();

            runner.ChangeState(TestSubStateId.SubState1);

            Assert.AreEqual(0, callLog.Count);
        }

        [TestMethod]
        public void Test_SubState_ChangeState_WhenParentExited_DoesNotStartNewSubState()
        {
            // This tests the critical bug fix: if stopping a sub-state causes the parent
            // to exit (ActiveInstance == null), the new sub-state must NOT be started.
            var callLog = new List<string>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            // Create an exit handler that triggers parent state change when SubState1 exits
            var exitHandler = new SubStateExitHandler(state =>
            {
                if (state is TrackingSubState1)
                {
                    callLog.Add("ExitHandler:TriggeringParentChange");
                }
            });

            var container = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(SubStateExitHandler), exitHandler }
            });
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(SubStateExitHandler), typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            var sub2 = new StateData<TrackingSubState2, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState2, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);
            stateData.AddSubState(TestSubStateId.SubState2, sub2);

            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);

            // When the exit handler fires during sub-state change, the stateChanger call
            // will trigger parent Stop(), which nulls ActiveInstance
            stateChanger.When(x => x.ChangeState(Arg.Any<TestStateId>())).Do(_ => runner.Stop());

            // Update exit handler to actually trigger the parent state change
            var exitHandler2 = new SubStateExitHandler(state =>
            {
                if (state is TrackingSubState1)
                {
                    callLog.Add("ExitHandler:TriggeringParentChange");
                    stateChanger.ChangeState(TestStateId.State2);
                }
            });

            // Rebuild with updated handler
            var container2 = CreateContainer(callLog, new Dictionary<Type, object>
            {
                { typeof(SubStateExitHandler), exitHandler2 }
            });
            var stateData2 = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container2, handlerTypes);
            var sub1b = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container2, handlerTypes);
            var sub2b = new StateData<TrackingSubState2, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState2, container2, handlerTypes);
            stateData2.AddSubState(TestSubStateId.SubState1, sub1b);
            stateData2.AddSubState(TestSubStateId.SubState2, sub2b);

            var runner2 = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData2, stateChanger);
            stateChanger.When(x => x.ChangeState(Arg.Any<TestStateId>())).Do(_ => runner2.Stop());

            runner2.Start(TestStateId.State1);
            callLog.Clear();

            // Trigger sub-state change from SubState1 to SubState2
            runner2.ChangeState(TestSubStateId.SubState2);

            // SubState2 should NOT have entered because the parent was stopped
            Assert.IsFalse(callLog.Contains("SubState2:Enter"),
                "SubState2 should not enter after parent state was exited");
        }

        [TestMethod]
        public void Test_SubState_BindInfo_IncludesSubStateControllers()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);

            var bindInfo = runner.BindInfo;

            // Should include parent controller + sub-state controller
            Assert.IsTrue(bindInfo.Length >= 2);
        }

        [TestMethod]
        public void Test_SubState_CreateStateController_ReturnsSubStateController()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            var sub1 = new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, sub1);
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);

            var bindInfo = runner.BindInfo;
            var parentBind = bindInfo[0];

            Assert.IsTrue(parentBind.Instance is IStateController<TestEventId, TestSubStateId>);
        }
        private StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId> CreateRunnerWithSubStates(List<string> callLog)
        {
            var container = CreateContainer(callLog);
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId, TestSubStateId>(
                TestStateId.State1, container, handlerTypes);
            stateData.AddSubState(TestSubStateId.SubState1, new StateData<TrackingSubState1, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1, container, handlerTypes));
            stateData.AddSubState(TestSubStateId.SubState2, new StateData<TrackingSubState2, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState2, container, handlerTypes));
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            return new StateRunner<TrackingState1, TestStateId, TestEventId, TestSubStateId>(stateData, stateChanger);
        }

        [TestMethod]
        public void Test_ActiveSubStateRunner_WithoutSubStates_ReturnsNull()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, Substitute.For<IStateChanger<TestStateId>>());
            runner.Start(TestStateId.State1);

            Assert.IsNull(runner.ActiveSubStateRunner);
        }

        [TestMethod]
        public void Test_ActiveSubStateRunner_WithSubStates_BeforeStart_ReturnsNull()
        {
            var runner = CreateRunnerWithSubStates(new List<string>());

            Assert.IsNull(runner.ActiveSubStateRunner);
        }

        [TestMethod]
        public void Test_ActiveSubStateRunner_WithSubStates_AfterStart_ReturnsActiveSubState()
        {
            var runner = CreateRunnerWithSubStates(new List<string>());

            runner.Start(TestStateId.State1);

            Assert.AreSame(runner.ActiveSubState, runner.ActiveSubStateRunner);
            Assert.AreEqual(TestSubStateId.SubState1, runner.ActiveSubState.StateId);
        }

        [TestMethod]
        public void Test_ActiveSubStateRunner_WithSubStates_AfterChangeState_ReturnsNewSubState()
        {
            var runner = CreateRunnerWithSubStates(new List<string>());
            runner.Start(TestStateId.State1);

            runner.ChangeState(TestSubStateId.SubState2);

            Assert.AreSame(runner.ActiveSubState, runner.ActiveSubStateRunner);
            Assert.AreEqual(TestSubStateId.SubState2, runner.ActiveSubState.StateId);
        }

        [TestMethod]
        public void Test_ActiveSubStateRunner_WithSubStates_AfterStop_ReturnsNull()
        {
            var runner = CreateRunnerWithSubStates(new List<string>());
            runner.Start(TestStateId.State1);

            runner.Stop();

            Assert.IsNull(runner.ActiveSubStateRunner);
        }

        [TestMethod]
        public void Test_Runner_ImplementsEventTriggerOnlyForItsEventIdType()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, Substitute.For<IStateChanger<TestStateId>>());

            Assert.IsInstanceOfType<IStateRunner>(runner);
            Assert.IsInstanceOfType<IEventTrigger<TestEventId>>(runner);
            Assert.IsNotInstanceOfType<IEventTrigger<TestSubEventId>>(runner);
        }

        [TestMethod]
        public void Test_EventTrigger_TriggerEvent_RegisteredEvent_InvokesActionAndReturnsTrue()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            var eventInvoked = false;
            stateData.AddEvent(TestEventId.Event1, e => eventInvoked = true);
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, Substitute.For<IStateChanger<TestStateId>>());
            runner.Start(TestStateId.State1);

            var handled = ((IEventTrigger<TestEventId>)runner).TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(handled);
            Assert.IsTrue(eventInvoked);
        }

        [TestMethod]
        public void Test_EventTrigger_TriggerEvent_WhenNotActive_ReturnsFalse()
        {
            var callLog = new List<string>();
            var container = CreateContainer(callLog);
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, new Dictionary<Type, List<Type>>());
            stateData.AddEvent(TestEventId.Event1, e => { });
            var runner = new StateRunner<TrackingState1, TestStateId, TestEventId>(stateData, Substitute.For<IStateChanger<TestStateId>>());

            var handled = ((IEventTrigger<TestEventId>)runner).TriggerEvent(TestEventId.Event1);

            Assert.IsFalse(handled);
        }

    }
}
