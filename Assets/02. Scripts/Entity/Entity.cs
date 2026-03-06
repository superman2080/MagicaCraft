using System;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(Rigidbody2D))]
public abstract class Entity : MonoBehaviour, IDamagable
{
    public Rigidbody2D Rb2d { get; private set; }
    public Collider2D Col { get; private set; }

    public abstract StateBase<Entity> EntryState { get; }
    public StateMachine<Entity> StateMachine => stateMachine;
    protected StateMachine<Entity> stateMachine;

    public DefaultStat DefaultStat { get; protected set; }

    public float HP => DefaultStat.HP.CurrentValue;

    // 엔티티의 속성 (이중 속성 지원)
    // Inspector에서 설정하거나 서브클래스 생성자에서 지정한다.
    [SerializeField] private ElementType[] affinities = { ElementType.None };
    public IReadOnlyList<ElementType> Affinities => affinities;

    // 공격 속성에 대한 최종 데미지 배율을 계산한다.
    public float GetDamageMultiplier(ElementType attackElement)
        => ElementChart.GetMultiplier(attackElement, affinities);

    #region Events
    public event OnDiedHandler OnDied;
    public event OnTakeDamageHandler OnTakeDamage;
    #endregion

    #region Status Effects
    private StatusEffectHandler statusEffectHandler;
    public void AddEffect(StatusEffect effect) => statusEffectHandler.AddEffect(effect);
    public void HasEffect(StatusEffectType type) => statusEffectHandler.HasEffect(type);
    public void HasEffect<T>() where T : StatusEffect => statusEffectHandler.HasEffect<T>();
    public void ClearEffects() => statusEffectHandler.ClearEffects();

    #endregion

    public virtual void TakeDamage(IAttackable caster, float amount, ElementType element = ElementType.None)
    {
        float multiplier  = GetDamageMultiplier(element);
        float finalAmount = Mathf.Abs(amount) * multiplier;

        OnTakeDamage?.Invoke(this, caster, finalAmount);
        DefaultStat.HP.Subtract(finalAmount);
        if (DefaultStat.HP.IsEmpty)
        {
            OnDied?.Invoke(this, caster);
        }
    }

    public virtual void Heal(Entity caster, float amount)
    {
        DefaultStat.HP.Add(Mathf.Abs(amount));
    }

    // protected virtual: 서브클래스에서 override 후 base.Start() 호출
    protected virtual void Start()
    {
        Rb2d = gameObject.GetComponent<Rigidbody2D>();
        Col = gameObject.GetComponent<Collider2D>();

        stateMachine = new StateMachine<Entity>(this, EntryState);
        statusEffectHandler = new StatusEffectHandler(this);
    }

    // protected virtual: 서브클래스에서 override 후 base.Update() 호출
    protected virtual void Update()
    {
        stateMachine.Update();
    }

    protected virtual void LateUpdate()
    {
        statusEffectHandler.UpdateEffect();
        DefaultStat.Update();
    }

    // 물리 연산용 상태 업데이트
    protected virtual void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }
}
