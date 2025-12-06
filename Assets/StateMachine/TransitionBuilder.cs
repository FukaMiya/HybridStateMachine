using System;

namespace FukaMiya.Utils
{
    public interface ITransitionInitializer
    {
        ITransitionInitializer From<T>() where T : State, new();
        ITransitionStarter<NoContext> To<T>() where T : State, new();
        ITransitionStarter<TContext> To<T, TContext>(Func<TContext> context) where T : State<TContext>, new();
        ITransitionStarter<NoContext> To(State toState);
        ITransitionStarter<TContext> To<TContext>(State toState, Func<TContext> context);
    }

    public interface ITransitionStarter<TContext> : ITransitionParameterSetter<TContext>
    {
        ITransitionChain<TContext> When(StateCondition condition);
        ITransition Always();
    }

    public interface ITransitionChain<TContext> : ITransitionParameterSetter<TContext>
    {
        ITransitionChain<TContext> And(StateCondition condition);
        ITransitionChain<TContext> Or(StateCondition condition);
        ITransition Build();
    }

    public interface ITransitionFinalizer<TContext> : ITransitionParameterSetter<TContext>
    {
        ITransition Build();
    }

    public interface ITransitionParameterSetter<TContext>
    {
        ITransitionFinalizer<TContext> SetAllowReentry(bool allowReentry);
        ITransitionFinalizer<TContext> SetWeight(float weight);
    }

    internal sealed class TransitionBuilder<TContext> : ITransitionStarter<TContext>, ITransitionChain<TContext>, ITransitionFinalizer<TContext>
    {
        private State fromState;
        private State fixedToState;
        private Func<State> stateProvider;
        private StateCondition condition;
        private readonly TransitionParams transitionParams = new();
        private Func<TContext> contextProvider;

        public static ITransitionStarter<TContext> To(State fromState, State toState, Func<TContext> contextProvider)
        {
            var instance = new TransitionBuilder<TContext>
            {
                fromState = fromState,
                fixedToState = toState,
                contextProvider = contextProvider
            };
            return instance;
        }

        public static ITransitionStarter<TContext> To(State fromState, Func<State> toStateProvider, Func<TContext> contextProvider)
        {
            var instance = new TransitionBuilder<TContext>
            {
                fromState = fromState,
                stateProvider = toStateProvider,
                contextProvider = contextProvider
            };
            return instance;
        }

        public ITransitionChain<TContext> When(StateCondition condition)
        {
            this.condition = condition;
            return this;
        }

        public ITransitionChain<TContext> And(StateCondition condition)
        {
            var current = this.condition;
            this.condition = () => current() && condition();
            return this;
        }

        public ITransitionChain<TContext> Or(StateCondition condition)
        {
            var current = this.condition;
            this.condition = () => current() || condition();
            return this;
        }

        public ITransitionFinalizer<TContext> SetAllowReentry(bool allowReentry)
        {
            transitionParams.IsReentryAllowed = allowReentry;
            return this;
        }

        public ITransitionFinalizer<TContext> SetWeight(float weight)
        {
            transitionParams.Weight = weight;
            return this;
        }

        public ITransition Always()
        {
            return Build();
        }

        public ITransition Build()
        {
            if (fixedToState == null && stateProvider == null)
            {
                throw new InvalidOperationException("Either fixedToState or stateProvider must be set.");
            }

            Transition<TContext> transition;
            if (fixedToState != null)
            {
                transition = new Transition<TContext>(fixedToState, contextProvider);
            }
            else
            {
                transition = new Transition<TContext>(stateProvider, contextProvider);
            }
            transition.SetCondition(condition);
            transition.SetParams(transitionParams);
            fromState.AddTransition(transition);
            return transition;
        }
    }

    internal sealed class TransitionInitializer : ITransitionInitializer
    {
        private readonly StateMachine stateMachine;
        private readonly State fromState;

        public TransitionInitializer(StateMachine stateMachine, State fromState)
        {
            this.stateMachine = stateMachine;
            this.fromState = fromState;
        }

        public ITransitionInitializer From<T>() where T : State, new()
        {
            var newFromState = stateMachine.At<T>();
            return new TransitionInitializer(stateMachine, newFromState);
        }

        public ITransitionStarter<NoContext> To<T>() where T : State, new()
        {
            var toState = stateMachine.At<T>();
            return TransitionBuilder<NoContext>.To(fromState, toState, null);
        }

        public ITransitionStarter<NoContext> To(State toState)
        {
            return TransitionBuilder<NoContext>.To(fromState, toState, null);
        }

        public ITransitionStarter<TContext> To<T, TContext>(Func<TContext> context) where T : State<TContext>, new()
        {
            var toState = stateMachine.At<T>();
            return TransitionBuilder<TContext>.To(fromState, toState, context);
        }

        public ITransitionStarter<TContext> To<TContext>(State toState, Func<TContext> context)
        {
            if (toState is State<TContext>)
            {
                return TransitionBuilder<TContext>.To(fromState, toState, context);
            }
            else
            {
                throw new InvalidOperationException($"The state {toState.GetType().Name} is not of type State<{typeof(TContext).Name}>.");
            }
        }
    }

    public static class Condition
    {
        public static StateCondition Any(params StateCondition[] conditions)
        {
            return () =>
            {
                foreach (var condition in conditions)
                {
                    if (condition()) return true;
                }
                return false;
            };
        }

        public static StateCondition All(params StateCondition[] conditions)
        {
            return () =>
            {
                foreach (var condition in conditions)
                {
                    if (!condition()) return false;
                }
                return true;
            };
        }

        public static StateCondition Not(StateCondition condition)
        {
            return () => !condition();
        }
        
        public static StateCondition Is(Func<bool> predicate)
        {
            return new StateCondition(predicate);
        }
    }
}