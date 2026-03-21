using Cerberus.Builder;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Cerberus.Tests.Builder
{
    [TestClass]
    public class StateBuilderWithSubStatesTests
    {
        [TestMethod]
        public void Test_AddEvent_ReturnsSelfWithCorrectType()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .AddEvent(TestEventId.Event1, e => { });

            Assert.IsInstanceOfType(sm,
                typeof(StateBuilderWithSubStates<NoOpState, TestStateId, TestEventId, TestSubStateId,
                    StateMachineBuilderWithStates<TestStateId>>));
        }

        [TestMethod]
        public void Test_State_AddsSubState()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .State<NoOpState, TestSubEventId>(TestSubStateId.SubState1).End()
                .End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_StateWithNestedSubStates_AddsSubState()
        {
            var sm = new StateMachineBuilder<TestStateId>()
                .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                    .State<NoOpState, TestSubEventId, TestGrandchildStateId>(TestSubStateId.SubState1)
                        .State<NoOpState, TestGrandchildEventId>(TestGrandchildStateId.GrandchildState1).End()
                    .End()
                .End()
                .Build();

            Assert.IsNotNull(sm);
        }

        [TestMethod]
        public void Test_State_DuplicateSubStateId_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                new StateMachineBuilder<TestStateId>()
                    .State<NoOpState, TestEventId, TestSubStateId>(TestStateId.State1)
                        .State<NoOpState, TestSubEventId>(TestSubStateId.SubState1).End()
                        .State<NoOpState, TestSubEventId>(TestSubStateId.SubState1));
        }
    }
}
