using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance;

    // �ӵ������ã��������/�����ӵ�
    [System.Serializable]
    public class BulletPool
    {
        public string tag; // ����ӵ����ͣ�Player/Enemy��
        public GameObject prefab; // �ӵ�Ԥ����
        public int poolSize; // ��ʼ�ش�С
        public bool canGrow; // �ز���ʱ�Ƿ��Զ�����
        public List<GameObject> poolList; // �ӵ������б�
    }

    [SerializeField] private List<BulletPool> bulletPools;
    private Dictionary<string, BulletPool> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // 1. ��ʼ��������ֵ�
        poolDictionary = new Dictionary<string, BulletPool>();
        foreach (BulletPool thisPool in bulletPools)
        {
            poolDictionary.Add(thisPool.tag, thisPool);
            // ��ǰ������ʼ�������ӵ�������
            thisPool.poolList = new List<GameObject>();
            for (int i = 0; i < thisPool.poolSize; i++)
            {
                GameObject bullet = Instantiate(thisPool.prefab, transform);
                bullet.SetActive(false);
                bullet.tag = thisPool.tag;
                thisPool.poolList.Add(bullet);
            }
        }
    }

    // 2. �ӳ��л�ȡ�ӵ�
    public GameObject GetBulletFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"�ӵ���δ�ҵ���ǩ��{tag}");
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

        // ����������������ʱ���������ӵ�
        if (targetPool.canGrow)
        {
            GameObject newBullet = Instantiate(targetPool.prefab, position, rotation, transform);
            targetPool.poolList.Add(newBullet);
            return newBullet;
        }

        Debug.LogWarning($"��ǩ{tag}���ӵ����������Ҳ���������");
        return null;
    }

    // 3. �����ӵ������У����ã�
    public void ReturnBulletToPool(GameObject bullet, string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"�ӵ���δ�ҵ���ǩ��{tag}");
            return;
        }

        bullet.SetActive(false);
        // ��ѡ�������ӵ�״̬�����ٶȡ���ײ���ȣ�
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
    }
}
