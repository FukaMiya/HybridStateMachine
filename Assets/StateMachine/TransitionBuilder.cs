using System;

namespace FukaMiya.Utils
{
    public interface ITransitionStarter<TContext> : ITransitionParameterSetter<TContext>
    {
        ITransitionChain<TContext> When(StateCondition condition);
        ITransitionConditionSetter<TContext> On<TEvent>(TEvent eventId) where TEvent : Enum;
        ITransition Always();
    }

    public interface ITransitionConditionSetter<TContext>
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
        ITransitionFinalizer<TContext> SetName(string name);
    }

    internal sealed class TransitionBuilder<TContext>
        : ITransitionConditionSetter<TContext>, ITransitionStarter<TContext>, ITransitionChain<TContext>, ITransitionFinalizer<TContext>, IDisposable
    {
        private State fromState;
        private State fixedToState;
        private Func<State> stateProvider;
        private StateCondition condition;
        private int eventId = -1;
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

        public ITransitionConditionSetter<TContext> On<TEvent>(TEvent eventId) where TEvent : Enum
        {
            if (fromState.StateMachine is EnumTypeHolder enumTypeHolder)
            {
                if (enumTypeHolder.EnumType != null && enumTypeHolder.EnumType != typeof(TEvent))
                {
                    throw new InvalidOperationException($"Event type mismatch. Expected: {enumTypeHolder.EnumType.Name}, Actual: {typeof(TEvent).Name}");
                }
            }

            this.condition = null;
            this.eventId = eventId.GetHashCode();
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

        public ITransitionFinalizer<TContext> SetName(string name)
        {
            transitionParams.Name = name;
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
                transition = new Transition<TContext>(fixedToState, contextProvider, eventId);
            }
            else
            {
                transition = new Transition<TContext>(stateProvider, contextProvider, eventId);
            }
            transition.SetCondition(condition);
            transition.SetParams(transitionParams);
            fromState.AddTransition(transition);
            Dispose();
            return transition;
        }

        public void Dispose()
        {
            fromState = null;
            fixedToState = null;
            stateProvider = null;
            condition = null;
            contextProvider = null;
        }
    }

    public static class TransitionExtensions
    {
        public static ITransitionStarter<NoContext> To<T>(this State from) where T : State
        {
            return TransitionBuilder<NoContext>.To(from, from.StateMachine.At<T>(), null);
        }

        public static ITransitionStarter<NoContext> To(this State from, State to)
        {
            return TransitionBuilder<NoContext>.To(from, to, null);
        }

        public static ITransitionStarter<TContext> To<T, TContext>(this State from, Func<TContext> context) where T : State<TContext>
        {
            var toState = from.StateMachine.At<T>();
            return TransitionBuilder<TContext>.To(from, toState, context);
        }

        public static ITransitionStarter<TContext> To<TContext>(this State from, State to, Func<TContext> context)
        {
            if (to is State<TContext>)
            {
                return TransitionBuilder<TContext>.To(from, to, context);
            }
            else
            {
                throw new InvalidOperationException($"The state {to.GetType().Name} is not of type State<{typeof(TContext).Name}>.");
            }
        }

        public static ITransitionStarter<NoContext> Back(this State from)
        {
            var builder = TransitionBuilder<NoContext>
                .To(from, () => from.StateMachine.PreviousState, null);
            builder.SetName("PreviousState");
            return builder;
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