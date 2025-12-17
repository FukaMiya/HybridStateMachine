using System;

namespace HybridStateMachine
{
    /// <summary>
    /// Implementation of a state machine that supports both push and pull mechanisms.
    /// </summary>
    internal sealed class PushAndPullStateMachine : PullStateMachine, IPushAndPullStateMachine, IEnumTypeHolder
    {
        public PushAndPullStateMachine(StateFactory factory) : base(factory)
        {
            AnyState = new AnyState();
            AnyState.SetStateMachine(this);
        }

        public void Fire(int eventId)
        {
            if (AnyState.CheckTransition(eventId, out var nextState) ||
                CurrentState.CheckTransition(eventId, out nextState))
            {
                ChangeState(nextState);
                return;
            }
        }

        public Type EnumType { get; private set; }
        public void SetEnumType(Type enumType)
        {
            EnumType = enumType;
        }
    }

}