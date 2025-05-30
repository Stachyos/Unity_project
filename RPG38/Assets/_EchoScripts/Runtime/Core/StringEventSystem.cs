using System;
using System.Collections.Generic;

namespace GameLogic
{
    public class StringEventSystem
    {
        public static readonly StringEventSystem Global = new StringEventSystem();

        private Dictionary<string, IEasyEvent> mEvents = new Dictionary<string, IEasyEvent>();

        public IUnRegister Register(string key, Action onEvent)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent;
                return easyEvent.Register(onEvent);
            }
            else
            {
                var easyEvent = new EasyEvent();
                mEvents.Add(key, easyEvent);
                return easyEvent.Register(onEvent);
            }
        }

        public void UnRegister(string key, Action onEvent)
        {

            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent;
                easyEvent?.UnRegister(onEvent);
            }
        }

        public void Send(string key)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent;
                easyEvent?.Trigger();
            }
        }


        public IUnRegister Register<T>(string key, Action<T> onEvent)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T>;
                return easyEvent.Register(onEvent);
            }
            else
            {
                var easyEvent = new EasyEvent<T>();
                mEvents.Add(key, easyEvent);
                return easyEvent.Register(onEvent);
            }
        }
        
        public IUnRegister Register<T,K>(string key, Action<T,K> onEvent)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T,K>;
                return easyEvent.Register(onEvent);
            }
            else
            {
                var easyEvent = new EasyEvent<T,K>();
                mEvents.Add(key, easyEvent);
                return easyEvent.Register(onEvent);
            }
        }
        
        public void UnRegister<T,K>(string key, Action<T,K> onEvent)
        {

            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T,K>;
                easyEvent?.UnRegister(onEvent);
            }
        }


        public void UnRegister<T>(string key, Action<T> onEvent)
        {

            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T>;
                easyEvent?.UnRegister(onEvent);
            }
        }

        public void Send<T>(string key, T data)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T>;
                easyEvent?.Trigger(data);
            }
        }
        
        public void Send<T,K>(string key, T data,K data2)
        {
            if (mEvents.TryGetValue(key, out var e))
            {
                var easyEvent = e as EasyEvent<T,K>;
                easyEvent?.Trigger(data,data2);
            }
        }
    }
}