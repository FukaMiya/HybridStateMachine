using System;

namespace HybridStateMachine
{
    /// <summary>
    /// Delegate for state transition conditions.
    /// </summary>
    /// <returns></returns>
    public delegate bool StateCondition();

    /// <summary>
    /// Parameters for a transition.
    /// Set via ITransitionParameterSetter.
    /// </summary>
    internal sealed class TransitionParams
    {
        public string Name { get; set; } = string.Empty;
        public float Weight { get; set; } = 1f;
        public bool IsReentryAllowed { get; set; } = false;
    }

    /// <summary>
    /// Interface for a transition between states.
    /// </summary>
    public interface ITransition
    {
        StateCondition Condition { get; }
        int EventId { get; }

        string Name { get; }
        float Weight { get; }
        bool IsReentryAllowed { get; }
        
        State GetToState();
        void OnTransition(State state);
    }

    /// <summary>
    /// Transition between states.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    internal sealed class Transition<TContext> : ITransition
    {
        private readonly State to;
        private readonly Func<TContext> contextProvider;
        private readonly Func<State> stateProvider;
        public int EventId { get; private set; } = -1;
        public StateCondition Condition { get; private set; }
        public TransitionParams Params { get; private set; }

        public float Weight => Params.Weight;
        public bool IsReentryAllowed => Params.IsReentryAllowed;
        public string Name => Params.Name;

        public Transition(State to, Func<TContext> contextProvider, int eventId = -1)
        {
            this.to = to;
            this.contextProvider = contextProvider;
            this.EventId = eventId;
        }

        public Transition(Func<State> stateProvider, Func<TContext> contextProvider, int eventId = -1)
        {
            this.stateProvider = stateProvider;
            this.contextProvider = contextProvider;
            this.EventId = eventId;
        }

        public void OnTransition(State nextState)
        {
            if (nextState is State<TContext> stateWithContext)
            {
                stateWithContext.SetContextProvider(contextProvider);
            }

            else if (typeof(TContext) == typeof(NoContext) && nextState is IStateWithContext clearableState)
            {
                clearableState.ClearContextProvider();
            }
        }

        public void SetCondition(StateCondition condition) => Condition = condition;
        public void SetParams(TransitionParams transitionParams) => Params = transitionParams;
        public State GetToState() => stateProvider != null ? stateProvider() : to;
        public TContext GetContext() => contextProvider != null ? contextProvider() : default;
    }
}