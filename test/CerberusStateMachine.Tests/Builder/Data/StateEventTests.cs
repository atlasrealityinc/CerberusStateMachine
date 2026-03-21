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
    public class StateEventTests
    {
        private StateRunner<TrackingState1, TestStateId, TestEventId> CreateRunner(
            List<string> callLog,
            IStateChanger<TestStateId> stateChanger = null)
        {
            var container = Substitute.For<IStateMachineContainer>();
            container.Resolve<TrackingState1>().Returns(new TrackingState1(callLog));
            container.Resolve(Arg.Any<Type>()).Returns(ci => Activator.CreateInstance((Type)ci[0]));
            var handlerTypes = new Dictionary<Type, List<Type>>
            {
                { typeof(IState), new List<Type> { typeof(Cerberus.StateHandlers.EnterAndExitStateHandler) } }
            };
            var stateData = new StateData<TrackingState1, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);

            return new StateRunner<TrackingState1, TestStateId, TestEventId>(
                stateData,
                stateChanger ?? Substitute.For<IStateChanger<TestStateId>>());
        }

        [TestMethod]
        public void Test_ChangeState_TriggersStateChangerChangeState()
        {
            var callLog = new List<string>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var runner = CreateRunner(callLog, stateChanger);
            runner.Start(TestStateId.State1);
            var stateEvent = new StateEvent<TrackingState1, TestStateId>(runner, TestStateId.State1);

            stateEvent.ChangeState(TestStateId.State2);

            stateChanger.Received(1).ChangeState(TestStateId.State2);
        }

        [TestMethod]
        public void Test_PreviousStateId_ReturnsConstructorValue()
        {
            var callLog = new List<string>();
            var runner = CreateRunner(callLog);
            runner.Start(TestStateId.State2);
            var stateEvent = new StateEvent<TrackingState1, TestStateId>(runner, TestStateId.State2);

            Assert.AreEqual(TestStateId.State2, stateEvent.PreviousStateId);
        }

        [TestMethod]
        public void Test_StateInstance_ReturnsRunnerActiveInstance()
        {
            var callLog = new List<string>();
            var runner = CreateRunner(callLog);
            runner.Start(TestStateId.State1);
            var stateEvent = new StateEvent<TrackingState1, TestStateId>(runner, TestStateId.State1);

            var instance = stateEvent.StateInstance;

            Assert.IsNotNull(instance);
            Assert.IsInstanceOfType(instance, typeof(TrackingState1));
        }
    }
}
