using System;
using System.Collections.Generic;
using System.Text;

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

            CurrentState.Update();
        }

        void ChangeState(State nextState)
        {
            CurrentState.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void SetInitialState<T>() where T : State, new()
        {
            CurrentState = At<T>();
            CurrentState.Enter();
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

        public string ToMermaidString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("stateDiagram-v2");
            foreach (var state in states.Values)
            {
                foreach (var t in state.GetTransitions)
                {
                    var toState = t.GetToState();
                    sb.AppendLine($"    {state} --> {(toState == null ? "AnyState" : toState.ToString())}");
                }
            }
            return sb.ToString();
        }
    }

    public static class StateExtensions
    {
        public static ITransitionStarter<NoContext> To<T>(this State from) where T : State, new()
        {
            return TransitionBuilder<NoContext>.To(from, from.StateMachine.At<T>(), null);
        }

        public static ITransitionStarter<NoContext> To(this State from, State to)
        {
            return TransitionBuilder<NoContext>.To(from, to, null);
        }

        public static ITransitionStarter<TContext> To<T, TContext>(this State from, Func<TContext> context) where T : State<TContext>, new()
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
            return TransitionBuilder<NoContext>.To(from, () => from.StateMachine.PreviousState, null);
        }
    }
}
