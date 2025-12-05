using System;
using System.Collections.Generic;

namespace FukaMiya.Utils
{
    // public interface IState
    // {
    //     public StateMachine StateMachine { get; }
    //     public IReadOnlyList<ITransition> GetTransitions { get; }

    //     public void AddTransition(ITransition transition);
    //     public void Enter();
    //     public void Exit();
    //     public void Update();
    //     public bool IsStateOf<T>() where T : IState;
    //     public bool IsStateOf(Type type);
    // }

    public abstract class State
    {
        public StateMachine StateMachine { get; private set; }
        public void Setup(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        protected readonly List<ITransition> transitions = new();
        public IReadOnlyList<ITransition> GetTransitions => transitions.AsReadOnly();

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate() { }

        public void Enter()
        {
            OnEnter();
        }

        public void Exit()
        {
            OnExit();
        }

        public void Update()
        {
            OnUpdate();
        }

        public virtual bool CheckTransitionTo(out State nextState)
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

                    if (maxWeightToState == null || transition.Params.Weight > maxWeight)
                    {
                        maxWeightToState = toState;
                        maxWeight = transition.Params.Weight;
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

        public void AddTransition(ITransition transition)
        {
            if (transitions.Contains(transition))
            {
                throw new InvalidOperationException("Transition already exists in this state.");
            }
            transitions.Add(transition);
        }

        public bool IsStateOf<T>() where T : State => this is T;
        public bool IsStateOf(Type type) => GetType() == type;

        public override string ToString() => GetType().Name;
    }

    public abstract class State<T> : State
    {
        public T Context
        {
            get
            {
                return contextProvider != null ? contextProvider() : default;
            }
        }
        private Func<T> contextProvider;
        public void SetContextProvider(Func<T> contextProvider) => this.contextProvider = contextProvider;

        public override bool CheckTransitionTo(out State nextState)
        {
            State maxWeightToState = null;
            ITransition maxWeightTransition = null;
            float maxWeight = float.MinValue;
            foreach (var transition in transitions)
            {
                if (transition.Condition == null || transition.Condition())
                {
                    var toState = transition.GetToState();
                    if (toState == null) continue;
                    if (!transition.Params.IsReentryAllowed && StateMachine.CurrentState.IsStateOf(toState.GetType())) continue;

                    if (maxWeightToState == null || transition.Params.Weight > maxWeight)
                    {
                        maxWeightToState = toState;
                        maxWeightTransition = transition;
                        maxWeight = transition.Params.Weight;
                    }
                }
            }

            if (maxWeightToState != null)
            {
                nextState = maxWeightToState;
                if (maxWeightTransition is Transition<T> typedTransition)
                {
                    SetContextProvider(typedTransition.GetContext);
                }
                return true;
            }

            nextState = null;
            return false;
        }
    }

    public sealed class AnyState : State
    {
    }

    public sealed class NoContext
    {
    }
}