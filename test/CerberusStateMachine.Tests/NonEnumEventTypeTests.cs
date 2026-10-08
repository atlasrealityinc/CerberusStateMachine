using Cerberus.Builder;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests
{
    /// <summary>
    /// Event id types are not limited to enums. These tests drive string, int, struct, record, class and
    /// interface event ids end to end through the builder and the unified state controller.
    /// </summary>
    [TestClass]
    public class NonEnumEventTypeTests
    {
        #region string

        [TestMethod]
        public void Test_StringEvents_StateLevel_HandledAndTransitions()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => { log.Add("State1:go"); e.ChangeState(TestStateId.State2); })
                    .End()
                .State<NoOpState, string>(TestStateId.State2)
                    .AddEvent(TestStringEventId.Stop, e => log.Add("State2:stop"))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStringEventId.Stop), "Stop is not registered on State1");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go));
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Stop), "State2 should now be active");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStringEventId.Unregistered));
            CollectionAssert.AreEqual(new[] { "State1:go", "State2:stop" }, log);
        }

        [TestMethod]
        public void Test_StringEvents_EqualButDistinctInstance_IsHandled()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            //A new string with the same characters is a different reference but an equal key
            var distinctInstance = new string(TestStringEventId.Go.ToCharArray());
            Assert.IsFalse(ReferenceEquals(distinctInstance, TestStringEventId.Go));

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(distinctInstance));
            Assert.AreEqual(1, handled);
        }

        [TestMethod]
        public void Test_StringEvents_MachineLevel_HandledFromAnyState()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId, string>()
                .AddEvent("reset", e => { log.Add("machine:reset"); e.ChangeState(TestStateId.State1); })
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => e.ChangeState(TestStateId.State2))
                    .End()
                .State<NoOpState, string>(TestStateId.State2)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go), "State1 handles Go");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStringEventId.Go), "State2 has no events");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent("reset"), "Machine-level event is handled regardless of state");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go), "reset moved the machine back to State1");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStringEventId.Unregistered));
            CollectionAssert.AreEqual(new[] { "machine:reset" }, log);
        }

        [TestMethod]
        public void Test_StringEvents_SubStateLevel_InnermostFirstThenParent()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => log.Add("State1"))
                    .State<NoOpState, string>(TestSubStateId.SubState1)
                        .AddEvent(TestStringEventId.Go, e => log.Add("SubState1"))
                        .End()
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go));
            CollectionAssert.AreEqual(new[] { "SubState1", "State1" }, log);
        }

        [TestMethod]
        public void Test_StringEvents_SubState_ChangeStateFromHandler()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .State<NoOpState, string>(TestSubStateId.SubState1)
                        .AddEvent(TestStringEventId.Go, e => { log.Add("SubState1"); e.ChangeState(TestSubStateId.SubState2); })
                        .End()
                    .State<NoOpState, string>(TestSubStateId.SubState2)
                        .AddEvent(TestStringEventId.Go, e => log.Add("SubState2"))
                        .End()
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go), "SubState1 handles and transitions");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go), "SubState2 handles");
            CollectionAssert.AreEqual(new[] { "SubState1", "SubState2" }, log);
        }

        #endregion

        #region int

        [TestMethod]
        public void Test_IntEvents_StateLevel_Handled()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, int>(TestStateId.State1)
                    .AddEvent(42, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(42));
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(7));
            Assert.AreEqual(1, handled);
        }

        [TestMethod]
        public void Test_IntEvents_LongArgument_DoesNotReachIntState()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, int>(TestStateId.State1)
                    .AddEvent(42, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            //The type argument is inferred from the argument's static type, so a long never matches a state declared with int
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(42L));
            Assert.AreEqual(0, handled);
        }

        #endregion

        #region struct and record

        [TestMethod]
        public void Test_StructEvents_EqualButDistinctInstance_IsHandled_AtStateAndMachineLevel()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId, TestStructEventId>()
                .AddEvent(TestStructEventId.Beta, e => log.Add("machine:Beta"))
                .State<NoOpState, TestStructEventId>(TestStateId.State1)
                    .AddEvent(TestStructEventId.Alpha, e => log.Add("State1:Alpha"))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestStructEventId(1)), "Equal to Alpha");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestStructEventId(2)), "Equal to Beta");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStructEventId.Unregistered));
            CollectionAssert.AreEqual(new[] { "State1:Alpha", "machine:Beta" }, log);
        }

        [TestMethod]
        public void Test_RecordEvents_ValueEquality_IsHandled()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestRecordEventId>(TestStateId.State1)
                    .AddEvent(new TestRecordEventId("jump"), e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestRecordEventId("jump")));
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(new TestRecordEventId("duck")));
            Assert.AreEqual(1, handled);
        }

        #endregion

        #region class and interface

        [TestMethod]
        public void Test_ClassEvents_ReferenceEquality_DifferentInstanceIsNotHandled()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestConcreteEventId>(TestStateId.State1)
                    .AddEvent(TestConcreteEventId.Shared, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestConcreteEventId.Shared));
            //TestConcreteEventId does not override Equals, so an equal-looking instance is a different key
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(new TestConcreteEventId("shared")));
            Assert.AreEqual(1, handled);
        }

        [TestMethod]
        public void Test_InterfaceEvents_TypeArgumentMustMatchDeclaredType()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, ITestEventId>(TestStateId.State1)
                    .AddEvent(TestConcreteEventId.Shared, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            //Inferred as TestConcreteEventId, and no state was declared with that event type
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestConcreteEventId.Shared));
            Assert.AreEqual(0, handled);

            //An explicit type argument matches the type the state was declared with
            Assert.IsTrue(stateMachine.StateController.TriggerEvent<ITestEventId>(TestConcreteEventId.Shared));
            Assert.AreEqual(1, handled);
        }

        #endregion

        #region mixed levels

        [TestMethod]
        public void Test_MixedLevels_EnumParentAndStringSubState_EachTypeReachesOnlyItsLevel()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestEventId.Event1, e => log.Add("State1"))
                    .State<NoOpState, string>(TestSubStateId.SubState1)
                        .AddEvent(TestStringEventId.Go, e => log.Add("SubState1"))
                        .End()
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestStringEventId.Go));
            CollectionAssert.AreEqual(new[] { "SubState1" }, log);

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestEventId.Event1));
            CollectionAssert.AreEqual(new[] { "SubState1", "State1" }, log);
        }

        #endregion

        #region duplicates and nulls

        [TestMethod]
        public void Test_DuplicateStringEvent_StateLevel_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => { });

            Assert.ThrowsExactly<ArgumentException>(() => builder.AddEvent(TestStringEventId.Go, e => { }));
        }

        [TestMethod]
        public void Test_DuplicateStringEvent_MachineLevel_ThrowsArgumentException()
        {
            var builder = new StateMachineBuilder<TestStateId, string>()
                .AddEvent(TestStringEventId.Go, e => { });

            Assert.ThrowsExactly<ArgumentException>(() => builder.AddEvent(TestStringEventId.Go, e => { }));
        }

        [TestMethod]
        public void Test_NullEvent_AddEvent_StateLevel_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent(null, e => { }));
            Assert.AreEqual("eventId", ex.ParamName);
        }

        [TestMethod]
        public void Test_NullEvent_AddEvent_MachineLevel_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId, string>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent(null, e => { }));
            Assert.AreEqual("eventId", ex.ParamName);
        }

        [TestMethod]
        public void Test_NullEvent_TriggerEvent_ThrowsArgumentNullException()
        {
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => { })
                    .End()
                .Build();
            stateMachine.Start();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => stateMachine.StateController.TriggerEvent<string>(null));
            Assert.AreEqual("eventId", ex.ParamName);
        }

        [TestMethod]
        public void Test_NullEvent_TriggerEvent_ThrowsEvenWhenNoStateUsesThatType()
        {
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1)
                    .End()
                .Build();
            stateMachine.Start();

            //The guard runs before the walk, so the result does not depend on which levels match
            Assert.ThrowsExactly<ArgumentNullException>(() => stateMachine.StateController.TriggerEvent<string>(null));
        }

        #endregion

        #region deprecated per-state controllers

        [TestMethod]
        public void Test_DeprecatedProvider_LooksUpControllerForStringEventState()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            var controller = stateMachine.StateControllerProvider
                .GetStateController<IStateController<string>, TestStateId, string>(TestStateId.State1);

            Assert.IsTrue(controller.TriggerEvent(TestStringEventId.Go));
            Assert.IsFalse(controller.TriggerEvent(TestStringEventId.Unregistered));
            Assert.AreEqual(1, handled);
        }

        #endregion

        #region allocation

        [TestMethod]
        public void Test_StringEvents_TriggerEvent_WalkDoesNotAllocate()
        {
            var stateMachine = new StateMachineBuilder<TestStateId, string>()
                .AddEvent(TestStringEventId.Go, e => { })
                .State<NoOpState, string, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestStringEventId.Go, e => { })
                    .State<NoOpState, string, TestGrandchildStateId>(TestSubStateId.SubState1)
                        .AddEvent(TestStringEventId.Go, e => { })
                        .State<NoOpState, string>(TestGrandchildStateId.GrandchildState1)
                            .AddEvent(TestStringEventId.Go, e => { })
                            .End()
                        .End()
                    .End()
                .Build();
            stateMachine.Start();
            //Warm up so JIT compilation and generic instantiation are not measured
            stateMachine.StateController.TriggerEvent(TestStringEventId.Unregistered);
            stateMachine.StateController.TriggerEvent(TestStringEventId.Unregistered);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var handled = stateMachine.StateController.TriggerEvent(TestStringEventId.Unregistered);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.IsFalse(handled);
            Assert.AreEqual(0L, allocated);
        }

        [TestMethod]
        public void Test_StructEvents_TriggerEvent_WalkDoesNotAllocate()
        {
            var stateMachine = new StateMachineBuilder<TestStateId, TestStructEventId>()
                .AddEvent(TestStructEventId.Alpha, e => { })
                .State<NoOpState, TestStructEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestStructEventId.Alpha, e => { })
                    .State<NoOpState, TestStructEventId, TestGrandchildStateId>(TestSubStateId.SubState1)
                        .AddEvent(TestStructEventId.Alpha, e => { })
                        .State<NoOpState, TestStructEventId>(TestGrandchildStateId.GrandchildState1)
                            .AddEvent(TestStructEventId.Alpha, e => { })
                            .End()
                        .End()
                    .End()
                .Build();
            stateMachine.Start();
            //Warm up so JIT compilation and generic instantiation are not measured
            stateMachine.StateController.TriggerEvent(TestStructEventId.Unregistered);
            stateMachine.StateController.TriggerEvent(TestStructEventId.Unregistered);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var handled = stateMachine.StateController.TriggerEvent(TestStructEventId.Unregistered);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.IsFalse(handled);
            Assert.AreEqual(0L, allocated);
        }

        #endregion
    }
}
