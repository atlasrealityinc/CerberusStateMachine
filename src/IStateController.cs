using System;

namespace Cerberus
{
    public interface IStateController<EventIdT>
        where EventIdT : Enum
    {
        bool TriggerEvent(EventIdT eventId);
    }

    public interface IStateController<EventIdT, SubStateIdT> : IStateController<EventIdT>
        where EventIdT : Enum
        where SubStateIdT : Enum
    {
        SubStateIdT CurrentSubState { get; }
    }
}
