using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Cerberus.Tests
{
    [TestClass]
    public class StateTests
    {
        [TestMethod]
        public void Test_OnEnter_DefaultImplementation_DoesNotThrow()
        {
            var callLog = new List<string>();
            var state = new TrackingState(callLog, "Test");

            state.OnEnter();

            Assert.IsTrue(state.Entered);
        }

        [TestMethod]
        public void Test_OnExit_DefaultImplementation_DoesNotThrow()
        {
            var callLog = new List<string>();
            var state = new TrackingState(callLog, "Test");

            state.OnExit();

            Assert.IsTrue(state.Exited);
        }

        [TestMethod]
        public void Test_GenericState_OnEnter_ReturnsTypedValue()
        {
            var state = new TypedTrackingState();

            var result = state.OnEnter();

            Assert.AreEqual("entered", result);
            Assert.AreEqual("entered", state.EnterResult);
        }

        [TestMethod]
        public void Test_GenericState_OnExit_ReturnsTypedValue()
        {
            var state = new TypedTrackingState();

            var result = state.OnExit();

            Assert.AreEqual("exited", result);
            Assert.AreEqual("exited", state.ExitResult);
        }

        [TestMethod]
        public void Test_GenericState_IStateOnEnter_DelegatesToTypedOnEnter()
        {
            var state = new TypedTrackingState();
            IState iState = state;

            iState.OnEnter();

            Assert.AreEqual("entered", state.EnterResult);
        }

        [TestMethod]
        public void Test_GenericState_IStateOnExit_DelegatesToTypedOnExit()
        {
            var state = new TypedTrackingState();
            IState iState = state;

            iState.OnExit();

            Assert.AreEqual("exited", state.ExitResult);
        }

        [TestMethod]
        public void Test_DualTypedState_OnEnter_ReturnsString()
        {
            var state = new DualTypedTrackingState();

            var result = state.OnEnter();

            Assert.AreEqual("entered", result);
        }

        [TestMethod]
        public void Test_DualTypedState_OnExit_ReturnsInt()
        {
            var state = new DualTypedTrackingState();

            var result = state.OnExit();

            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void Test_DualTypedState_IStateOnEnter_DelegatesToTypedOnEnter()
        {
            var state = new DualTypedTrackingState();
            IState iState = state;

            iState.OnEnter();

            Assert.AreEqual("entered", state.EnterResult);
        }

        [TestMethod]
        public void Test_DualTypedState_IStateOnExit_DelegatesToTypedOnExit()
        {
            var state = new DualTypedTrackingState();
            IState iState = state;

            iState.OnExit();

            Assert.AreEqual(42, state.ExitResult);
        }
    }
}
