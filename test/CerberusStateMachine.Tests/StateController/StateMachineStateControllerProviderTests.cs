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

        private List<BindInfo> CreateStateControllers(
            params (TestStateId stateId, Type eventIdType, object instance)[] entries)
        {
            var list = new List<BindInfo>();
            foreach (var (stateId, eventIdType, instance) in entries)
            {
                list.Add(new StateControllerBindInfo<TestStateId>(
                    stateId, instance, new[] { typeof(IStateController<>).MakeGenericType(eventIdType) }));
            }
            return list;
        }

        private static BindInfo CreateSubStateBindInfo(TestSubStateId subStateId, Type eventIdType, object instance)
        {
            return new StateControllerBindInfo<TestSubStateId>(
                subStateId, instance, new[] { typeof(IStateController<>).MakeGenericType(eventIdType) });
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
            var stateControllers = new List<BindInfo>();
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
            var stateControllers = new List<BindInfo>
            {
                new StateControllerBindInfo<TestStateId>(
                    TestStateId.State1, controller, controller.GetType().GetInterfaces())
            };
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

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
        [TestMethod]
        public void Test_Constructor_NullStateControllers_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new StateMachineStateControllerProvider<TestStateId>(null, null));
        }

        [TestMethod]
        public void Test_GetStateController_ContractTypesListSubStateInterfaceFirst_StillResolvesEventIdType()
        {
            var controller = Substitute.For<IStateController<TestEventId, TestSubStateId>>();
            var stateControllers = new List<BindInfo>
            {
                new StateControllerBindInfo<TestStateId>(TestStateId.State1, controller,
                    new[] { typeof(IStateController<TestEventId, TestSubStateId>), typeof(IStateController<TestEventId>) })
            };
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var result = provider.GetStateController<IStateController<TestEventId, TestSubStateId>, TestStateId, TestEventId>(
                TestStateId.State1);

            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Test_GetStateController_SubStateController_RetrievedBySubStateId()
        {
            var parentController = CreateMockController();
            var subStateController = Substitute.For<IStateController<TestSubEventId>>();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), parentController));
            stateControllers.Add(CreateSubStateBindInfo(TestSubStateId.SubState1, typeof(TestSubEventId), subStateController));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var result = provider.GetStateController<IStateController<TestSubEventId>, TestSubStateId, TestSubEventId>(
                TestSubStateId.SubState1);

            Assert.AreSame(subStateController, result);
        }

        [TestMethod]
        public void Test_GetStateController_ParentAndSubStateShareEventIdType_EachRetrievable()
        {
            var parentController = CreateMockController();
            var subStateController = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), parentController));
            stateControllers.Add(CreateSubStateBindInfo(TestSubStateId.SubState1, typeof(TestEventId), subStateController));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var parentResult = provider.GetStateController<IStateController<TestEventId>, TestStateId, TestEventId>(
                TestStateId.State1);
            var subStateResult = provider.GetStateController<IStateController<TestEventId>, TestSubStateId, TestEventId>(
                TestSubStateId.SubState1);

            Assert.AreSame(parentController, parentResult);
            Assert.AreSame(subStateController, subStateResult);
        }

        [TestMethod]
        public void Test_GetStateController_SiblingSubStatesShareEventIdType_EachRetrievable()
        {
            var subState1Controller = CreateMockController();
            var subState2Controller = CreateMockController();
            var stateControllers = new List<BindInfo>
            {
                CreateSubStateBindInfo(TestSubStateId.SubState1, typeof(TestEventId), subState1Controller),
                CreateSubStateBindInfo(TestSubStateId.SubState2, typeof(TestEventId), subState2Controller)
            };
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var subState1Result = provider.GetStateController<IStateController<TestEventId>, TestSubStateId, TestEventId>(
                TestSubStateId.SubState1);
            var subState2Result = provider.GetStateController<IStateController<TestEventId>, TestSubStateId, TestEventId>(
                TestSubStateId.SubState2);

            Assert.AreSame(subState1Controller, subState1Result);
            Assert.AreSame(subState2Controller, subState2Result);
        }

        [TestMethod]
        public void Test_Constructor_SameStateIdAndEventIdTypeRegisteredTwice_DoesNotThrowAndEnumeratesBoth()
        {
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), CreateMockController()),
                (TestStateId.State1, typeof(TestEventId), CreateMockController()));

            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            Assert.AreEqual(2, provider.StateControllers.Count());
            Assert.AreEqual(2, provider.GetStateControllers<TestStateId>().Count());
        }

        [TestMethod]
        public void Test_GetStateController_SameStateIdAndEventIdTypeRegisteredTwice_ThrowsArgumentException()
        {
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), CreateMockController()),
                (TestStateId.State1, typeof(TestEventId), CreateMockController()));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            Assert.ThrowsExactly<ArgumentException>(() =>
                provider.GetStateController<IStateController<TestEventId>, TestStateId, TestEventId>(
                    TestStateId.State1));
        }

        [TestMethod]
        public void Test_GetStateControllers_SubStateIdType_ReturnsOnlySubStateControllers()
        {
            var stateControllers = CreateStateControllers(
                (TestStateId.State1, typeof(TestEventId), CreateMockController()));
            stateControllers.Add(CreateSubStateBindInfo(TestSubStateId.SubState1, typeof(TestEventId), CreateMockController()));
            stateControllers.Add(CreateSubStateBindInfo(TestSubStateId.SubState2, typeof(TestEventId), CreateMockController()));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var subStateResults = provider.GetStateControllers<TestSubStateId>().ToList();
            var stateResults = provider.GetStateControllers<TestStateId>().ToList();

            Assert.AreEqual(2, subStateResults.Count);
            CollectionAssert.AreEquivalent(new[] { TestSubStateId.SubState1, TestSubStateId.SubState2 }, subStateResults.Select(r => r.State).ToList());
            Assert.AreEqual(1, stateResults.Count);
        }

        [TestMethod]
        public void Test_StateControllers_PreservesRegistrationOrder()
        {
            var controller1 = CreateMockController();
            var controller2 = CreateMockController();
            var stateControllers = CreateStateControllers(
                (TestStateId.State2, typeof(TestEventId), controller1),
                (TestStateId.State1, typeof(TestEventId), controller2));
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            var instances = provider.StateControllers.Select(b => b.Instance).ToList();

            CollectionAssert.AreEqual(new object[] { controller1, controller2 }, instances);
        }

        [TestMethod]
        public void Test_Constructor_BindInfoWithoutStateControllerContract_IsEnumeratedButNotIndexed()
        {
            var plainBindInfo = new BindInfo(new object(), new[] { typeof(IDisposable) });
            var stateControllers = new List<BindInfo> { plainBindInfo };
            var provider = new StateMachineStateControllerProvider<TestStateId>(stateControllers, null);

            Assert.AreEqual(1, provider.StateControllers.Count());
            Assert.ThrowsExactly<ArgumentException>(() =>
                provider.GetStateController<IStateController<TestEventId>, TestStateId, TestEventId>(
                    TestStateId.State1));
        }

    }
}
