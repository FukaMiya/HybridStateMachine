using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FukaMiya.Utils
{
    public interface IStateMachine
    {
        State CurrentState { get; }
        State PreviousState { get; }
        AnyState AnyState { get; }
        void SetInitialState<T>() where T : State;
        State At<T>() where T : State;
        string ToMermaidString();
    }

    public interface IPullStateMachine : IStateMachine
    {
        void Update();
    }

    internal interface EnumTypeHolder
    {
        Type EnumType { get; }
        void SetEnumType(Type enumType);
    }

    public interface IPushStateMachine : IStateMachine
    {
        void Fire(int e);
    }

    public static class IPushStateMachineExtensions
    {
        public static void Fire<TEvent>(this IPushStateMachine stateMachine, TEvent eventId) where TEvent : Enum
        {
            if (stateMachine is EnumTypeHolder enumTypeHolder)
            {
                if (enumTypeHolder.EnumType != null && enumTypeHolder.EnumType != typeof(TEvent))
                {
                    throw new InvalidOperationException($"Event type mismatch. Expected: {enumTypeHolder.EnumType.Name}, Actual: {typeof(TEvent).Name}");
                }
            }
            stateMachine.Fire(eventId.GetHashCode());
        }
    }

    public interface IPushAndPullStateMachine : IPullStateMachine, IPushStateMachine
    {
    }

    public static class StateMachine
    {
        public static IPullStateMachine Create(StateFactory factory)
        {
            return new PullStateMachine(factory);
        }

        public static IPushAndPullStateMachine Create<TEvent>(StateFactory factory) where TEvent : Enum
        {
            var sm = new PushAndPullStateMachine(factory);
            sm.SetEnumType(typeof(TEvent));
            return sm;
        }
    }

    public sealed class StateFactory
    {
        private readonly Dictionary<Type, Func<State>> factories;
        private readonly Dictionary<Type, State> stateCache = new();
        public IReadOnlyList<State> CachedStates => new List<State>(stateCache.Values);
        public bool IsAutoCreateEnabled { get; set; } = true;

        public StateFactory()
        {
            factories = new Dictionary<Type, Func<State>>();
        }
        public StateFactory(Dictionary<Type, Func<State>> factories)
        {
            this.factories = factories;
        }

        public void Register<T>(Func<State> factory) where T : State
        {
            factories[typeof(T)] = factory;
        }

        public State CreateState<T>() where T : State
        {
            var stateType = typeof(T);
            if (stateCache.TryGetValue(stateType, out var cachedState))
            {
                return cachedState;
            }

            if (!factories.ContainsKey(stateType))
            {
                if (IsAutoCreateEnabled)
                {
                    State autoCreatedState;
                    try
                    {
                        autoCreatedState = Activator.CreateInstance(stateType) as State;
                    }
                    catch (MissingMethodException)
                    {
                        throw new InvalidOperationException($"Failed to auto-create {stateType.Name}. It requires a parameterless constructor.");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"An error occurred while creating {stateType.Name}: {ex.Message}", ex);
                    }

                    stateCache[stateType] = autoCreatedState;
                    return autoCreatedState;
                }
                else
                {
                    throw new InvalidOperationException($"State of type {stateType.Name} is not registered in the StateFactory.");   
                }
            }

            var newState = factories[stateType]() ?? throw new InvalidOperationException($"Factory for state type {stateType.Name} returned null.");
            stateCache[stateType] = newState;
            return newState;
        }

        public void ClearCache() => stateCache.Clear();
    }
}
