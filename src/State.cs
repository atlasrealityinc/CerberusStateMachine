namespace Cerberus
{
    public interface IState
    {
        void OnEnter();
        void OnExit();
    }

    public abstract class State : IState
    {
        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }
    }

    public abstract class State<T> : State<T, T>
    {

    }

    public abstract class State<TEnter, TExit> : IState
    {
        void IState.OnEnter()
        {
            OnEnter();
        }

        void IState.OnExit()
        {
            OnExit();
        }

        public abstract TEnter OnEnter();

        public abstract TExit OnExit();
    }
}
