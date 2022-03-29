using System;

namespace Cerberus.IoC
{
    public interface IStateMachineContainer
    {
        T Resolve<T>();
        object Resolve(Type type);
    }
}
