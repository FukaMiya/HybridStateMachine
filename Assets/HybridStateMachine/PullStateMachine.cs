using System;
using System.Text;

namespace HybridStateMachine
{
    /// <summary>
    /// Implementation of a pull-based state machine.
    /// </summary>
    internal class PullStateMachine : IPullStateMachine
    {
        public State CurrentState { get; protected set; }
        public State PreviousState { get; protected set; }
        public AnyState AnyState { get; protected set; }
        public event Action<State> OnStateChanged;

        private readonly StateFactory stateFactory;

        public PullStateMachine(StateFactory factory)
        {
            stateFactory = factory;
            AnyState = new AnyState();
            AnyState.SetStateMachine(this);
        }

        public void Update()
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException("CurrentState is not set. Please set the initial state using SetInitialState<T>() method.");
            }

            if (AnyState.CheckTransition(-1, out var nextState) ||
                CurrentState.CheckTransition(-1, out nextState))
            {
                ChangeState(nextState);
                return;
            }

            CurrentState.Update();
        }

        public void SetInitialState<T>() where T : State
        {
            CurrentState = At<T>();
            CurrentState.Enter();
        }

        public void SetInitialState(State state)
        {
            CurrentState = state;
            CurrentState.Enter();
        }

        public T At<T>() where T : State
        {
            if (typeof(T) == typeof(AnyState))
            {
                return AnyState as T;
            }

            var state = stateFactory.GetState<T>();
            state.SetStateMachine(this);
            return state;
        }

        public string ToMermaidString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("stateDiagram-v2");
            foreach (var state in stateFactory.CachedStates)
            {
                foreach (var t in state.GetTransitions())
                {
                    var toState = t.GetToState();
                    var transitionName = string.IsNullOrEmpty(t.Name) ? (toState == null ? "AnyState" : toState.ToString()) : t.Name;
                    sb.AppendLine($"    {state} --> {transitionName}");
                }
            }
            return sb.ToString();
        }

        protected void ChangeState(State nextState)
        {
            CurrentState.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.Enter();
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}
