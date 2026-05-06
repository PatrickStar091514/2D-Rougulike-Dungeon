using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueDungeon.Core.Events
{
    /// <summary>
    /// 无参事件回调委托
    /// </summary>
    public delegate void CallBack();

    /// <summary>
    /// 泛型单参数事件回调委托
    /// </summary>
    public delegate void CallBack<in T>(T arg);

    /// <summary>
    /// 泛型双参数事件回调委托
    /// </summary>
    public delegate void CallBack<in T, in X>(T arg1, X arg2);

    /// <summary>
    /// 基于 GameEventType 枚举 + Delegate 字典的事件中心。
    /// 支持无参、单参数、双参数三种委托签名，同一 GameEventType 绑定唯一委托类型。
    /// </summary>
    public static class EventCenter
    {
        private static readonly Dictionary<GameEventType, Delegate> _eventTable = new();
        private static bool _sceneListenerRegistered = false;

        /// <summary>
        /// 注册场景切换清理监听（首次 AddListener 时自动调用）
        /// </summary>
        private static void EnsureSceneListener()
        {
            if (_sceneListenerRegistered) return;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _sceneListenerRegistered = true;
        }

        /// <summary>
        /// 场景卸载时自动清除所有事件订阅，跨场景对象需在加载后重新注册
        /// </summary>
        private static void OnSceneUnloaded(Scene scene)
        {
            // 不在此处清空 _eventTable。
            // 场景对象由引擎销毁，在 OnDisable -> RemoveListener 中自行清理。
            // DontDestroyOnLoad 对象跨场景存活，通过 SceneManager.sceneLoaded 重新注册。
            Debug.Log($"[DEBUG] EventCenter.OnSceneUnloaded: scene={scene.name}, 当前注册事件类型数={_eventTable.Count}");
        }

        #region 无参 API

        /// <summary>
        /// 注册无参事件监听
        /// </summary>
        public static void AddListener(GameEventType eventType, CallBack handler)
        {
            CheckAddListener(eventType, handler);
            _eventTable[eventType] = (CallBack)_eventTable[eventType] + handler;
        }

        /// <summary>
        /// 移除无参事件监听
        /// </summary>
        public static void RemoveListener(GameEventType eventType, CallBack handler)
        {
            if (CheckRemoveListener(eventType, handler))
            {
                _eventTable[eventType] = (CallBack)_eventTable[eventType] - handler;
                CleanupIfNull(eventType);
            }
        }

        /// <summary>
        /// 广播无参事件
        /// </summary>
        public static void Broadcast(GameEventType eventType)
        {
            if (!_eventTable.TryGetValue(eventType, out var d)) return;

            if (d is CallBack callback)
            {
                foreach (var del in callback.GetInvocationList())
                {
                    try
                    {
                        ((CallBack)del)();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventCenter] {eventType} 处理器异常: {e}");
                    }
                }
            }
            else
            {
                Debug.LogError($"[EventCenter] {eventType} 广播类型不匹配: 期望 CallBack，实际 {d.GetType()}");
            }
        }

        #endregion

        #region 单参数泛型 API

        /// <summary>
        /// 注册泛型单参数事件监听
        /// </summary>
        public static void AddListener<T>(GameEventType eventType, CallBack<T> handler)
        {
            CheckAddListener(eventType, handler);
            _eventTable[eventType] = (CallBack<T>)_eventTable[eventType] + handler;
            Debug.Log($"[DEBUG] EventCenter.AddListener<{typeof(T).Name}>({eventType}) handler={handler.Method.DeclaringType?.Name}.{handler.Method.Name}");
        }

        /// <summary>
        /// 移除泛型单参数事件监听
        /// </summary>
        public static void RemoveListener<T>(GameEventType eventType, CallBack<T> handler)
        {
            Debug.Log($"[DEBUG] EventCenter.RemoveListener<{typeof(T).Name}>({eventType}) handler={handler.Method.DeclaringType?.Name}.{handler.Method.Name}");
            if (CheckRemoveListener(eventType, handler))
            {
                _eventTable[eventType] = (CallBack<T>)_eventTable[eventType] - handler;
                CleanupIfNull(eventType);
            }
        }

        /// <summary>
        /// 广播泛型单参数事件
        /// </summary>
        public static void Broadcast<T>(GameEventType eventType, T arg)
        {
            if (!_eventTable.TryGetValue(eventType, out var d))
            {
                Debug.Log($"[DEBUG] EventCenter.Broadcast<{typeof(T).Name}>({eventType}): 无监听者");
                return;
            }

            if (d is CallBack<T> callback)
            {
                var invocations = callback.GetInvocationList();
                Debug.Log($"[DEBUG] EventCenter.Broadcast<{typeof(T).Name}>({eventType}): 监听者数={invocations.Length}");
                foreach (var del in invocations)
                {
                    try
                    {
                        ((CallBack<T>)del)(arg);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventCenter] {eventType} 处理器异常: {e}");
                    }
                }
            }
            else
            {
                Debug.LogError($"[EventCenter] {eventType} 广播类型不匹配: 期望 CallBack<{typeof(T).Name}>，实际 {d.GetType()}");
            }
        }

        #endregion

        #region 双参数泛型 API

        /// <summary>
        /// 注册泛型双参数事件监听
        /// </summary>
        public static void AddListener<T, X>(GameEventType eventType, CallBack<T, X> handler)
        {
            CheckAddListener(eventType, handler);
            _eventTable[eventType] = (CallBack<T, X>)_eventTable[eventType] + handler;
        }

        /// <summary>
        /// 移除泛型双参数事件监听
        /// </summary>
        public static void RemoveListener<T, X>(GameEventType eventType, CallBack<T, X> handler)
        {
            if (CheckRemoveListener(eventType, handler))
            {
                _eventTable[eventType] = (CallBack<T, X>)_eventTable[eventType] - handler;
                CleanupIfNull(eventType);
            }
        }

        /// <summary>
        /// 广播泛型双参数事件
        /// </summary>
        public static void Broadcast<T, X>(GameEventType eventType, T arg1, X arg2)
        {
            if (!_eventTable.TryGetValue(eventType, out var d)) return;

            if (d is CallBack<T, X> callback)
            {
                foreach (var del in callback.GetInvocationList())
                {
                    try
                    {
                        ((CallBack<T, X>)del)(arg1, arg2);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventCenter] {eventType} 处理器异常: {e}");
                    }
                }
            }
            else
            {
                Debug.LogError($"[EventCenter] {eventType} 广播类型不匹配: 期望 CallBack<{typeof(T).Name}, {typeof(X).Name}>，实际 {d.GetType()}");
            }
        }

        #endregion

        /// <summary>
        /// 清除所有事件订阅（场景切换或测试隔离时调用）
        /// </summary>
        public static void Clear()
        {
            _eventTable.Clear();
        }

        #region 内部校验

        /// <summary>
        /// 注册前校验：确保同一 GameEventType 的委托类型一致
        /// </summary>
        private static void CheckAddListener(GameEventType eventType, Delegate handler)
        {
            EnsureSceneListener();

            if (!_eventTable.ContainsKey(eventType))
            {
                _eventTable[eventType] = null;
                return;
            }

            Delegate existing = _eventTable[eventType];
            if (existing != null && existing.GetType() != handler.GetType())
            {
                throw new InvalidOperationException(
                    $"[EventCenter] {eventType} 委托类型冲突: 已注册 {existing.GetType().Name}，尝试添加 {handler.GetType().Name}");
            }
        }

        /// <summary>
        /// 移除前校验：检查事件是否存在且委托类型匹配
        /// </summary>
        private static bool CheckRemoveListener(GameEventType eventType, Delegate handler)
        {
            if (!_eventTable.TryGetValue(eventType, out var existing))
            {
                Debug.LogWarning($"[EventCenter] RemoveListener: {eventType} 未注册任何监听");
                return false;
            }

            if (existing != null && existing.GetType() != handler.GetType())
            {
                Debug.LogWarning(
                    $"[EventCenter] RemoveListener: {eventType} 委托类型不匹配: {existing.GetType().Name} vs {handler.GetType().Name}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 移除后若委托为 null 则清理条目
        /// </summary>
        private static void CleanupIfNull(GameEventType eventType)
        {
            if (_eventTable[eventType] == null)
            {
                _eventTable.Remove(eventType);
            }
        }

        #endregion
    }
}
