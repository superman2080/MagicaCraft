using UnityEngine;

public delegate void OnTakeDamageHandler(IDamagable sender, IAttackable caster, float amount);
public delegate void OnHealHandler(IDamagable sender, IAttackable caster, float amount);
public delegate void OnDiedHandler(IDamagable sender, IAttackable caster);

public interface IDamagable
{
    DefaultStat DefaultStat { get; }
    DefaultStatDataSO DefaultStatData { get; }

    float HP { get; }

    // element 미지정 시 None(배율 1.0)으로 처리되어 기존 호출과 호환된다.
    void TakeDamage(IAttackable caster, float amount, ElementType element = ElementType.None);

    void Heal(Entity caster, float amount);
}
