using UnityEngine;

public delegate void OnAttackHandler(IAttackable sender, IDamagable target, float amount);

public interface IAttackable
{
    AttackStat AttackStat { get; }
    AttackStatDataSO AttackStatData { get; }

    LayerMask AttackLayer { get; }

    void Attack(IDamagable target, float amount);
}
