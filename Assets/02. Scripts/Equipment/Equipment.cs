using UnityEngine;

public abstract class Equipment<T> : IEquipable<T> where T : MonoBehaviour
{
    public T Owner => owner;
    protected T owner;

    public virtual void Divest(T owner)
    {
        this.owner = null;
    }

    public virtual void Equip(T owner)
    {
        this.owner = owner;
    }

    public virtual void Execute(T owner) { }
    public virtual void FixedExecute(T owner) { }
}
