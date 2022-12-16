namespace Cerberus.StateHandlers
{
    internal class EnterAndExitStateHandler : IStateHandler<IState>
    {
        public void OnEnterState(IState stateInstance)
        {
            stateInstance.OnEnter();
        }

        public void OnExitState(IState stateInstance)
        {
            stateInstance.OnExit();
        }
    }
}
