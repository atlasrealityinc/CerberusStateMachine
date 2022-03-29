namespace Cerberus.StateHandlers
{
    internal class EnterAndExitStateHandler : IStateHandler<State>
    {
        public void OnEnterState(State stateInstance)
        {
            stateInstance.OnEnter();
        }

        public void OnExitState(State stateInstance)
        {
            stateInstance.OnExit();
        }
    }
}
