using System.Collections.Generic;
using UnityEngine;

namespace RogueDungeon.Core.Pool
{
    /// <summary>
    /// 通用 GameObject 对象池，MonoBehaviour 单例实现。
    /// 支持按 key 分组的 Get/Release/Warmup 操作，每个 key 拥有独立的父节点容器。
    /// 获取/回收时触发 IPoolable 回调。
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private const int DefaultMaxCapacity = 64; // 默认最大容量

        private readonly Dictionary<string, Queue<GameObject>> _pools = new(); // key → 可用对象队列
        private readonly Dictionary<string, Transform> _containers = new();    // key → 独立父节点容器
        private readonly Dictionary<string, int> _maxCapacities = new();       // key → 最大容量

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 从池中获取对象。若池中有可用对象则复用，否则实例化新对象。
        /// 获取后自动调用所有 IPoolable.OnPoolGet() 回调。
        /// </summary>
        public GameObject Get(string key, GameObject prefab)
        {
            GameObject obj;

            if (_pools.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                obj = queue.Dequeue();
                obj.transform.SetParent(null);
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(prefab);
            }

            NotifyPoolGet(obj);
            return obj;
        }

        /// <summary>
        /// 回收对象入池。若池已满则直接销毁。
        /// 回收前自动调用所有 IPoolable.OnPoolRelease() 回调。
        /// </summary>
        public void Release(string key, GameObject obj)
        {
            NotifyPoolRelease(obj);

            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }

            int maxCap = _maxCapacities.TryGetValue(key, out var cap) ? cap : DefaultMaxCapacity;

            if (_pools[key].Count >= maxCap)
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(GetContainer(key));
            _pools[key].Enqueue(obj);
        }

        /// <summary>
        /// 预热：预先实例化指定数量的对象并入池，减少运行时 Instantiate 开销
        /// </summary>
        public void Warmup(string key, GameObject prefab, int count)
        {
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }

            Transform container = GetContainer(key);

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(container);
                _pools[key].Enqueue(obj);
            }
        }

        /// <summary>
        /// 设置指定 key 的池最大容量，超过时 Release 将直接销毁对象
        /// </summary>
        public void SetMaxCapacity(string key, int maxCapacity)
        {
            _maxCapacities[key] = Mathf.Max(1, maxCapacity);
        }

        /// <summary>
        /// 清空指定 key 的池并销毁所有对象及其容器
        /// </summary>
        public void ClearPool(string key)
        {
            if (_pools.TryGetValue(key, out var queue))
            {
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }

                _pools.Remove(key);
            }

            if (_containers.TryGetValue(key, out var container))
            {
                if (container != null) Destroy(container.gameObject);
                _containers.Remove(key);
            }

            _maxCapacities.Remove(key);
        }

        /// <summary>
        /// 清空所有池并销毁所有对象和容器
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
                while (kvp.Value.Count > 0)
                {
                    var obj = kvp.Value.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }

            _pools.Clear();

            foreach (var kvp in _containers)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }

            _containers.Clear();
            _maxCapacities.Clear();
        }

        /// <summary>
        /// 获取或创建指定 key 的容器节点
        /// </summary>
        private Transform GetContainer(string key)
        {
            if (_containers.TryGetValue(key, out var container) && container != null)
            {
                return container;
            }

            var go = new GameObject($"[Pool:{key}]");
            go.transform.SetParent(transform);
            go.SetActive(false);
            _containers[key] = go.transform;
            return go.transform;
        }

        /// <summary>
        /// 通知对象上所有 IPoolable 组件执行获取回调
        /// </summary>
        private static void NotifyPoolGet(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables)
            {
                p.OnPoolGet();
            }
        }

        /// <summary>
        /// 通知对象上所有 IPoolable 组件执行回收回调
        /// </summary>
        private static void NotifyPoolRelease(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables)
            {
                p.OnPoolRelease();
            }
        }
    }
}
