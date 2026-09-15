using System;

namespace Cerberus.Runner
{
    /// <summary>
    /// Implemented by runners that can handle events of a specific event id type.
    /// </summary>
    internal interface IEventTrigger<EventIdT>
        where EventIdT : Enum
    {
        bool TriggerEvent(EventIdT eventId);
    }
}
