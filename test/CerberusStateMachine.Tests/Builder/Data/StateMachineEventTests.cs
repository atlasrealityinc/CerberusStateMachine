using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateMachineEventTests
    {
        [TestMethod]
        public void Test_ChangeState_TriggersStateChangerChangeState()
        {
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();
            var machineEvent = new StateMachineEvent<TestStateId>(stateChanger);

            machineEvent.ChangeState(TestStateId.State2);

            stateChanger.Received(1).ChangeState(TestStateId.State2);
        }
    }
}
