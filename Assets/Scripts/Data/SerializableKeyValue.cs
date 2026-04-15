namespace RogueDungeon.Data
{
    /// <summary>
    /// 可序列化的键值对，替代 Dictionary 用于 JsonUtility 序列化
    /// </summary>
    [System.Serializable]
    public class SerializableKeyValue<TKey, TValue>
    {
        public TKey Key;   // 键
        public TValue Value; // 值

        public SerializableKeyValue() { }

        public SerializableKeyValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
