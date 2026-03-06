using System.Collections.Generic;
using UnityEngine;

public abstract class Pool<T> : MonoBehaviour where T : Poolable
{
    public int initialSize = 10;

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindAnyObjectByType(typeof(T));

                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    GameObject poolObj = GameObject.Find("Pool") ?? new GameObject("Pool");

                    instance = obj.GetComponent<T>();
                    instance.transform.SetParent(poolObj.transform);
                }
            }
            return instance;
        }
    }

    private Queue<T> pool = new Queue<T>();

    public void Start()
    {
        for (int i = 0; i < initialSize; i++)
        {
            T obj = Instantiate(Instance, transform);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public virtual T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            return obj;
        }
        else
        {
            T obj = Instantiate(Instance, transform);
            obj.gameObject.SetActive(true);
            return obj;
        }
    }

    public virtual T Get(Vector3 position)
    {
        T obj = Get();
        obj.transform.position = position;
        return obj;
    }

    public virtual T Get(Vector3 position, Quaternion rotation)
    {
        T obj = Get(position);
        obj.transform.rotation = rotation;
        return obj;
    }

    public virtual T Get(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        T obj = Get(position, rotation);
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
