using System;
using System.Collections.Generic;

namespace HybridStateMachine
{
    /// <summary>
    /// Interface for a state machine.
    /// </summary>
    public interface IStateMachine
    {
        /// <summary>
        /// The current active state.
        /// </summary>
        State CurrentState { get; }

        /// <summary>
        /// The previous state before the current state.
        /// It will be null if there is no previous state.
        /// </summary>
        State PreviousState { get; }

        /// <summary>
        /// A special state that matches any state.
        /// </summary>
        AnyState AnyState { get; }

        /// <summary>
        /// Event triggered when the current state changes.
        /// </summary>
        event Action<State> OnStateChanged;

        /// <summary>
        /// Sets the initial state.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void SetInitialState<T>() where T : State;

        /// <summary>
        /// Sets the initial state.
        /// </summary>
        /// <param name="state"></param>
        void SetInitialState(State state);

        /// <summary>
        /// Gets the state of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T At<T>() where T : State;

        /// <summary>
        /// Generates a Mermaid diagram string representing the state machine.
        /// </summary>
        /// <returns></returns>
        string ToMermaidString();
    }

    /// <summary>
    /// Interface for a pull-based state machine.
    /// pull-based state machines require regular updates to process transitions.
    /// </summary>
    public interface IPullStateMachine : IStateMachine
    {
        /// <summary>
        /// Updates the state machine.
        /// </summary>
        void Update();
    }

    /// <summary>
    /// Interface for a push-based state machine.
    /// push-based state machines process transitions in response to events.
    /// </summary>
    public interface IPushStateMachine : IStateMachine
    {
        /// <summary>
        /// Fires an event to trigger transitions.
        /// </summary>
        /// <param name="e"></param>
        void Fire(int e);
    }

    /// <summary>
    /// Interface for a state machine that supports both push and pull mechanisms.
    /// </summary>
    public interface IPushAndPullStateMachine : IPullStateMachine, IPushStateMachine
    {
    }

    /// <summary>
    /// Interface to hold enum type information for event-based state machines.
    /// </summary>
    internal interface IEnumTypeHolder
    {
        Type EnumType { get; }
        void SetEnumType(Type enumType);
    }

    /// <summary>
    /// Extension methods for IPushStateMachine.
    /// If it is not a push state machine, the Fire method will be ignored.
    /// </summary>
    public static class IPushStateMachineExtensions
    {
        /// <summary>
        /// Fires an event using an enum type.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="stateMachine"></param>
        /// <param name="eventId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Fire<TEvent>(this IStateMachine stateMachine, TEvent eventId) where TEvent : Enum
        {
            if (stateMachine is not IPushStateMachine pushStateMachine)
            {
                return;
            }

            if (stateMachine is IEnumTypeHolder enumTypeHolder)
            {
                if (enumTypeHolder.EnumType != null && enumTypeHolder.EnumType != typeof(TEvent))
                {
                    throw new InvalidOperationException($"Event type mismatch. Expected: {enumTypeHolder.EnumType.Name}, Actual: {typeof(TEvent).Name}");
                }
            }
            pushStateMachine.Fire(eventId.GetHashCode());
        }
    }

    /// <summary>
    /// Static class for creating state machines.
    /// </summary>
    public static class StateMachine
    {
        /// <summary>
        /// Creates a pull-based state machine.
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IPullStateMachine Create(StateFactory factory = null)
        {
            factory ??= new StateFactory();
            return new PullStateMachine(factory);
        }

        /// <summary>
        /// Creates a push-and-pull-based state machine.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IPushAndPullStateMachine Create<TEvent>(StateFactory factory = null) where TEvent : Enum
        {
            factory ??= new StateFactory();
            var sm = new PushAndPullStateMachine(factory);
            sm.SetEnumType(typeof(TEvent));
            return sm;
        }
    }

    /// <summary>
    /// Factory class for creating states.
    /// </summary>
    public sealed class StateFactory
    {
        private readonly Dictionary<Type, Func<State>> factories = new();
        private readonly Dictionary<Type, State> stateCache = new();

        /// <summary>
        /// Gets the cached states.
        /// </summary>
        public IEnumerable<State> CachedStates => stateCache.Values;

        /// <summary>
        /// Clears the state cache.
        /// </summary>
        public void ClearCache() => stateCache.Clear();

        /// <summary>
        /// Enables or disables automatic state creation.
        /// If enabled, states not registered in the factory will be created using their parameterless constructor.
        /// If disabled, an exception will be thrown when trying to create an unregistered state.
        /// </summary>
        public bool IsAutoCreateEnabled { get; set; } = true;

        /// <summary>
        /// Registers a state factory for the specified state type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        public void Register<T>(Func<State> factory) where T : State
        {
            factories[typeof(T)] = factory;
        }

        internal T GetState<T>() where T : State
        {
            var stateType = typeof(T);
            if (stateCache.TryGetValue(stateType, out var cachedState))
            {
                return cachedState as T;
            }

            return CreateState<T>();
        }

        internal T CreateState<T>() where T : State
        {
            var stateType = typeof(T);
            if (factories.TryGetValue(stateType, out var factory))
            {
                var newState = factory() ?? throw new InvalidOperationException($"Factory for state type {stateType.Name} returned null.");
                stateCache[stateType] = newState;
                return newState as T;
            }

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
                return autoCreatedState as T;
            }
            
            throw new InvalidOperationException($"State of type {stateType.Name} is not registered in the StateFactory.");   
        }
    }
}
