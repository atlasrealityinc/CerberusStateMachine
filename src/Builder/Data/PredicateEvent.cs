using System;

namespace Cerberus.Builder.Data
{
    /// <summary>
    /// A handler registered against a predicate instead of an event id key. The runner evaluates
    /// <see cref="Predicate"/> against the triggered event id, in registration order, and runs the
    /// <see cref="Action"/> of the first one that returns true.
    /// </summary>
    internal readonly struct PredicateEvent<EventIdT, EventArgT>
    {
        public Func<EventIdT, bool> Predicate { get; }
        public Action<EventArgT> Action { get; }

        public PredicateEvent(Func<EventIdT, bool> predicate, Action<EventArgT> action)
        {
            Predicate = predicate;
            Action = action;
        }
    }
}
