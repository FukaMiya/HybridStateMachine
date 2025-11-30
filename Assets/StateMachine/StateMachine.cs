using System;
using System.Collections.Generic;

namespace FukaMiya.Utils
{
    public sealed class StateMachine
    {
        public State CurrentState { get; private set; }
        public State PreviousState { get; private set; }

        private readonly Dictionary<Type, State> states = new();
        public AnyState AnyState { get; }

        public StateMachine()
        {
            AnyState = new AnyState();
            AnyState.Setup(this);
            states[typeof(AnyState)] = AnyState;
        }

        public void Update()
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException("CurrentState is not set. Please set the initial state using SetInitialState<T>() method.");
            }

            if (AnyState.CheckTransitionTo(out var nextState) ||
                CurrentState.CheckTransitionTo(out nextState))
            {
                ChangeState(nextState);
                return;
            }

            CurrentState.OnUpdate();
        }

        void ChangeState(State nextState)
        {
            CurrentState.OnExit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.OnEnter();
        }

        public void SetInitialState<T>() where T : State, new()
        {
            CurrentState = At<T>();
            CurrentState.OnEnter();
        }

        public State At<T>() where T : State, new()
        {
            if (states.TryGetValue(typeof(T), out var state))
            {
                return state;
            }

            state = CreateStateInstance<T>();
            states[typeof(T)] = state;
            return state;
        }

        State CreateStateInstance<T>() where T : State, new()
        {
            T instance = new ();
            instance.Setup(this);
            return instance;
        }
    }

    public static class StateExtensions
    {
        public static ITransitionStarter To<T>(this State from) where T : State, new()
        {
            return TransitionBuilder.To(from, from.StateMachine.At<T>());
        }

        public static ITransitionStarter Back(this State from)
        {
            return TransitionBuilder.To(from, () => from.StateMachine.PreviousState);
        }
    }

    public abstract class State
    {
        public StateMachine StateMachine { get; private set; }
        public void Setup(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        private readonly HashSet<Transition> transitions = new();

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }

        public bool CheckTransitionTo(out State nextState)
        {
            State maxWeightToState = null;
            float maxWeight = float.MinValue;
            foreach (var transition in transitions)
            {
                if (transition.Condition == null || transition.Condition())
                {
                    var toState = transition.GetToState();
                    if (toState == null) continue;
                    if (!transition.Params.IsReentryAllowed && StateMachine.CurrentState.IsStateOf(toState.GetType())) continue;

                    if (maxWeightToState == null || transition.Weight > maxWeight)
                    {
                        maxWeightToState = toState;
                        maxWeight = transition.Weight;
                    }
                }
            }

            if (maxWeightToState != null)
            {
                nextState = maxWeightToState;
                return true;
            }

            nextState = null;
            return false;
        }

        public void AddTransition(Transition transition)
        {
            transitions.Add(transition);
        }

        public bool IsStateOf<T>() where T : State => this is T;
        public bool IsStateOf(Type type) => GetType() == type;

        public override string ToString() => GetType().Name;
    }

    public sealed class AnyState : State
    {
    }
}
