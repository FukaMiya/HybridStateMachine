using System;

namespace FukaMiya.Utils
{
    public delegate bool StateCondition();
    public sealed class TransitionParams
    {
        public float Weight { get; set; } = 1f;
        public bool IsReentryAllowed { get; set; } = false;
    }

    public interface ITransition
    {
        public StateCondition Condition { get; }
        TransitionParams Params { get; }
        public State GetToState();
        public void OnTransition(State state);
    }

    internal sealed class Transition<TContext> : ITransition
    {
        private readonly State to;
        private readonly Func<TContext> contextProvider;
        private readonly Func<State> stateProvider;
        public StateCondition Condition { get; private set; }

        public TransitionParams Params { get; private set;}

        public Transition(State to, Func<TContext> contextProvider)
        {
            this.to = to;
            this.contextProvider = contextProvider;
        }

        public Transition(Func<State> stateProvider, Func<TContext> contextProvider)
        {
            this.stateProvider = stateProvider;
            this.contextProvider = contextProvider;
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