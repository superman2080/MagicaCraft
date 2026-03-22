using System.Collections.Generic;
using UnityEngine;

public abstract class Pool<T> : MonoBehaviour where T : Poolable
{
    public int initialSize = 10;
    public T prefab;

    private static Pool<T> instance;
    public static Pool<T> Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (Pool<T>)FindAnyObjectByType(typeof(Pool<T>));

                if (instance == null)
                {
                    Debug.LogError($"[Pool] {typeof(T).Name} Pool이 씬에 없습니다. Pool 오브젝트를 씬에 추가하세요.");
                }
            }
            return instance;
        }
    }

    private Queue<T> pool = new();

    protected virtual void Start()
    {
        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNew();
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    protected virtual T CreateNew() => Instantiate(prefab, transform);

    public virtual T Get()
    {
        var obj = pool.Count > 0 ? pool.Dequeue() : CreateNew();
        obj.OnSpawn();
        return obj;
    }

    public virtual T Get(Vector3 position)
    {
        var obj = Get();
        obj.transform.position = position;
        return obj;
    }

    public virtual T Get(Vector3 position, Quaternion rotation)
    {
        var obj = Get(position);
        obj.transform.rotation = rotation;
        return obj;
    }

    public virtual T Get(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var obj = Get(position, rotation);
        obj.transform.localScale = scale;
        return obj;
    }

    public virtual void Return(T obj)
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        obj.OnDespawn();
        pool.Enqueue(obj);
    }
}
