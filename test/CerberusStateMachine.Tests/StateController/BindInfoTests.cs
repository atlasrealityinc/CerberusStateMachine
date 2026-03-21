using Cerberus.StateController;
using Cerberus.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Cerberus.Tests.StateController
{
    [TestClass]
    public class BindInfoTests
    {
        [TestMethod]
        public void Test_BindInfo_Constructor_SetsInstance()
        {
            var instance = new object();
            var contractTypes = new[] { typeof(IDisposable) };

            var bindInfo = new BindInfo(instance, contractTypes);

            Assert.AreSame(instance, bindInfo.Instance);
        }

        [TestMethod]
        public void Test_BindInfo_Constructor_SetsContractTypes()
        {
            var instance = new object();
            var contractTypes = new[] { typeof(IDisposable), typeof(IComparable) };

            var bindInfo = new BindInfo(instance, contractTypes);

            CollectionAssert.AreEqual(contractTypes, bindInfo.ContractTypes);
        }

        [TestMethod]
        public void Test_StateControllerBindInfo_Constructor_SetsState()
        {
            var instance = new object();
            var contractTypes = new[] { typeof(IDisposable) };

            var bindInfo = new StateControllerBindInfo<TestStateId>(
                TestStateId.State1, instance, contractTypes);

            Assert.AreEqual(TestStateId.State1, bindInfo.State);
        }

        [TestMethod]
        public void Test_StateControllerBindInfo_InheritsBindInfoProperties()
        {
            var instance = new object();
            var contractTypes = new[] { typeof(IDisposable) };

            var bindInfo = new StateControllerBindInfo<TestStateId>(
                TestStateId.State1, instance, contractTypes);

            Assert.AreSame(instance, bindInfo.Instance);
            CollectionAssert.AreEqual(contractTypes, bindInfo.ContractTypes);
        }
    }
}
