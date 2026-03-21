using Cerberus.IoC;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cerberus.Tests.IoC
{
    [TestClass]
    public class DefaultStateMachineContainerTests
    {
        [TestMethod]
        public void Test_ResolveGeneric_CreatesInstance()
        {
            var container = new DefaultStateMachineContainer();

            var instance = container.Resolve<NoOpState>();

            Assert.IsNotNull(instance);
            Assert.IsInstanceOfType(instance, typeof(NoOpState));
        }

        [TestMethod]
        public void Test_ResolveType_CreatesInstance()
        {
            var container = new DefaultStateMachineContainer();

            var instance = container.Resolve(typeof(NoOpState));

            Assert.IsNotNull(instance);
            Assert.IsInstanceOfType(instance, typeof(NoOpState));
        }

        [TestMethod]
        public void Test_ResolveGeneric_CreatesNewInstanceEachTime()
        {
            var container = new DefaultStateMachineContainer();

            var instance1 = container.Resolve<NoOpState>();
            var instance2 = container.Resolve<NoOpState>();

            Assert.AreNotSame(instance1, instance2);
        }

        [TestMethod]
        public void Test_ResolveType_CreatesNewInstanceEachTime()
        {
            var container = new DefaultStateMachineContainer();

            var instance1 = container.Resolve(typeof(NoOpState));
            var instance2 = container.Resolve(typeof(NoOpState));

            Assert.AreNotSame(instance1, instance2);
        }
    }
}
