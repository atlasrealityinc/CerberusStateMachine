using Cerberus.Runner;
using System;

namespace Cerberus.StateController
{
    /// <summary>
    /// A single controller able to trigger an event of any event id type. The event is offered to every
    /// currently active state, from the innermost active sub-state out to the top-level state, and finally
    /// to the machine-level events. Triggering an event does not allocate.
    /// </summary>
    internal class StateMachineStateController : IStateController
    {
        private readonly Func<IStateRunner> _getActiveStateRunner;
        private readonly IStateRunner _stateMachineRunner;

        public StateMachineStateController(Func<IStateRunner> getActiveStateRunner, IStateRunner stateMachineRunner)
        {
            _getActiveStateRunner = getActiveStateRunner ?? throw new ArgumentNullException(nameof(getActiveStateRunner));
            _stateMachineRunner = stateMachineRunner;
        }

        public bool TriggerEvent<EventIdT>(EventIdT eventId)
            where EventIdT : Enum
        {
            var handled = TriggerInnermostFirst(_getActiveStateRunner.Invoke(), eventId);
            //Machine-level events are the outermost scope, so they are offered the event last
            handled |= TriggerInnermostFirst(_stateMachineRunner, eventId);
            return handled;
        }

        private static bool TriggerInnermostFirst<EventIdT>(IStateRunner runner, EventIdT eventId)
            where EventIdT : Enum
        {
            if (runner == null)
            {
                return false;
            }

            //The child link is read before anything is triggered, so a handler that changes state cannot alter the walk
            var handled = TriggerInnermostFirst(runner.ActiveSubStateRunner, eventId);
            if (runner is IEventTrigger<EventIdT> eventTrigger)
            {
                //No short circuit here, every active level is offered the event
                handled |= eventTrigger.TriggerEvent(eventId);
            }
            return handled;
        }
    }
}
