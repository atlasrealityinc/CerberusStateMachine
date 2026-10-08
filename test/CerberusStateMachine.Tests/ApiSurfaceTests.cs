using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cerberus.Tests
{
    /// <summary>
    /// Locks in the public generic constraints: event id type parameters are unconstrained so any type can be an
    /// event id, while state id type parameters still require an enum. Sweeps every exported type and public
    /// generic method so a newly added member is covered automatically.
    /// </summary>
    [TestClass]
    public class ApiSurfaceTests
    {
        private static readonly HashSet<string> EventIdParameterNames = new HashSet<string> { "EventIdT", "SubEventIdT" };
        private static readonly HashSet<string> StateIdParameterNames = new HashSet<string> { "StateIdT", "SubStateIdT", "SubSubStateIdT" };

        [TestMethod]
        public void Test_EventIdTypeParameters_HaveNoConstraints()
        {
            var parameters = CollectGenericParameters(EventIdParameterNames);

            Assert.IsTrue(parameters.Count >= 8, $"Expected to find the event id parameters across the public API, found {parameters.Count}");
            CollectionAssert.Contains(parameters.Select(p => p.Owner).ToList(), "IStateController.TriggerEvent");
            CollectionAssert.Contains(parameters.Select(p => p.Owner).ToList(), "StateMachineBuilder`2");

            var constrained = parameters
                .Where(p => p.Parameter.GetGenericParameterConstraints().Length > 0
                         || p.Parameter.GenericParameterAttributes != GenericParameterAttributes.None)
                .Select(p => $"{p.Owner}<{p.Parameter.Name}>")
                .ToList();

            Assert.AreEqual(0, constrained.Count, "Event id type parameters must accept any type. Constrained: " + string.Join(", ", constrained));
        }

        [TestMethod]
        public void Test_StateIdTypeParameters_StillRequireEnum()
        {
            var parameters = CollectGenericParameters(StateIdParameterNames);

            Assert.IsTrue(parameters.Count >= 8, $"Expected to find the state id parameters across the public API, found {parameters.Count}");
            CollectionAssert.Contains(parameters.Select(p => p.Owner).ToList(), "IStateMachine`1");

            var unconstrained = parameters
                .Where(p => !p.Parameter.GetGenericParameterConstraints().Contains(typeof(Enum)))
                .Select(p => $"{p.Owner}<{p.Parameter.Name}>")
                .ToList();

            Assert.AreEqual(0, unconstrained.Count, "State id type parameters are expected to require an enum. Unconstrained: " + string.Join(", ", unconstrained));
        }

        private static List<(string Owner, Type Parameter)> CollectGenericParameters(HashSet<string> names)
        {
            var found = new List<(string Owner, Type Parameter)>();
            foreach (var type in typeof(IStateController).Assembly.GetExportedTypes())
            {
                if (type.IsGenericTypeDefinition)
                {
                    foreach (var parameter in type.GetGenericArguments().Where(p => names.Contains(p.Name)))
                    {
                        found.Add((type.Name, parameter));
                    }
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsGenericMethodDefinition);
                foreach (var method in methods)
                {
                    foreach (var parameter in method.GetGenericArguments().Where(p => names.Contains(p.Name)))
                    {
                        found.Add(($"{type.Name}.{method.Name}", parameter));
                    }
                }
            }
            return found;
        }
    }
}
