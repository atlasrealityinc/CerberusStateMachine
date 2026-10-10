using Cerberus.Builder;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests
{
    /// <summary>
    /// Predicate events: AddEvent(predicate, action) runs its handler when the triggered event id satisfies the
    /// predicate instead of equalling a key. These tests drive the feature end to end through the builder and the
    /// unified state controller, at state, sub-state and machine level.
    /// </summary>
    [TestClass]
    public class PredicateEventTests
    {
        #region state level

        [TestMethod]
        public void Test_StateLevel_TwoPredicatesSplitOnAField_EachTakesItsOwnTransition()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => { log.Add($"State1:on:{e.EventId.Amount}"); e.ChangeState(TestStateId.State2); })
                    .AddEvent(i => !i.IsOn, e => { log.Add($"State1:off:{e.EventId.Amount}"); e.ChangeState(TestStateId.State3); })
                    .End()
                .State<NoOpState, TestFlagEventId>(TestStateId.State2)
                    .AddEvent(i => !i.IsOn, e => { log.Add("State2:off"); e.ChangeState(TestStateId.State1); })
                    .End()
                .State<NoOpState, TestFlagEventId>(TestStateId.State3)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 7)), "State1 handles on");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(false, 1)), "State2 handles off");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(false, 9)), "State1 handles off");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "State3 has no events");
            CollectionAssert.AreEqual(new[] { "State1:on:7", "State2:off", "State1:off:9" }, log);
        }

        [TestMethod]
        public void Test_StateLevel_KeyBasedEventWinsOverPredicate_RegardlessOfRegistrationOrder()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => log.Add($"predicate:{e.EventId.Amount}"))
                    .AddEvent(new TestFlagEventId(true, 1), e => log.Add("key:1"))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 1)), "Equal to the key");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 2)), "Not equal to the key, satisfies the predicate");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(new TestFlagEventId(false, 1)), "Neither");
            CollectionAssert.AreEqual(new[] { "key:1", "predicate:2" }, log);
        }

        [TestMethod]
        public void Test_StateLevel_OverlappingPredicates_FirstRegisteredWins()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.Amount > 0, e => log.Add("positive"))
                    .AddEvent(i => i.Amount > 10, e => log.Add("large"))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 50)), "Both match, first wins");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 5)), "Only the first matches");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(new TestFlagEventId(true, 0)), "Neither matches");
            CollectionAssert.AreEqual(new[] { "positive", "positive" }, log);
        }

        [TestMethod]
        public void Test_StateLevel_PredicateWithEnumEventId_Works()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId>(TestStateId.State1)
                    .AddEvent(TestEventId.Event1, e => log.Add("key:Event1"))
                    .AddEvent(id => id == TestEventId.Event1 || id == TestEventId.Event2, e => log.Add($"predicate:{e.EventId}"))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestEventId.Event1), "Key wins");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestEventId.Event2), "Predicate");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestEventId.Event3));
            CollectionAssert.AreEqual(new[] { "key:Event1", "predicate:Event2" }, log);
        }

        [TestMethod]
        public void Test_StateLevel_PredicateWithStringEventId_Works()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, string>(TestStateId.State1)
                    .AddEvent(s => s.StartsWith("go"), e => log.Add(e.EventId))
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent("go-left"));
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestStringEventId.Stop));
            CollectionAssert.AreEqual(new[] { "go-left" }, log);
        }

        [TestMethod]
        public void Test_StateLevel_PredicateContext_ExposesPreviousStateIdAndStateInstance()
        {
            var previous = new List<TestStateId>();
            var instances = new List<IState>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => e.ChangeState(TestStateId.State2))
                    .End()
                .State<TypedTrackingState, TestFlagEventId>(TestStateId.State2)
                    .AddEvent(i => !i.IsOn, e => { previous.Add(e.PreviousStateId); instances.Add(e.StateInstance); })
                    .End()
                .Build();
            stateMachine.Start();

            stateMachine.StateController.TriggerEvent(TestFlagEventId.On);
            stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);

            CollectionAssert.AreEqual(new[] { TestStateId.State1 }, previous);
            Assert.AreEqual(1, instances.Count);
            Assert.IsInstanceOfType(instances[0], typeof(TypedTrackingState));
        }

        #endregion

        #region machine level

        [TestMethod]
        public void Test_MachineLevel_PredicateHandledFromAnyState_WithEventId()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(i => !i.IsOn, e => { log.Add($"machine:off:{e.EventId.Amount}"); e.ChangeState(TestStateId.State1); })
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => e.ChangeState(TestStateId.State2))
                    .End()
                .State<NoOpState, TestFlagEventId>(TestStateId.State2)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "State1 handles on");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "State2 has no events and the machine predicate is for off");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(new TestFlagEventId(false, 4)), "Machine-level predicate handles off from State2");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "Back in State1");
            CollectionAssert.AreEqual(new[] { "machine:off:4" }, log);
        }

        [TestMethod]
        public void Test_MachineLevel_KeyBasedEventWinsOverPredicate()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(i => true, e => log.Add("predicate"))
                .AddEvent(TestFlagEventId.On, e => log.Add("key"))
                .State<NoOpState, TestEventId>(TestStateId.State1)
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.On));
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.Off));
            CollectionAssert.AreEqual(new[] { "key", "predicate" }, log);
        }

        #endregion

        #region sub-states

        [TestMethod]
        public void Test_SubStateLevel_PredicateHandlersAtEveryLevelRun_InnermostFirst()
        {
            //First-match-wins applies within a level, every active level still gets its own chance
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(i => i.IsOn, e => log.Add("machine"))
                .State<NoOpState, TestFlagEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => log.Add("State1"))
                    .State<NoOpState, TestFlagEventId>(TestSubStateId.SubState1)
                        .AddEvent(i => i.IsOn, e => log.Add("SubState1"))
                        .End()
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.On));
            CollectionAssert.AreEqual(new[] { "SubState1", "State1", "machine" }, log);
        }

        [TestMethod]
        public void Test_SubStateLevel_PredicateTransitionsBetweenSubStates()
        {
            var log = new List<string>();
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .State<NoOpState, TestFlagEventId>(TestSubStateId.SubState1)
                        .AddEvent(i => i.IsOn, e => { log.Add("SubState1:on"); e.ChangeState(TestSubStateId.SubState2); })
                        .End()
                    .State<NoOpState, TestFlagEventId>(TestSubStateId.SubState2)
                        .AddEvent(i => !i.IsOn, e => { log.Add("SubState2:off"); e.ChangeState(TestSubStateId.SubState1); })
                        .End()
                    .End()
                .Build();
            stateMachine.Start();

            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "SubState1 handles on");
            Assert.IsFalse(stateMachine.StateController.TriggerEvent(TestFlagEventId.On), "SubState2 only handles off");
            Assert.IsTrue(stateMachine.StateController.TriggerEvent(TestFlagEventId.Off), "SubState2 handles off");
            CollectionAssert.AreEqual(new[] { "SubState1:on", "SubState2:off" }, log);
        }

        #endregion

        #region deprecated per-state controllers

        [TestMethod]
        public void Test_DeprecatedProvider_ControllerReachesPredicateEvents()
        {
            var handled = 0;
            var stateMachine = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => handled++)
                    .End()
                .Build();
            stateMachine.Start();

            var controller = stateMachine.StateControllerProvider
                .GetStateController<IStateController<TestFlagEventId>, TestStateId, TestFlagEventId>(TestStateId.State1);

            Assert.IsTrue(controller.TriggerEvent(TestFlagEventId.On));
            Assert.IsFalse(controller.TriggerEvent(TestFlagEventId.Off));
            Assert.AreEqual(1, handled);
        }

        #endregion

        #region null predicate

        [TestMethod]
        public void Test_NullPredicate_StateLevel_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId>(TestStateId.State1);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));
            Assert.AreEqual("predicate", ex.ParamName);
        }

        [TestMethod]
        public void Test_NullPredicate_SubStateParentLevel_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestFlagEventId, TestSubStateId>(TestStateId.State1);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));
            Assert.AreEqual("predicate", ex.ParamName);
        }

        [TestMethod]
        public void Test_NullPredicate_MachineLevel_ThrowsArgumentNullException()
        {
            var builder = new StateMachineBuilder<TestStateId, TestFlagEventId>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => builder.AddEvent((Func<TestFlagEventId, bool>)null, e => { }));
            Assert.AreEqual("predicate", ex.ParamName);
        }

        #endregion

        #region allocation

        [TestMethod]
        public void Test_PredicateEvents_NoMatch_TriggerEventDoesNotAllocate()
        {
            var stateMachine = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(i => i.IsOn, e => { })
                .State<NoOpState, TestFlagEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(i => i.IsOn, e => { })
                    .AddEvent(i => i.Amount > 100, e => { })
                    .State<NoOpState, TestFlagEventId, TestGrandchildStateId>(TestSubStateId.SubState1)
                        .AddEvent(i => i.IsOn, e => { })
                        .State<NoOpState, TestFlagEventId>(TestGrandchildStateId.GrandchildState1)
                            .AddEvent(i => i.IsOn, e => { })
                            .AddEvent(i => i.Amount > 100, e => { })
                            .End()
                        .End()
                    .End()
                .Build();
            stateMachine.Start();
            //Warm up so JIT compilation and generic instantiation are not measured
            stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);
            stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var handled = stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.IsFalse(handled);
            Assert.AreEqual(0L, allocated);
        }

        [TestMethod]
        public void Test_MixedKeyAndPredicateEvents_NoMatch_TriggerEventDoesNotAllocate()
        {
            var stateMachine = new StateMachineBuilder<TestStateId, TestFlagEventId>()
                .AddEvent(TestFlagEventId.On, e => { })
                .AddEvent(i => i.Amount > 100, e => { })
                .State<NoOpState, TestFlagEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestFlagEventId.On, e => { })
                    .AddEvent(i => i.Amount > 100, e => { })
                    .State<NoOpState, TestFlagEventId, TestGrandchildStateId>(TestSubStateId.SubState1)
                        .AddEvent(TestFlagEventId.On, e => { })
                        .AddEvent(i => i.Amount > 100, e => { })
                        .State<NoOpState, TestFlagEventId>(TestGrandchildStateId.GrandchildState1)
                            .AddEvent(TestFlagEventId.On, e => { })
                            .AddEvent(i => i.Amount > 100, e => { })
                            .End()
                        .End()
                    .End()
                .Build();
            stateMachine.Start();
            //Warm up so JIT compilation and generic instantiation are not measured
            stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);
            stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var handled = stateMachine.StateController.TriggerEvent(TestFlagEventId.Off);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.IsFalse(handled);
            Assert.AreEqual(0L, allocated);
        }

        #endregion
    }
}
