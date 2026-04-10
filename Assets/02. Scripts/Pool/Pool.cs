using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public enum PoolKey
{
    SpellProjectile,
}


public class Pool : Singleton<Pool>
{
    [SerializeField] private SerializedDictionary<PoolKey, Poolable> prefabs = new();
    [SerializeField] private int initialSize = 5;

    private readonly Dictionary<PoolKey, Queue<IPoolable>> _pools = new();

    private void Start()
    {
        foreach (var kvp in prefabs)
        {
            var queue = new Queue<IPoolable>();
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew(kvp.Key);
                obj.OnDespawn();
                queue.Enqueue(obj);
            }
            _pools[kvp.Key] = queue;
        }
    }

    private IPoolable CreateNew(PoolKey key)
    {
        if (!prefabs.TryGetValue(key, out var prefab) || prefab == null)
            throw new InvalidOperationException(
                $"[Pool] 키 '{key}'에 대한 프리팹이 등록되지 않았습니다.");
        return Instantiate(prefab);
    }

    private IPoolable GetInternal(PoolKey key)
    {
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<IPoolable>();
        return queue.Count > 0 ? queue.Dequeue() : CreateNew(key);
    }

    public IPoolable Get(PoolKey key)
    {
        var obj = GetInternal(key);
        obj.OnSpawn();
        return obj;
    }

    // 데이터 주입 오버로드 — initializer 실행 후 OnSpawn() 호출
    public T Get<T>(PoolKey key, Action<T> initializer) where T : class, IPoolable
    {
        var obj = GetInternal(key) as T;
        initializer?.Invoke(obj);
        obj.OnSpawn();
        return obj;
    }

    public void Return(PoolKey key, IPoolable obj)
    {
        obj.OnDespawn();
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<IPoolable>();
        queue.Enqueue(obj);
    }
}
