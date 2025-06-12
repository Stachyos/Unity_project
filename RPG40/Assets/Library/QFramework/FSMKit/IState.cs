using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public interface IState
    {
        bool Condition();
        void Enter();
        void Update();
        void ServerUpdate();
        void ClientUpdate();
        void FixedUpdate();
        void OnGUI();
        void Exit();
    }


    public class CustomState : IState
    {
        private Func<bool> mOnCondition;
        private Action mOnEnter;
        private Action mOnUpdate;
        private Action mOnClientUpdate;
        private Action mOnServerUpdate;
        private Action mOnFixedUpdate;
        private Action mOnGUI;
        private Action mOnExit;

        public CustomState OnCondition(Func<bool> onCondition)
        {
            mOnCondition = onCondition;
            return this;
        }

        public CustomState OnEnter(Action onEnter)
        {
            mOnEnter = onEnter;
            return this;
        }


        public CustomState OnUpdate(Action onUpdate)
        {
            mOnUpdate = onUpdate;
            return this;
        }

        public CustomState OnFixedUpdate(Action onFixedUpdate)
        {
            mOnFixedUpdate = onFixedUpdate;
            return this;
        }

        public CustomState OnGUI(Action onGUI)
        {
            mOnGUI = onGUI;
            return this;
        }

        public CustomState OnExit(Action onExit)
        {
            mOnExit = onExit;
            return this;
        }

        public CustomState OnClientUpdate(Action onClientUpdate)
        {
            mOnClientUpdate = onClientUpdate;
            return this;
        }

        public CustomState OnServerUpdate(Action onServerUpdate)
        {
            mOnServerUpdate = onServerUpdate;
            return this;
        }


        public bool Condition()
        {
            var result = mOnCondition?.Invoke();
            return result == null || result.Value;
        }

        public void Enter()
        {
            mOnEnter?.Invoke();
        }


        public void Update()
        {
            mOnUpdate?.Invoke();
        }

        public void ClientUpdate()
        {
            mOnClientUpdate?.Invoke();
        }

        public void ServerUpdate()
        {
            mOnServerUpdate?.Invoke();
        }

        public void FixedUpdate()
        {
            mOnFixedUpdate?.Invoke();
        }


        public void OnGUI()
        {
            mOnGUI?.Invoke();
        }

        public void Exit()
        {
            mOnExit?.Invoke();
        }
    }

    public class EasyFSM<T>
    {
        protected Dictionary<T, IState> mStates = new Dictionary<T, IState>();

        public void AddState(T id, IState state)
        {
            mStates.Add(id, state);
        }


        public CustomState State(T t)
        {
            if (mStates.ContainsKey(t))
            {
                return mStates[t] as CustomState;
            }

            var state = new CustomState();
            mStates.Add(t, state);
            return state;
        }

        private IState mCurrentState;
        private T mCurrentStateId;

        public IState CurrentState => mCurrentState;
        public T CurrentStateId => mCurrentStateId;
        public T PreviousStateId { get; private set; }

        public long FrameCountOfCurrentState = 1;
        public float SecondsOfCurrentState = 0.0f;

        public void ChangeState(T t)
        {
            if (t.Equals(CurrentStateId)) return;

            if (mStates.TryGetValue(t, out var state))
            {
                if (mCurrentState != null && state.Condition())
                {
                    mCurrentState.Exit();
                    PreviousStateId = mCurrentStateId;
                    mCurrentState = state;
                    mCurrentStateId = t;
                    mOnStateChanged?.Invoke(PreviousStateId, CurrentStateId);
                    FrameCountOfCurrentState = 1;
                    SecondsOfCurrentState = 0.0f;
                    mCurrentState.Enter();
                }
            }


        }

        private Action<T, T> mOnStateChanged = (_, __) => { };

        public void OnStateChanged(Action<T, T> onStateChanged)
        {
            mOnStateChanged += onStateChanged;
        }

        public void StartState(T t)
        {
            if (mStates.TryGetValue(t, out var state))
            {
                PreviousStateId = t;
                mCurrentState = state;
                mCurrentStateId = t;
                FrameCountOfCurrentState = 0;
                SecondsOfCurrentState = 0.0f;
                state.Enter();
            }
        }

        public void FixedUpdate()
        {
            mCurrentState?.FixedUpdate();
        }

        public void Update()
        {
            mCurrentState?.Update();
            FrameCountOfCurrentState++;
            SecondsOfCurrentState += Time.deltaTime;
        }

        public void ServerUpdate()
        {
            mCurrentState?.ServerUpdate();
        }

        public void ClientUpdate()
        {
            mCurrentState?.ClientUpdate();
        }

        public void OnGUI()
        {
            mCurrentState?.OnGUI();
        }

        public void Clear()
        {
            mCurrentState = null;
            mCurrentStateId = default;
            mStates.Clear();
        }
    }

    public abstract class AbstractState<TStateId, TTarget> : IState
    {
        protected EasyFSM<TStateId> _fsm;
        protected TTarget _target;

        public AbstractState(EasyFSM<TStateId> easyFsm, TTarget target)
        {
            _fsm = easyFsm;
            _target = target;
        }


        bool IState.Condition()
        {
            return OnCondition();
            ;
        }

        void IState.Enter()
        {
            OnEnter();
        }

        void IState.Update()
        {
            OnUpdate();
        }

        public void ServerUpdate()
        {
            OnServerUpdate();
        }

        public void ClientUpdate()
        {
            OnClientUpdate();
        }

        void IState.FixedUpdate()
        {
            OnFixedUpdate();
        }

        public virtual void OnGUI()
        {
        }

        void IState.Exit()
        {
            OnExit();
        }

        protected virtual bool OnCondition() => true;

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnExit()
        {
        }

        protected virtual void OnServerUpdate()
        {
            
        }

        protected virtual void OnClientUpdate()
        {
            
        }
    }
}