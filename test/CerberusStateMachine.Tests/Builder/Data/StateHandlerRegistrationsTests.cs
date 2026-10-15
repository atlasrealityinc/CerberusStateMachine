using Cerberus.Builder.Data;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Cerberus.Tests.Builder.Data
{
    [TestClass]
    public class StateHandlerRegistrationsTests
    {
        [TestMethod]
        public void Test_Add_PreservesRegistrationOrderAcrossStateTypes()
        {
            var registrations = new StateHandlerRegistrations
            {
                { typeof(IState), typeof(TrackingStateHandlerA) },
                { typeof(ICustomState), typeof(CustomStateHandler) },
                { typeof(IState), typeof(TrackingStateHandlerB) }
            };

            CollectionAssert.AreEqual(
                new[] { typeof(TrackingStateHandlerA), typeof(CustomStateHandler), typeof(TrackingStateHandlerB) },
                registrations.Select(r => r.HandlerType).ToArray());
            CollectionAssert.AreEqual(
                new[] { typeof(IState), typeof(ICustomState), typeof(IState) },
                registrations.Select(r => r.StateType).ToArray());
        }

        [TestMethod]
        public void Test_AddGeneric_RecordsStateAndHandlerTypes()
        {
            var registrations = new StateHandlerRegistrations();

            registrations.Add<IState, TrackingStateHandlerA>();

            Assert.AreEqual(1, registrations.Count);
            var registration = registrations.Single();
            Assert.AreEqual(typeof(IState), registration.StateType);
            Assert.AreEqual(typeof(TrackingStateHandlerA), registration.HandlerType);
        }

        [TestMethod]
        public void Test_Add_NullStateType_ThrowsArgumentNullException()
        {
            var registrations = new StateHandlerRegistrations();

            Assert.ThrowsExactly<ArgumentNullException>(() => registrations.Add(null, typeof(TrackingStateHandlerA)));
        }

        [TestMethod]
        public void Test_Add_NullHandlerType_ThrowsArgumentNullException()
        {
            var registrations = new StateHandlerRegistrations();

            Assert.ThrowsExactly<ArgumentNullException>(() => registrations.Add(typeof(IState), null));
        }
    }
}
