using System;
using System.Collections.Generic;
using UnityEngine;

// DSL로 제어되는 마법 투사체.
// SpellProjectilePool에서 가져와 Init()으로 초기화한 뒤 사용한다.
//
// 이벤트 흐름:
//   1. Fire 명령어 → Init() → item.OnStart 실행 → 발사
//   2. Update()   → item.OnUpdate 실행 (매 프레임)
//   3. 충돌       → item.OnEnd 실행 (proj + victim 주입). item 없으면 기본 데미지
//   4. 수명 초과  → 자동 반환
//
// 노드 시스템 전용 이벤트:
//   OnHit    — OnTriggerEnter2D 시 피격 Entity와 함께 발생 (ProjectileNode가 구독)
//   OnExpire — 수명 만료 또는 마나 소진 시 발생 (OnHit 이후 ReturnToPool에서는 발생 안 함)
[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class SpellProjectile : Poolable
{
    // ─── 노드 시스템 이벤트 ───────────────────────────────────
    public event Action<Entity> OnHit;
    public event Action         OnExpire;

    [Tooltip("투사체 수명 (초). 이 시간이 지나면 자동 반환된다.")]
    [SerializeField] private float maxLifetime = 5f;

    public Rigidbody2D Rb2d { get; private set; }

    // ─── 투사체 파라미터 ──────────────────────────────────────
    // item이 null인 경우 기본 피격 처리에 사용된다.
    public float       Damage  { get; private set; }
    public float       Speed   { get; internal set; }   // SetSpeed에서 쓰기 허용
    public ElementType Element { get; private set; }

    // ─── 내부 상태 ────────────────────────────────────────────

    private SpellContext        ctx;
    private ManaTracker         mana;
    private ProjectileObject    projObj;
    private SpellProjectileData data;
    private LayerMask           attackLayer;
    private float               spawnTime;
    private bool                isAlive;
    private bool                hitOccurred;   // true이면 ReturnToPool 시 OnExpire 미발생
    private System.Action       returnCallback;

    // ─── Unity 생명주기 ───────────────────────────────────────

    private void Awake()
    {
        Rb2d = GetComponent<Rigidbody2D>();

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        Rb2d.gravityScale = 0f;
    }

    private void Update()
    {
        if (!isAlive) return;

        if (Time.time - spawnTime > maxLifetime)
        {
            OnExpire?.Invoke();
            ReturnToPool();
            return;
        }

        if (projObj == null) return;

        RunLifecycle(() => projObj.RunUpdate(
            ctx, mana, new Dictionary<string, object> { ["proj"] = data }));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAlive) return;

        if (ctx?.Caster != null && other.gameObject == ctx.Caster.gameObject) return;

        if ((attackLayer & (1 << other.gameObject.layer)) == 0) return;

        if (!other.TryGetComponent<IDamagable>(out var damagable)) return;

        hitOccurred = true;

        if (projObj != null && other.TryGetComponent<Entity>(out var victim))
        {
            // OnFinish: proj + victim 주입
            RunLifecycle(() => projObj.RunFinish(
                ctx, mana,
                new Dictionary<string, object> { ["proj"] = data, ["victim"] = victim }));
            OnHit?.Invoke(victim);
        }
        else
        {
            // projObj 없을 때 기본 피격 처리 + 노드 이벤트
            damagable.TakeDamage(ctx?.Caster as IAttackable, Damage, Element);
            if (other.TryGetComponent<Entity>(out var hitEntity))
                OnHit?.Invoke(hitEntity);
        }

        ReturnToPool();
    }

    // ─── 초기화 / 풀 반환 ─────────────────────────────────────

    // Fire 명령어에서 호출. 투사체를 발사 상태로 설정하고 item.OnStart를 실행한다.
    // item이 null이면 element/damage를 이용한 기본 피격 처리가 사용된다.
    public void Init(
        SpellContext     ctx,
        ManaTracker      mana,
        Vector2          startPos,
        Vector2          direction,
        float            speed,
        System.Action    returnCallback,
        ProjectileObject projObj = null,
        ElementType      element = ElementType.None,
        float            damage  = 0f)
    {
        this.ctx            = ctx;
        this.mana           = mana;
        this.projObj        = projObj;
        Speed               = speed;
        Element             = element;
        Damage              = damage;
        this.returnCallback = returnCallback;
        data                = new SpellProjectileData(this);
        spawnTime           = Time.time;
        isAlive             = true;
        hitOccurred         = false;

        Rb2d.position       = startPos;
        Rb2d.linearVelocity = direction.normalized * speed;

        var attacker = ctx.Caster as IAttackable;
        attackLayer = attacker != null ? attacker.AttackLayer : Physics2D.AllLayers;

        this.mana.OnDepleted += OnManaDepleted;

        // OnStart 실행
        if (this.projObj != null)
            RunLifecycle(() => this.projObj.RunStart(
                this.ctx, this.mana, new Dictionary<string, object> { ["proj"] = data }));
    }

    // ProjectileNode에서 호출. DSL 라이프사이클 없이 순수 C# 노드로 동작한다.
    public void InitForNode(
        SpellNodeContext nodeCtx,
        Vector2          startPos,
        Vector2          direction,
        float            speed,
        ElementType      element,
        float            damage)
    {
        ctx         = nodeCtx.SpellContext;
        mana        = nodeCtx.Mana;
        projObj     = null;
        Speed       = speed;
        Element     = element;
        Damage      = damage;
        data        = new SpellProjectileData(this);
        spawnTime   = Time.time;
        isAlive     = true;
        hitOccurred = false;

        returnCallback = () => Pool.Instance.Return(PoolKey.SpellProjectile, this);

        Rb2d.position       = startPos;
        Rb2d.linearVelocity = direction.normalized * speed;

        var attacker = ctx.Caster as IAttackable;
        attackLayer = attacker != null ? attacker.AttackLayer : Physics2D.AllLayers;

        mana.OnDepleted += OnManaDepleted;
    }

    public override void OnDespawn()
    {
        isAlive = false;
        if (mana != null)
        {
            mana.OnDepleted -= OnManaDepleted;
            mana = null;
        }
        // 구독 누수 방지
        OnHit    = null;
        OnExpire = null;

        ctx            = null;
        projObj        = null;
        data           = null;
        returnCallback = null;
        hitOccurred    = false;
        Rb2d.linearVelocity = Vector2.zero;
        base.OnDespawn();
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private void OnManaDepleted()
    {
        if (!hitOccurred)
            OnExpire?.Invoke();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isAlive = false;
        returnCallback?.Invoke();
    }

    private void RunLifecycle(System.Action runner)
    {
        if (ctx == null) return;
        try
        {
            runner();
        }
        catch (ReturnSignal) { }
        catch (OutOfManaException)
        {
            ReturnToPool();
        }
        catch (RuntimeException ex)
        {
            Debug.LogWarning($"[SpellProjectile] 라이프사이클 오류: {ex.Message}");
            ReturnToPool();
        }
    }
}
