using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CommonState
{
    Concentration,
}

public enum HardCCStates
{
    HardCC, // 복합 상태(CompositeStateBase) 키 — 메인 FSM에 등록
    Fear,   // 서브 상태 키 — HardCCState.SubStateMachine에 등록
    Stun,   // 서브 상태 키 (Priority 1, 추후 구현)
}

public delegate void OnConcentrationEnterEventHandler(Entity entity);
public delegate void OnConcentrationExecuteEventHandler(Entity entity);
public delegate void OnConcentrationExitEventHandler(Entity entity);

public class Concentration: StateBase
{
    public OnConcentrationEnterEventHandler OnConcentrationEnter;
    public OnConcentrationExecuteEventHandler OnConcentrationExecute;
    public OnConcentrationExitEventHandler OnConcentrationExit;

    public Concentration(Entity caster) : base(caster)
    {
    }
    public override void Enter()
    {
        OnConcentrationEnter?.Invoke(caster);
        base.Enter();
    }
    public override void Execute()
    {
        OnConcentrationExecute?.Invoke(caster);
        // 집중 상태에서의 행동 (예: 공격력 증가)
        base.Execute();
    }
    public override void Exit()
    {
        OnConcentrationExit?.Invoke(caster);
        base.Exit();
    }
}

// 제어 불가능 상태이상(HardCC)을 통합 관리하는 복합 상태.
// 동시에 여러 HardCC 효과가 걸렸을 때 Priority가 가장 높은 효과의 서브 상태로 자동 전환한다.
// 새 HardCC 종류 추가 시:
//   1. HardCCStates에 서브 상태 열거값 추가
//   2. effectToState 딕셔너리에 (StatusEffect 타입, 열거값) 매핑 추가
//   3. 생성자에서 SubStateMachine.RegisterState() 호출
public class HardCCState : CompositeStateBase
{
    // StatusEffect 구체 타입 → HardCCStates 서브 상태 키 매핑
    private static readonly Dictionary<Type, HardCCStates> effectToState = new()
    {
        { typeof(Fear), HardCCStates.Fear },
        // { typeof(Stun), HardCCStates.Stun }, // 추후 Stun 구현 시 추가
    };

    public HardCCState(Entity entity) : base(entity)
    {
        SubStateMachine.RegisterState(HardCCStates.Fear, new FearState(entity));
    }

    public override void Enter()
    {
        // Enter 전에 초기 서브 상태를 설정해 둔다 (Enter 중복 호출 방지를 위해 SetInitialState 사용)
        var topKey = GetTopPriorityStateKey(caster);
        if (topKey.HasValue)
            SubStateMachine.SetInitialState(topKey.Value);

        base.Enter(); // → SubStateMachine.Enter() → 서브 상태 Enter()
    }

    public override void Execute()
    {
        // 매 프레임 최고 우선순위 HardCC 효과를 확인하고, 변경됐으면 서브 상태를 전환한다.
        var topKey = GetTopPriorityStateKey(caster);
        if (topKey.HasValue)
        {
            var targetState = SubStateMachine.GetState(topKey.Value);
            if (SubStateMachine.CurState != targetState)
                SubStateMachine.ChangeState(topKey.Value);
        }

        base.Execute(); // → SubStateMachine.Update() → 서브 상태 Execute()
    }

    public override void Exit()
    {
        base.Exit(); // → SubStateMachine.Exit() → 서브 상태 Exit()
    }

    private HardCCStates? GetTopPriorityStateKey(Entity entity)
    {
        var top = entity.GetEffects(StatusEffectType.HARD_CC)
            .OrderByDescending(e => e.Priority)
            .FirstOrDefault();

        if (top == null) return null;
        return effectToState.TryGetValue(top.GetType(), out var key) ? key : (HardCCStates?)null;
    }
}

public class FearState : StateBase
{
    private Entity fearCaster;

    public FearState(Entity caster) : base(caster) { }

