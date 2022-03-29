using System;

namespace Cerberus.IoC
{
    internal class DefaultStateMachineContainer : IStateMachineContainer
    {
        public T Resolve<T>()
        {
            return Activator.CreateInstance<T>();
        }

        public object Resolve(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}
