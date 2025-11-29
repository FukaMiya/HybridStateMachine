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
            if (CurrentState.IsStateOf(nextState.GetType()))
            {
                return;
            }

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
            return TransitionBuilder.To(from, () => 
            {
                UnityEngine.Debug.Log($"Returning to previous state: {from.StateMachine.PreviousState}");
                return from.StateMachine.PreviousState;
            });
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
            Transition maxWeightTransition = null;
            foreach (var transition in transitions)
            {
                if (transition.Condition == null || transition.Condition())
                {
                    if (maxWeightTransition == null || transition.Weight > maxWeightTransition.Weight)
                    {
                        maxWeightTransition = transition;
                    }
                }
            }

            if (maxWeightTransition != null)
            {
                nextState = maxWeightTransition.GetToState();
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
