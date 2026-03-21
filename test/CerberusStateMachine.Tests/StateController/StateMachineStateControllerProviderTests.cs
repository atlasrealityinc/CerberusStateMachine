using Cerberus.Runner;
using Cerberus.StateController;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus.Tests.StateController
{
    [TestClass]
    public class StateMachineStateControllerProviderTests
    {
        private IStateController<TestEventId> CreateMockController()
        {
            return Substitute.For<IStateController<TestEventId>>();
        }

        private Dictionary<Enum, Dictionary<Type, BindInfo>> CreateStateControllers(
            params (TestStateId stateId, Type eventIdType, object instance)[] entries)
        {
            var dict = new Dictionary<Enum, Dictionary<Type, BindInfo>>();
            foreach (var (stateId, eventIdType, instance) in entries)
            {
                if (!dict.ContainsKey(stateId))
                    dict[stateId] = new Dictionary<Type, BindInfo>();
                dict[stateId][eventIdType] = new StateControllerBindInfo<TestStateId>(
                    stateId, instance, instance.GetType().GetInterfaces());
            }
            return dict;
        }

        [TestMethod]
        public void Test_GetStateController_ReturnsCorrectController()
        {
            var controller = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var result = provider.GetStateController<IStateController<TestEventId>, TestStateId, TestEventId>(
                TestStateId.State1);

            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Test_GetStateController_UnknownStateId_ThrowsArgumentException()
        {
            var stateControllers = new Dictionary<Enum, Dictionary<Type, BindInfo>>();
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            Assert.ThrowsException<ArgumentException>(() =>
                provider.GetStateController<IStateController<TestEventId>, TestStateId, TestEventId>(
                    TestStateId.State1));
        }

        [TestMethod]
        public void Test_GetStateController_WrongEventType_ThrowsArgumentException()
        {
            var controller = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            Assert.ThrowsException<ArgumentException>(() =>
                provider.GetStateController<IStateController<TestMachineEventId>, TestStateId, TestMachineEventId>(
                    TestStateId.State1));
        }

        [TestMethod]
        public void Test_GetStateController_WrongControllerType_ThrowsArgumentException()
        {
            // Create a controller that doesn't match the expected sub-state controller type
            var controller = CreateMockController();
            var dict = new Dictionary<Enum, Dictionary<Type, BindInfo>>
            {
                {
                    TestStateId.State1, new Dictionary<Type, BindInfo>
                    {
                        { typeof(TestEventId), new StateControllerBindInfo<TestStateId>(
                            TestStateId.State1, controller, controller.GetType().GetInterfaces()) }
                    }
                }
            };
            var provider = new StateMachineStateControllerProvider<TestStateId>(dict, null);

            Assert.ThrowsException<ArgumentException>(() =>
                provider.GetStateController<IStateController<TestEventId, TestSubStateId>, TestStateId, TestEventId>(
                    TestStateId.State1));
        }

        [TestMethod]
        public void Test_GetStateControllers_ReturnsMatchingControllers()
        {
            var controller1 = CreateMockController();
            var controller2 = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller1),
                (TestStateId.State2, typeof(TestEventId), controller2));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var results = provider.GetStateControllers<TestStateId>().ToList();

            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public void Test_StateControllers_IncludesMachineRunner_WhenPresent()
        {
            var controller = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller));
            var machineRunner = Substitute.For<StateMachineRunner<TestStateId>>();
            var machineBindInfo = new BindInfo(new object(), new[] { typeof(IStateController<TestMachineEventId>) });
            machineRunner.BindInfo.Returns(machineBindInfo);
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, machineRunner);

            var allControllers = provider.StateControllers.ToList();

            Assert.AreEqual(2, allControllers.Count);
        }

        [TestMethod]
        public void Test_StateControllers_ExcludesMachineRunner_WhenNull()
        {
            var controller = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var allControllers = provider.StateControllers.ToList();

            Assert.AreEqual(1, allControllers.Count);
        }

        [TestMethod]
        public void Test_StateControllers_ExcludesMachineRunner_WhenBindInfoNull()
        {
            var controller = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), controller));
            var machineRunner = Substitute.For<StateMachineRunner<TestStateId>>();
            machineRunner.BindInfo.Returns((BindInfo)null);
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, machineRunner);

            var allControllers = provider.StateControllers.ToList();

            Assert.AreEqual(1, allControllers.Count);
        }
    }
}
