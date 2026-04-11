using System;
using UnityEngine;

public abstract class Weapon<T> : Equipment<T> where T: MonoBehaviour
{
    [SerializeField] private AttackStatDataSO attackStatData;

    public abstract void Using(IAttackable attackable);
}
