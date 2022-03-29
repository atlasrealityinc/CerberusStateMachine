using Cerberus.Builder.Data;
using Cerberus.Runner;
using Cerberus.StateController;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerberus
{
    internal class StateMachine<StateIdT> : IStateMachine<StateIdT>, IStateChanger<StateIdT>
            where StateIdT : Enum
    {
        protected readonly IDictionary<StateIdT, StateRunner<StateIdT>> _stateRunners;

        public IStateControllerProvider StateControllerProvider { get; }

        private StateRunner<StateIdT> _activeState = null;
        private bool _isRunning = false;

        public StateMachine(IDictionary<StateIdT, StateData<StateIdT>> stateData, StateMachineData<StateIdT> stateMachineData)
        {
            if (stateData.Count <= 0)
            {
                throw new ArgumentException("You must have states in order to build the state machine!");
            }
            _stateRunners = stateData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build(this));
            StateControllerProvider = new StateMachineStateControllerProvider<StateIdT>(
                _stateRunners
                    .ToDictionary(kvp => (Enum)kvp.Key, kvp => kvp.Value.BindInfo.ToDictionary(bindInfo => bindInfo.ContractTypes[0].GetGenericArguments()[0], bindInfo => bindInfo)),
                stateMachineData?.Build(this));
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;

            ChangeState(_stateRunners.First().Key);
        }

        public void ChangeState(StateIdT stateId)
        {
            if (_activeState != null)
            {
                if (_activeState.StateId.Equals(stateId))
                {
                    return;
                }

                _activeState.Stop();
            }

            if (_stateRunners.TryGetValue(stateId, out var nextActiveState))
            {
                _activeState = nextActiveState;
                _activeState.Start();
            }
        }
    }
}
