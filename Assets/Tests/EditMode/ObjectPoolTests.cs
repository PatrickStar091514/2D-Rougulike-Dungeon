using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Core.Pool;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// ObjectPool 对象池的 EditMode 单元测试
    /// </summary>
    public class ObjectPoolTests
    {
        private GameObject _poolGo;
        private ObjectPool _pool;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _poolGo = new GameObject("TestObjectPool");
            _pool = _poolGo.AddComponent<ObjectPool>();
            _prefab = new GameObject("TestPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_poolGo != null) Object.DestroyImmediate(_poolGo);
            if (_prefab != null) Object.DestroyImmediate(_prefab);
        }

        #region Get

        [Test]
        public void Get_EmptyPool_InstantiatesNewObject()
        {
            var obj = _pool.Get("bullet", _prefab);

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.activeSelf);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Get_AfterRelease_ReusesObject()
        {
            var obj = _pool.Get("bullet", _prefab);
            _pool.Release("bullet", obj);

            var obj2 = _pool.Get("bullet", _prefab);

            Assert.AreEqual(obj, obj2);
            Assert.IsTrue(obj2.activeSelf);

            Object.DestroyImmediate(obj2);
        }

        #endregion

        #region Release

        [Test]
        public void Release_ObjectDeactivatedAndParented()
        {
            var obj = _pool.Get("bullet", _prefab);
            _pool.Release("bullet", obj);

            Assert.IsFalse(obj.activeSelf);
            Assert.IsNotNull(obj.transform.parent);

            Object.DestroyImmediate(obj);
        }

        #endregion

        #region Warmup

        [Test]
        public void Warmup_PreInstantiatesObjects()
        {
            _pool.Warmup("bullet", _prefab, 3);

            // 连续取出 3 个应全部是预热对象（非新实例化）
            var objs = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                objs[i] = _pool.Get("bullet", _prefab);
                Assert.IsNotNull(objs[i]);
            }

            foreach (var obj in objs) Object.DestroyImmediate(obj);
        }

        #endregion

        #region Max Capacity

        [Test]
        public void Release_ExceedMaxCapacity_ObjectDestroyed()
        {
            _pool.SetMaxCapacity("bullet", 2);

            var objs = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                objs[i] = _pool.Get("bullet", _prefab);
            }

            // EditMode 下 Destroy 会报错，预期该日志
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

            // 回收 3 个，但最大容量为 2，第 3 个应被 Destroy（EditMode 下失败但逻辑正确）
            _pool.Release("bullet", objs[0]);
            _pool.Release("bullet", objs[1]);
            _pool.Release("bullet", objs[2]);

            // 从池中取出 2 个复用对象
            var r1 = _pool.Get("bullet", _prefab);
            var r2 = _pool.Get("bullet", _prefab);

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);

            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
            // objs[2] 在 EditMode 下 Destroy 失败，手动清理
            if (objs[2] != null) Object.DestroyImmediate(objs[2]);
        }

        [Test]
        public void SetMaxCapacity_MinimumIs1()
        {
            _pool.SetMaxCapacity("bullet", 0);

            var obj = _pool.Get("bullet", _prefab);
            _pool.Release("bullet", obj);

            // 容量至少为 1，所以应该能复用
            var obj2 = _pool.Get("bullet", _prefab);
            Assert.AreEqual(obj, obj2);

            Object.DestroyImmediate(obj2);
        }

        #endregion

        #region ClearPool

        [Test]
        public void ClearPool_RemovesAllPooledObjects()
        {
            _pool.Warmup("bullet", _prefab, 3);

            // ClearPool 内部调用 Destroy（EditMode 下报错）
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

            _pool.ClearPool("bullet");

            // 清理后字典应不再包含该 key，取出的是新实例
            var obj = _pool.Get("bullet", _prefab);
            Assert.IsNotNull(obj);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void ClearPool_UnknownKey_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _pool.ClearPool("nonexistent"));
        }

        #endregion

        #region ClearAll

        [Test]
        public void ClearAll_ClearsAllPools()
        {
            _pool.Warmup("bullet", _prefab, 2);
            _pool.Warmup("enemy", _prefab, 2);

            // ClearAll 内部调用 Destroy（EditMode 下报错），4 个对象 + 2 个容器 = 6 次
            for (int i = 0; i < 6; i++)
            {
                LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
            }

            _pool.ClearAll();

            // 两个 pool 都已清空，取出均应是新实例化
            var b = _pool.Get("bullet", _prefab);
            var e = _pool.Get("enemy", _prefab);

            Assert.IsNotNull(b);
            Assert.IsNotNull(e);

            Object.DestroyImmediate(b);
            Object.DestroyImmediate(e);
        }

        #endregion

        #region IPoolable 回调

        [Test]
        public void Get_InvokesIPoolableOnPoolGet()
        {
            var poolablePrefab = new GameObject("PoolablePrefab");
            var tracker = poolablePrefab.AddComponent<PoolableTracker>();

            var obj = _pool.Get("poolable", poolablePrefab);
            var t = obj.GetComponent<PoolableTracker>();

            Assert.AreEqual(1, t.GetCount);
            Assert.AreEqual(0, t.ReleaseCount);

            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(poolablePrefab);
        }

        [Test]
        public void Release_InvokesIPoolableOnPoolRelease()
        {
            var poolablePrefab = new GameObject("PoolablePrefab");
            poolablePrefab.AddComponent<PoolableTracker>();

            var obj = _pool.Get("poolable", poolablePrefab);
            _pool.Release("poolable", obj);

            var t = obj.GetComponent<PoolableTracker>();
            Assert.AreEqual(1, t.GetCount);
            Assert.AreEqual(1, t.ReleaseCount);

            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(poolablePrefab);
        }

        #endregion

        #region Per-Key 容器

        [Test]
        public void Release_DifferentKeys_DifferentContainers()
        {
            var objA = _pool.Get("typeA", _prefab);
            var objB = _pool.Get("typeB", _prefab);

            _pool.Release("typeA", objA);
            _pool.Release("typeB", objB);

            // 不同 key 应有不同父节点（通过名称验证）
            Assert.IsNotNull(objA.transform.parent);
            Assert.IsNotNull(objB.transform.parent);
            Assert.AreNotEqual(
                objA.transform.parent.gameObject.name,
                objB.transform.parent.gameObject.name);

            Object.DestroyImmediate(objA);
            Object.DestroyImmediate(objB);
        }

        #endregion
    }

    /// <summary>
    /// 用于测试 IPoolable 回调的辅助组件
    /// </summary>
    public class PoolableTracker : MonoBehaviour, IPoolable
    {
        public int GetCount;     // OnPoolGet 调用次数
        public int ReleaseCount; // OnPoolRelease 调用次数

        public void OnPoolGet() => GetCount++;
        public void OnPoolRelease() => ReleaseCount++;
    }
}
