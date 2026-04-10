using System;
using UnityEngine;

public abstract class Weapon<T> : IEquipable<T> where T: MonoBehaviour
{
    [SerializeField] private AttackStatDataSO attackStatData;

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

    public virtual void Execute(T owner){ }

    public virtual void FixedExecute(T owner) { }

    public abstract void Using(IAttackable attackable);
}
