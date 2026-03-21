using Cerberus.StateHandlers;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Cerberus.Tests.StateHandlers
{
    [TestClass]
    public class EnterAndExitStateHandlerTests
    {
        [TestMethod]
        public void Test_OnEnterState_CallsStateOnEnter()
        {
            var callLog = new List<string>();
            var state = new TrackingState1(callLog);
            var handler = new EnterAndExitStateHandler();

            handler.OnEnterState(state);

            Assert.IsTrue(state.Entered);
            Assert.AreEqual("State1:Enter", callLog[0]);
        }

        [TestMethod]
        public void Test_OnExitState_CallsStateOnExit()
        {
            var callLog = new List<string>();
            var state = new TrackingState1(callLog);
            var handler = new EnterAndExitStateHandler();

            handler.OnExitState(state);

            Assert.IsTrue(state.Exited);
            Assert.AreEqual("State1:Exit", callLog[0]);
        }
    }
}
