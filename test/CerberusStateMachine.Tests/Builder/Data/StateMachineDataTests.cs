using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateMachineDataTests
    {
        [TestMethod]
        public void Test_Build_ReturnsStateMachineRunner()
        {
            var data = new StateMachineData<TestStateId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsNull(runner.BindInfo);
        }

        [TestMethod]
        public void Test_TypedBuild_ReturnsTypedStateMachineRunner()
        {
            var data = new StateMachineData<TestStateId, TestMachineEventId>();
            var stateChanger = Substitute.For<IStateChanger<TestStateId>>();

            var runner = data.Build(stateChanger);

            Assert.IsNotNull(runner);
            Assert.IsInstanceOfType(runner, typeof(StateMachineRunner<TestStateId, TestMachineEventId>));
        }
    }
}
