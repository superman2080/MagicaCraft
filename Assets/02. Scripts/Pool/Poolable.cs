using UnityEngine;

public abstract class Poolable: MonoBehaviour
{
    public virtual void OnSpawn()
    {
        gameObject.SetActive(true);
    }
    public virtual void OnDespawn()
    {
        gameObject.SetActive(false);
    }

}
