using System;
using System.Collections.Generic;

namespace HybridStateMachine
{
    /// <summary>
    /// Interface for states that have context.
    /// </summary>
    internal interface IStateWithContext
    {
        void ClearContextProvider();
    }

    /// <summary>
    /// Base class for a state in the state machine.
    /// </summary>
    public abstract class State
    {
        private IStateMachine stateMachine;
        private readonly List<ITransition> transitions = new();

        /// <summary>
        /// Called when entering the state.
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// Called when exiting the state.
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Called on each update cycle while in the state.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Enter the state.
        /// </summary>
        public void Enter()
        {
            OnEnter();
        }

        /// <summary>
        /// Exit the state.
        /// </summary>
        public void Exit()
        {
            OnExit();
        }

        /// <summary>
        /// Update the state.
        /// </summary>
        public void Update()
        {
            OnUpdate();
        }


        /// <summary>
        /// Check if a transition can be made to another state based on the event ID.
        /// </summary>
        /// <param name="eventId">
        /// Use -1 for pull-based transitions (no event).
        /// </param>
        /// <param name="nextState"></param>
        /// <returns></returns>
        public bool CheckTransition(int eventId, out State nextState)
        {
            State maxWeightToState = null;
            ITransition maxWeightTransition = null;
            float maxWeight = float.MinValue;
            foreach (var transition in transitions)
            {
                if (transition.EventId == eventId && (transition.Condition == null || transition.Condition()))
                {
                    var toState = transition.GetToState();
                    if (toState == null) continue;
                    if (!transition.IsReentryAllowed && stateMachine.CurrentState.IsStateOf(toState.GetType())) continue;

                    if (maxWeightToState == null || transition.Weight > maxWeight)
                    {
                        maxWeightToState = toState;
                        maxWeightTransition = transition;
                        maxWeight = transition.Weight;
                    }
                }
            }

            if (maxWeightToState != null)
            {
                nextState = maxWeightToState;
                maxWeightTransition.OnTransition(nextState);
                return true;
            }

            nextState = null;
            return false;
        }

        internal void SetStateMachine(IStateMachine stateMachine) => this.stateMachine = stateMachine;
        internal IStateMachine GetStateMachine() => stateMachine;
        internal IReadOnlyList<ITransition> GetTransitions() => transitions.AsReadOnly();

        internal void AddTransition(ITransition transition)
        {
            if (transitions.Contains(transition))
            {
                throw new InvalidOperationException("Transition already exists in this state.");
            }
            transitions.Add(transition);
        }

        internal bool IsStateOf(Type type) => GetType() == type;

        public override string ToString() => GetType().Name;
    }

    /// <summary>
    /// Generic state class that holds context of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class State<T> : State, IStateWithContext
    {
        /// <summary>
        /// Context of the state.
        /// Lazyly evaluated using the context provider function.
        /// </summary>
        public T Context
        {
            get
            {
                return contextProvider != null ? contextProvider() : default;
            }
        }

        /// <summary>
        /// Clears the context provider function.
        /// </summary>
        public void ClearContextProvider() => this.contextProvider = null;

        private Func<T> contextProvider;
        internal void SetContextProvider(Func<T> contextProvider) => this.contextProvider = contextProvider;
    }

    /// <summary>
    /// Special state that can transition to any other state.
    /// </summary>
    public sealed class AnyState : State {}

    /// <summary>
    /// Struct representing no context.
    /// </summary>
    public readonly struct NoContext {}
}