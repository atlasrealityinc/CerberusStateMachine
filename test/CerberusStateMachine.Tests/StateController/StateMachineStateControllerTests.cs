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
    public class StateMachineStateControllerTests
    {
        /// <summary>
        /// A runner that handles TestEventId events, logging each trigger as "name:eventId".
        /// </summary>
        private static IStateRunner CreateRunner(List<string> callLog, string name, bool handles, IStateRunner activeSubStateRunner = null)
        {
            var runner = Substitute.For<IStateRunner, IEventTrigger<TestEventId>>();
            runner.ActiveSubStateRunner.Returns(activeSubStateRunner);
            ((IEventTrigger<TestEventId>)runner).TriggerEvent(Arg.Any<TestEventId>()).Returns(ci =>
            {
                callLog.Add($"{name}:{ci.Arg<TestEventId>()}");
                return handles;
            });
            return runner;
        }

        /// <summary>
        /// A runner whose event id type is TestSubEventId, so it must be skipped when TestEventId events are triggered.
        /// </summary>
        private static IStateRunner CreateRunnerWithOtherEventType(List<string> callLog, string name, IStateRunner activeSubStateRunner = null)
        {
            var runner = Substitute.For<IStateRunner, IEventTrigger<TestSubEventId>>();
            runner.ActiveSubStateRunner.Returns(activeSubStateRunner);
            ((IEventTrigger<TestSubEventId>)runner).TriggerEvent(Arg.Any<TestSubEventId>()).Returns(ci =>
            {
                callLog.Add($"{name}:{ci.Arg<TestSubEventId>()}");
                return true;
            });
            return runner;
        }

        /// <summary>
        /// Hand written fake used where a handler needs to mutate the hierarchy while it is being walked,
        /// which is awkward to express by reconfiguring substitutes from inside their own callbacks.
        /// </summary>
        private class FakeRunner : IStateRunner, IEventTrigger<TestEventId>
        {
            public IStateRunner ActiveSubStateRunner { get; set; }
            public Func<TestEventId, bool> OnTriggerEvent { get; set; }

            public bool TriggerEvent(TestEventId eventId)
            {
                return OnTriggerEvent?.Invoke(eventId) ?? false;
            }
        }

        [TestMethod]
        public void Test_Constructor_NullActiveStateRunnerGetter_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new StateMachineStateController(null, null));
        }

        [TestMethod]
        public void Test_Constructor_NullStateMachineRunner_IsAllowed()
        {
            var controller = new StateMachineStateController(() => null, null);

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void Test_TriggerEvent_TriggersFromInnermostToOutermostState()
        {
            var callLog = new List<string>();
            var grandchild = CreateRunner(callLog, "Grandchild", true);
            var child = CreateRunner(callLog, "Child", true, grandchild);
            var root = CreateRunner(callLog, "Root", true, child);
            var controller = new StateMachineStateController(() => root, null);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Grandchild:Event1", "Child:Event1", "Root:Event1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_TriggersStateMachineRunnerLast()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", true);
            var root = CreateRunner(callLog, "Root", true, child);
            var machine = CreateRunner(callLog, "Machine", true);
            var controller = new StateMachineStateController(() => root, machine);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Child:Event1", "Root:Event1", "Machine:Event1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_OffersEventToEveryLevel_EvenWhenInnerLevelHandlesIt()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", true);
            var root = CreateRunner(callLog, "Root", false, child);
            var machine = CreateRunner(callLog, "Machine", false);
            var controller = new StateMachineStateController(() => root, machine);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Child:Event1", "Root:Event1", "Machine:Event1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_PassesEventIdThrough()
        {
            var callLog = new List<string>();
            var root = CreateRunner(callLog, "Root", true);
            var controller = new StateMachineStateController(() => root, null);

            controller.TriggerEvent(TestEventId.Event2);

            CollectionAssert.AreEqual(new[] { "Root:Event2" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_SkipsLevelsWithDifferentEventIdType()
        {
            var callLog = new List<string>();
            var grandchild = CreateRunner(callLog, "Grandchild", true);
            var child = CreateRunnerWithOtherEventType(callLog, "Child", grandchild);
            var root = CreateRunner(callLog, "Root", true, child);
            var controller = new StateMachineStateController(() => root, null);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Grandchild:Event1", "Root:Event1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_EventIdTypeHandledOnlyByMiddleLevel_TriggersOnlyThatLevel()
        {
            var callLog = new List<string>();
            var grandchild = CreateRunner(callLog, "Grandchild", true);
            var child = CreateRunnerWithOtherEventType(callLog, "Child", grandchild);
            var root = CreateRunner(callLog, "Root", true, child);
            var controller = new StateMachineStateController(() => root, null);

            var handled = controller.TriggerEvent(TestSubEventId.SubEvent1);

            Assert.IsTrue(handled);
            CollectionAssert.AreEqual(new[] { "Child:SubEvent1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_NoLevelHandlesEventIdType_ReturnsFalseWithoutTriggering()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", true);
            var root = CreateRunner(callLog, "Root", true, child);
            var machine = CreateRunner(callLog, "Machine", true);
            var controller = new StateMachineStateController(() => root, machine);

            var handled = controller.TriggerEvent(TestMachineEventId.MachineEvent1);

            Assert.IsFalse(handled);
            Assert.AreEqual(0, callLog.Count);
        }

        [TestMethod]
        public void Test_TriggerEvent_OuterLevelHandles_ReturnsTrue()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", false);
            var root = CreateRunner(callLog, "Root", true, child);
            var controller = new StateMachineStateController(() => root, null);

            Assert.IsTrue(controller.TriggerEvent(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_TriggerEvent_InnerLevelHandles_ReturnsTrue()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", true);
            var root = CreateRunner(callLog, "Root", false, child);
            var controller = new StateMachineStateController(() => root, null);

            Assert.IsTrue(controller.TriggerEvent(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_TriggerEvent_OnlyStateMachineRunnerHandles_ReturnsTrue()
        {
            var callLog = new List<string>();
            var root = CreateRunner(callLog, "Root", false);
            var machine = CreateRunner(callLog, "Machine", true);
            var controller = new StateMachineStateController(() => root, machine);

            Assert.IsTrue(controller.TriggerEvent(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_TriggerEvent_NoLevelHandles_ReturnsFalse()
        {
            var callLog = new List<string>();
            var child = CreateRunner(callLog, "Child", false);
            var root = CreateRunner(callLog, "Root", false, child);
            var machine = CreateRunner(callLog, "Machine", false);
            var controller = new StateMachineStateController(() => root, machine);

            Assert.IsFalse(controller.TriggerEvent(TestEventId.Event1));
            Assert.AreEqual(3, callLog.Count);
        }

        [TestMethod]
        public void Test_TriggerEvent_NoActiveState_TriggersOnlyStateMachineRunner()
        {
            var callLog = new List<string>();
            var machine = CreateRunner(callLog, "Machine", true);
            var controller = new StateMachineStateController(() => null, machine);

            var handled = controller.TriggerEvent(TestEventId.Event1);

            Assert.IsTrue(handled);
            CollectionAssert.AreEqual(new[] { "Machine:Event1" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_NoActiveStateAndNoStateMachineRunner_ReturnsFalse()
        {
            var controller = new StateMachineStateController(() => null, null);

            Assert.IsFalse(controller.TriggerEvent(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_TriggerEvent_ReadsActiveStateOnEveryCall()
        {
            var callLog = new List<string>();
            var first = CreateRunner(callLog, "First", true);
            var second = CreateRunner(callLog, "Second", true);
            var active = first;
            var controller = new StateMachineStateController(() => active, null);

            controller.TriggerEvent(TestEventId.Event1);
            active = second;
            controller.TriggerEvent(TestEventId.Event2);

            CollectionAssert.AreEqual(new[] { "First:Event1", "Second:Event2" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_HierarchyIsCapturedBeforeTriggering_SubStateEnteredByHandlerIsNotTriggered()
        {
            var callLog = new List<string>();
            var root = new FakeRunner();
            var replacement = new FakeRunner { OnTriggerEvent = _ => { callLog.Add("Replacement"); return true; } };
            var child = new FakeRunner
            {
                OnTriggerEvent = _ =>
                {
                    callLog.Add("Child");
                    //Simulates the child's handler transitioning the parent to a different sub-state
                    root.ActiveSubStateRunner = replacement;
                    return true;
                }
            };
            root.ActiveSubStateRunner = child;
            root.OnTriggerEvent = _ => { callLog.Add("Root"); return true; };
            var controller = new StateMachineStateController(() => root, null);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Child", "Root" }, callLog);
        }

        [TestMethod]
        public void Test_TriggerEvent_HierarchyIsCapturedBeforeTriggering_StateEnteredByHandlerIsNotTriggered()
        {
            var callLog = new List<string>();
            var replacement = new FakeRunner { OnTriggerEvent = _ => { callLog.Add("Replacement"); return true; } };
            FakeRunner active = null;
            var root = new FakeRunner
            {
                OnTriggerEvent = _ =>
                {
                    callLog.Add("Root");
                    //Simulates the handler transitioning the machine to a different top-level state
                    active = replacement;
                    return true;
                }
            };
            active = root;
            var controller = new StateMachineStateController(() => active, null);

            controller.TriggerEvent(TestEventId.Event1);

            CollectionAssert.AreEqual(new[] { "Root" }, callLog);
        }
    }
}
