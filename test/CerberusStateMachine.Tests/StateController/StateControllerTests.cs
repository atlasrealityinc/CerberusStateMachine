using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Runner;
using Cerberus.StateController;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests.StateController
{
    [TestClass]
    public class StateControllerTests
    {
        [TestMethod]
        public void Test_TriggerEvent_DelegatesToCallback()
        {
            var callbackInvoked = false;
            var controller = new StateController<TestEventId>(eventId =>
            {
                callbackInvoked = true;
                return true;
            });

            controller.TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(callbackInvoked);
        }

        [TestMethod]
        public void Test_TriggerEvent_CallbackReturnsTrue_ReturnsTrue()
        {
            var controller = new StateController<TestEventId>(eventId => true);

            var result = controller.TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Test_TriggerEvent_CallbackReturnsFalse_ReturnsFalse()
        {
            var controller = new StateController<TestEventId>(eventId => false);

            var result = controller.TriggerEvent(TestEventId.Event1);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Test_CurrentSubState_ReturnsActiveSubStateId()
        {
            var callLog = new List<string>();
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingSubState1>().Returns(_ => new TrackingSubState1(callLog));
            container.Resolve<TrackingSubState2>().Returns(_ => new TrackingSubState2(callLog));
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
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

            var bindInfo = runner.BindInfo;
            var controller = (IStateController<TestEventId, TestSubStateId>)bindInfo[0].Instance;

            Assert.AreEqual(TestSubStateId.SubState1, controller.CurrentSubState);
        }

        [TestMethod]
        public void Test_CurrentSubState_AfterTransition_ReturnsNewSubStateId()
        {
            var callLog = new List<string>();
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(_ => new TrackingState1(callLog));
            container.Resolve<TrackingSubState1>().Returns(_ => new TrackingSubState1(callLog));
            container.Resolve<TrackingSubState2>().Returns(_ => new TrackingSubState2(callLog));
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
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

            runner.ChangeState(TestSubStateId.SubState2);

            var controller = (IStateController<TestEventId, TestSubStateId>)runner.BindInfo[0].Instance;
            Assert.AreEqual(TestSubStateId.SubState2, controller.CurrentSubState);
        }
    }
}
