using System;
using System.Collections.Generic;

namespace RPGSystem.Core
{
    /// <summary>
    /// 제네릭 이벤트 버스.
    /// 시스템 간 직접 참조 없이 이벤트로 통신한다.
    /// UI는 이 이벤트를 구독하여 갱신한다.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

        /// <summary>이벤트 구독</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_events.TryGetValue(type, out var existing))
                _events[type] = Delegate.Combine(existing, handler);
            else
                _events[type] = handler;
        }

        /// <summary>이벤트 구독 해제</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_events.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                    _events.Remove(type);
                else
                    _events[type] = result;
            }
        }

        /// <summary>이벤트 발행</summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            if (_events.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(eventData);
        }

        /// <summary>모든 이벤트 구독 해제 (씬 전환 시)</summary>
        public static void Clear()
        {
            _events.Clear();
        }
    }
}
