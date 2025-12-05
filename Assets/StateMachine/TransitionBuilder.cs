using System;

namespace FukaMiya.Utils
{
    public interface ITransitionInitializer
    {
        public ITransitionInitializer From<T>() where T : State, new();
        public ITransitionStarter<NoContext> To<T>() where T : State, new();
        public ITransitionStarter<TContext> To<T, TContext>(Func<TContext> context) where T : State<TContext>, new();
        public ITransitionStarter<NoContext> To(State toState);
        public ITransitionStarter<TContext> To<TContext>(State toState, Func<TContext> context);
    }

    public sealed class TransitionInitializer : ITransitionInitializer
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

    public interface ITransitionStarter<TContext> : ITransitionParameterSetter<TContext>
    {
        public ITransitionChain<TContext> When(StateCondition condition);
        public Transition<TContext> Always();
    }

    public interface ITransitionChain<TContext> : ITransitionParameterSetter<TContext>
    {
        public ITransitionChain<TContext> And(StateCondition condition);
        public ITransitionChain<TContext> Or(StateCondition condition);
        public Transition<TContext> Build();
    }

    public interface ITransitionFinalizer<TContext> : ITransitionParameterSetter<TContext>
    {
        public Transition<TContext> Build();
    }

    public interface ITransitionParameterSetter<TContext>
    {
        public ITransitionFinalizer<TContext> SetAllowReentry(bool allowReentry);
        public ITransitionFinalizer<TContext> SetWeight(float weight);
    }

    public sealed class TransitionBuilder<TContext> : ITransitionStarter<TContext>, ITransitionChain<TContext>, ITransitionFinalizer<TContext>
    {
        private State fromState;
        private State fixedToState;
        private Func<TContext> contextProvider;
        private Func<State> stateProvider;
        private StateCondition condition;

        private readonly TransitionParams transitionParams = new();

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

        public Transition<TContext> Always()
        {
            return Build();
        }

        public Transition<TContext> Build()
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

    public delegate bool StateCondition();

    public interface ITransition
    {
        public StateCondition Condition { get; }
        public TransitionParams Params { get; }
        public State GetToState();
    }

    public sealed class Transition<TContext> : ITransition
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

        public void SetCondition(StateCondition condition)
        {
            Condition = condition;
        }

        public void SetParams(TransitionParams transitionParams)
        {
            Params = transitionParams;
        }

        public State GetToState()
        {
            return stateProvider != null ? stateProvider() : to;
        }

        public TContext GetContext()
        {
            return contextProvider != null ? contextProvider() : default;
        }
    }
    
    public sealed class TransitionParams
    {
        public float Weight { get; set; } = 1f;
        public bool IsReentryAllowed { get; set; } = false;
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
        
        // ラムダ式を明示的に変換したい場合用（基本不要だが互換性のため）
        public static StateCondition Is(Func<bool> predicate)
        {
            return new StateCondition(predicate);
        }
    }
}