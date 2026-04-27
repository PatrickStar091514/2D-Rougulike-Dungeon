using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance;

    // 子弹池配置：区分玩家/敌人子弹
    [System.Serializable]
    public class BulletPool
    {
        public string tag; // 标记子弹类型（Player/Enemy）
        public GameObject prefab; // 子弹预制体
        public int poolSize; // 初始池大小
        public bool canGrow; // 池不够时是否自动扩容
        public List<GameObject> poolList; // 子弹对象列表
    }

    [SerializeField] private List<BulletPool> bulletPools;
    private Dictionary<string, BulletPool> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // 1. 初始化对象池字典
        poolDictionary = new Dictionary<string, BulletPool>();
        foreach (BulletPool thisPool in bulletPools)
        {
            poolDictionary.Add(thisPool.tag, thisPool);
            // 提前创建初始数量的子弹并禁用
            thisPool.poolList = new List<GameObject>();
            for (int i = 0; i < thisPool.poolSize; i++)
            {
                GameObject bullet = Instantiate(thisPool.prefab, transform);
                bullet.SetActive(false);
                thisPool.poolList.Add(bullet);
            }
        }
    }

    // 2. 从池中获取子弹
    public GameObject GetBulletFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"子弹池未找到标签：{tag}");
                return null;
        }

        BulletPool targetPool = poolDictionary[tag];
        foreach(GameObject bullet in targetPool.poolList)
        {
            if (!bullet.activeInHierarchy)
            {
                bullet.transform.position = position;
                bullet.transform.rotation = rotation;
                bullet.SetActive(true);
                return bullet;
            }
        }

        // 池已满且允许扩容时，创建新子弹
        if (targetPool.canGrow)
        {
            GameObject newBullet = Instantiate(targetPool.prefab, position, rotation, transform);
            targetPool.poolList.Add(newBullet);
            return newBullet;
        }

        Debug.LogWarning($"标签{tag}的子弹池已满，且不允许扩容");
        return null;
    }

    // 3. 回收子弹到池中（禁用）
    public void ReturnBulletToPool(GameObject bullet, string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"子弹池未找到标签：{tag}");
            return;
        }

        bullet.SetActive(false);
        // 可选：重置子弹状态（如速度、碰撞器等）
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
    }
}
