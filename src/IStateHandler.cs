namespace Cerberus
{
    public interface IStateHandler<StateT>
    {
        void OnEnterState(StateT stateInstance);

        void OnExitState(StateT stateInstance);
    }
}
