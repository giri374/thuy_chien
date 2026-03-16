using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance;

    [System.Serializable]
    public struct PoolConfig
    {
        public EffectType type;
        public GameObject prefab;
        public int defaultSize;
        public int maxSize;
    }

    [SerializeField] private List<PoolConfig> configs;

    // Dictionary lưu trữ Pool cho từng loại Effect
    private Dictionary<EffectType, IObjectPool<GameObject>> poolDict = new Dictionary<EffectType, IObjectPool<GameObject>>();

    void Awake()
    {
        Instance = this;
        InitializePools();
    }

    void InitializePools()
    {
        foreach (var config in configs)
        {
            // Tạo một biến cục bộ để tránh lỗi tham chiếu trong closure
            GameObject prefabToSpawn = config.prefab;

            var pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefabToSpawn),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: config.defaultSize,
                maxSize: config.maxSize
            );

            poolDict.Add(config.type, pool);
        }
    }

    public GameObject GetEffect(EffectType type, Vector3 position)
    {
        if (poolDict.ContainsKey(type))
        {
            GameObject obj = poolDict[type].Get();
            obj.transform.position = position;

            // Gán thông tin loại effect để lúc trả về biết trả vào pool nào
            var pooledObj = obj.GetComponent<PooledEffect>() ?? obj.AddComponent<PooledEffect>();
            pooledObj.type = type;

            return obj;
        }
        return null;
    }

    public void ReturnEffect(EffectType type, GameObject obj)
    {
        if (poolDict.ContainsKey(type)) poolDict[type].Release(obj);
    }
}
