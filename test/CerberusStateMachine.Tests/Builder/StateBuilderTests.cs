using Cerberus.Builder;
using Cerberus.Builder.Data;
using Cerberus.IoC;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Cerberus.Tests.Builder
{
    [TestClass]
    public class StateBuilderTests
    {
        [TestMethod]
        public void Test_AddEvent_AddsEventToStateData()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var parentBuilder = new object();
            var builder = new StateBuilder<NoOpState, TestStateId, TestEventId, object>(
                parentBuilder, stateData);

            builder.AddEvent(TestEventId.Event1, e => { });

            Assert.IsTrue(stateData.StateEvents.ContainsKey(TestEventId.Event1));
        }

        [TestMethod]
        public void Test_AddEvent_ReturnsSelf()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var parentBuilder = new object();
            var builder = new StateBuilder<NoOpState, TestStateId, TestEventId, object>(
                parentBuilder, stateData);

            var result = builder.AddEvent(TestEventId.Event1, e => { });

            Assert.AreSame(builder, result);
        }

        [TestMethod]
        public void Test_End_ReturnsParentBuilder()
        {
            var container = Substitute.For<IStateMachineContainer>();
            var handlerTypes = new Dictionary<Type, List<Type>>();
            var stateData = new StateData<NoOpState, TestStateId, TestEventId>(
                TestStateId.State1, container, handlerTypes);
            var parentBuilder = new object();
            var builder = new StateBuilder<NoOpState, TestStateId, TestEventId, object>(
                parentBuilder, stateData);

            var result = builder.End();

            Assert.AreSame(parentBuilder, result);
        }
    }
}