    public override void Enter()
    {
        base.Enter();
        fearCaster = caster.GetEffect<Fear>()?.Caster;
        caster.SpriteRenderer.color = Color.gray;
    }

    public override void Execute()
    {
        base.Execute();
        caster.SpriteRenderer.color = Color.gray;
        // Stat<T>.InitStat()이 매 LateUpdate에서 배율을 1로 초기화하므로 매 프레임 재적용한다.
        caster.DefaultStat.Multiply(DefaultStatType.MOVE_SPEED, 0.5f);
    }

    public override void FixedExecute()
    {
        base.FixedExecute();
        Vector2 direction = fearCaster
            ? (caster.transform.position - fearCaster.transform.position).normalized
            : Random.insideUnitCircle.normalized;
        caster.Rb2d.MovePosition((Vector2)caster.transform.position + direction * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        caster.SpriteRenderer.color = Color.white;
        // MOVE_SPEED 배율은 InitStat()이 다음 프레임에 자동으로 1로 복원한다.
    }
}

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public abstract class Entity : MonoBehaviour, IDamagable
{
    public Rigidbody2D Rb2d { get; private set; }
    public Collider2D Col { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }

    public abstract Enum EntryStateKey { get; }
    public EntityStateMachine StateMachine => stateMachine;
    protected EntityStateMachine stateMachine;

    public DefaultStat DefaultStat => defaultStat;
    private DefaultStat defaultStat;

    public float HP => defaultStat.HP.CurrentValue;

    // 엔티티의 속성 (이중 속성 지원)
    // Inspector에서 설정하거나 서브클래스 생성자에서 지정한다.
    [SerializeField] private ElementType[] affinities = { ElementType.None };
    public IReadOnlyList<ElementType> Affinities => affinities;

    public DefaultStatDataSO DefaultStatData => defaultStatData;
    [SerializeField] private DefaultStatDataSO defaultStatData;

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

    public StatusEffect GetEffect<T>() where T : StatusEffect => statusEffectHandler.GetEffect<T>();

    public bool HasEffect(StatusEffectType type) => statusEffectHandler.HasEffect(type);
    public bool HasEffect<T>() where T : StatusEffect => statusEffectHandler.HasEffect<T>();
    public IEnumerable<StatusEffect> GetEffects(StatusEffectType type) => statusEffectHandler.GetEffects(type);
    public void ClearEffects() => statusEffectHandler.ClearEffects();
    public void ClearEffects(StatusEffectType type) => statusEffectHandler.ClearEffects(type);

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
        SpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        defaultStat = new DefaultStat(defaultStatData);
        statusEffectHandler = new StatusEffectHandler(this);

        #region StateMachine
        stateMachine = new EntityStateMachine(this);
        InitializeStateMachine();
        if (EntryStateKey != null)
            stateMachine.ChangeState(EntryStateKey);
        #endregion
    }

    // protected virtual: 서브클래스에서 override 후 base.Update() 호출
    protected virtual void Update()
    {
        if (stateMachine.CurState == null) return;
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
        if (stateMachine.CurState == null) return;
        stateMachine.FixedUpdate();
    }

    protected virtual void InitializeStateMachine()
    {

        stateMachine.RegisterState(CommonState.Concentration, new Concentration(this));
        stateMachine.RegisterState(HardCCStates.HardCC, new HardCCState(this));

        // 어떤 상태에서든 HardCC 효과가 생기면 즉시 HardCCState로 전환
        stateMachine.RegisterAnyStateTransition(HardCCStates.HardCC,
            () => statusEffectHandler.HasEffect(StatusEffectType.HARD_CC));

        // HardCC 효과가 모두 해제되면 EntryState로 복귀
        stateMachine.RegisterCondition(HardCCStates.HardCC, EntryStateKey,
            () => !statusEffectHandler.HasEffect(StatusEffectType.HARD_CC));


    }
}
