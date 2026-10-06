using Cerberus.StateController;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace Cerberus.Tests.StateController
{
    /// <summary>
    /// Guards the deprecation of the per-state controller API (see docs/DEPRECATED.md) so a refactor cannot
    /// silently drop the [Obsolete] markers, turn them into errors, or apply them to the replacement API.
    /// </summary>
    [TestClass]
    public class DeprecationTests
    {
        private static readonly Type[] DeprecatedTypes =
        {
            typeof(IStateControllerProvider),
            typeof(BindInfo),
            typeof(StateControllerBindInfo<>),
            typeof(IStateController<>),
            typeof(IStateController<,>),
        };

        [TestMethod]
        public void Test_PerStateControllerTypes_AreMarkedObsolete()
        {
            foreach (var type in DeprecatedTypes)
            {
                var obsolete = type.GetCustomAttribute<ObsoleteAttribute>(inherit: false);

                Assert.IsNotNull(obsolete, $"{type.Name} must carry [Obsolete]");
                Assert.IsFalse(obsolete.IsError, $"{type.Name} must be a deprecation warning, not an error");
                StringAssert.Contains(obsolete.Message, "StateController", $"{type.Name} [Obsolete] message must point at the replacement");
                StringAssert.Contains(obsolete.Message, "docs/DEPRECATED.md", $"{type.Name} [Obsolete] message must point at the migration guide");
            }
        }

        [TestMethod]
        public void Test_IStateMachine_StateControllerProvider_IsMarkedObsolete()
        {
            var property = typeof(IStateMachine<>).GetProperty(nameof(IStateMachine<TestStateId>.StateControllerProvider));
            Assert.IsNotNull(property);

            var obsolete = property.GetCustomAttribute<ObsoleteAttribute>(inherit: false);

            Assert.IsNotNull(obsolete, "IStateMachine.StateControllerProvider must carry [Obsolete]");
            Assert.IsFalse(obsolete.IsError, "IStateMachine.StateControllerProvider must be a deprecation warning, not an error");
            StringAssert.Contains(obsolete.Message, "StateController", "[Obsolete] message must point at the replacement");
            StringAssert.Contains(obsolete.Message, "docs/DEPRECATED.md", "[Obsolete] message must point at the migration guide");
        }

        [TestMethod]
        public void Test_ReplacementApi_IsNotObsolete()
        {
            Assert.IsNull(typeof(IStateController).GetCustomAttribute<ObsoleteAttribute>(inherit: false), "IStateController is the replacement and must not be obsolete");
            Assert.IsNull(typeof(IStateMachine<>).GetCustomAttribute<ObsoleteAttribute>(inherit: false), "IStateMachine must not be obsolete");

            var property = typeof(IStateMachine<>).GetProperty(nameof(IStateMachine<TestStateId>.StateController));
            Assert.IsNotNull(property);
            Assert.IsNull(property.GetCustomAttribute<ObsoleteAttribute>(inherit: false), "IStateMachine.StateController is the replacement and must not be obsolete");
        }
    }
}
