using System;

namespace HybridStateMachine
{
    /// <summary>
    /// Interface that can build a transition.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ITransitionBuildable<TContext> : ITransitionParameterSetter<TContext>
    {
        /// <summary>
        /// Build the transition.
        /// </summary>
        /// <returns></returns>
        ITransition Build();
    }

    /// <summary>
    /// Interface for setting parameters on a transition.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ITransitionParameterSetter<TContext>
    {
        /// <summary>
        /// Set whether re-entry to the same state is allowed.
        /// </summary>
        /// <param name="allowReentry"></param>
        /// <returns></returns>
        ITransitionBuildable<TContext> SetAllowReentry(bool allowReentry);

        /// <summary>
        /// Set the weight of the transition.
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        ITransitionBuildable<TContext> SetWeight(float weight);

        /// <summary>
        /// Set the name of the transition.
        /// ex: Back() transition uses "PreviousState" as the name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ITransitionBuildable<TContext> SetName(string name);
    }

    /// <summary>
    /// Interface for starting a transition definition.
    /// </summary>
    /// <typeparam name="TContext">Type of data passed during transition (NoContext if none).</typeparam>
    public interface ITransitionStarter<TContext> : ITransitionBuildable<TContext>
    {
        /// <summary>
        /// Set the condition for the transition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITransitionChain<TContext> When(StateCondition condition);

        /// <summary>
        /// Set the event that triggers the transition.
        /// Use with push-based state machines.
        /// Ignored if used with pull-based state machines.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="eventId"></param>
        /// <returns></returns>
        ITransitionConditionSetter<TContext> On<TEvent>(TEvent eventId) where TEvent : Enum;
    }

    /// <summary>
    /// Interface for setting conditions on a transition.
    /// Not used for event-based transitions.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ITransitionConditionSetter<TContext> : ITransitionBuildable<TContext>
    {
        /// <summary>
        /// Set the condition for the transition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITransitionChain<TContext> When(StateCondition condition);
    }

    /// <summary>
    /// Interface for chaining multiple conditions for a transition.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ITransitionChain<TContext> : ITransitionBuildable<TContext>
    {
        /// <summary>
        /// Add an AND condition to the transition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITransitionChain<TContext> And(StateCondition condition);

        /// <summary>
        /// Add an OR condition to the transition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITransitionChain<TContext> Or(StateCondition condition);
    }

    /// <summary>
    /// Builder class for creating transitions between states.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    internal sealed class TransitionBuilder<TContext>
        : ITransitionConditionSetter<TContext>, ITransitionStarter<TContext>, ITransitionChain<TContext>, ITransitionBuildable<TContext>, IDisposable
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
            if (fromState.GetStateMachine() is IEnumTypeHolder enumTypeHolder)
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

        public ITransitionBuildable<TContext> SetAllowReentry(bool allowReentry)
        {
            transitionParams.IsReentryAllowed = allowReentry;
            return this;
        }

        public ITransitionBuildable<TContext> SetWeight(float weight)
        {
            transitionParams.Weight = weight;
            return this;
        }

        public ITransitionBuildable<TContext> SetName(string name)
        {
            transitionParams.Name = name;
            return this;
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

    /// <summary>
    /// Extension methods for defining transitions between states.
    /// </summary>
    public static class TransitionExtensions
    {
        /// <summary>
        /// Defines a transition to a state of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <returns></returns>
        public static ITransitionStarter<NoContext> To<T>(this State from) where T : State
        {
            return TransitionBuilder<NoContext>.To(from, from.GetStateMachine().At<T>(), null);
        }

        /// <summary>
        /// Defines a transition to a specified state.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static ITransitionStarter<NoContext> To(this State from, State to)
        {
            return TransitionBuilder<NoContext>.To(from, to, null);
        }

        /// <summary>
        /// Defines a transition to a state of type T with context.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="from"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ITransitionStarter<TContext> To<T, TContext>(this State from, Func<TContext> context) where T : State<TContext>
        {
            var toState = from.GetStateMachine().At<T>();
            return TransitionBuilder<TContext>.To(from, toState, context);
        }

        /// <summary>
        /// Defines a transition to a specified state with context.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ITransitionStarter<TContext> To<TContext>(this State from, State to, Func<TContext> context)
        {
            if (to is State<TContext>)
            {
                return TransitionBuilder<TContext>.To(from, to, context);
            }
            else
            {
                throw new InvalidOperationException($"The state {to.GetType().Name} is not of type State<{typeof(TContext).Name}>. Consider changing the context type or using NoContext.");
            }
        }

        /// <summary>
        /// Defines a transition back to the previous state.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public static ITransitionStarter<NoContext> Back(this State from)
        {
            var builder = TransitionBuilder<NoContext>
                .To(from, () => from.GetStateMachine().PreviousState, null);
            builder.SetName("PreviousState");
            return builder;
        }
    }

    /// <summary>
    /// Helper class for combining state conditions.
    /// </summary>
    public static class Condition
    {
        /// <summary>
        /// Combines multiple conditions with OR logic.
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Combines multiple conditions with AND logic.
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Negates a condition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static StateCondition Not(StateCondition condition)
        {
            return () => !condition();
        }
        
        /// <summary>
        /// Creates a state condition from a predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static StateCondition Is(Func<bool> predicate)
        {
            return new StateCondition(predicate);
        }
    }
}