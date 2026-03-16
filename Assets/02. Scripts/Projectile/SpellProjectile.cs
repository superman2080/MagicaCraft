using System.Collections.Generic;
using UnityEngine;

// DSL로 제어되는 마법 투사체
// SpellProjectilePool에서 가져와 Init()으로 초기화한 뒤 사용한다.
//
// 이벤트 흐름:
//   1. Fire 명령어 → Init() 호출 → 발사
//   2. Update()   → onUpdate(proj) 콜백 실행 (null이면 스킵)
//   3. 충돌       → onHit(victim)  콜백 실행, 기본 동작은 직접 데미지
//   4. 수명 초과  → 자동 반환
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SpellProjectile : Poolable
{
    [Tooltip("투사체 수명 (초). 이 시간이 지나면 자동 반환된다.")]
    [SerializeField] private float maxLifetime = 5f;

    public Rigidbody2D Rb2d { get; private set; }

    // ─── 투사체 파라미터 ──────────────────────────────────────

    public float       Damage   { get; private set; }
    public float       Speed    { get; internal set; }   // SetSpeed에서 쓰기 허용
    public ElementType Element  { get; private set; }

    // ─── 내부 상태 ────────────────────────────────────────────

    private SpellContext         _ctx;
    private ManaTracker          _mana;
    private SpellFunction        _onHit;
    private SpellFunction        _onUpdate;
    private SpellProjectileData  _data;
    private LayerMask            _attackLayer;
    private float                _spawnTime;
    private bool                 _isAlive;

    // ─── Unity 생명주기 ───────────────────────────────────────

    private void Awake()
    {
        Rb2d = GetComponent<Rigidbody2D>();

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        // 투사체는 중력 영향을 받지 않는다
        Rb2d.gravityScale = 0f;
    }

    private void Update()
    {
        if (!_isAlive) return;

        // 수명 초과 시 반환
        if (Time.time - _spawnTime > maxLifetime)
        {
            ReturnToPool();
            return;
        }

        if (_onUpdate == null) return;

        RunCallback(_onUpdate, new List<object> { _data });
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isAlive) return;

        // 시전자 본인 제외
        if (_ctx?.Caster != null && other.gameObject == _ctx.Caster.gameObject) return;

        // 공격 대상 레이어 체크
        if ((_attackLayer & (1 << other.gameObject.layer)) == 0) return;

        if (!other.TryGetComponent<IDamagable>(out var damagable)) return;

        if (_onHit != null && other.TryGetComponent<Entity>(out var victim))
        {
            // 사용자 정의 피격 처리
            RunCallback(_onHit, new List<object> { victim });
        }
        else
        {
            // 기본 피격 처리: 직접 데미지
            damagable.TakeDamage(_ctx?.Caster as IAttackable, Damage, Element);
        }

        ReturnToPool();
    }

    // ─── 초기화 / 풀 반환 ─────────────────────────────────────

    // Fire 명령어에서 호출. 투사체를 발사 상태로 설정한다.
    public void Init(
        SpellContext  ctx,
        ManaTracker   mana,
        Vector2       startPos,
        Vector2       direction,
        ElementType   element,
        float         damage,
        float         speed,
        SpellFunction onHit    = null,
        SpellFunction onUpdate = null)
    {
        _ctx        = ctx;
        _mana       = mana;
        Element     = element;
        Damage      = damage;
        Speed       = speed;
        _onHit      = onHit;
        _onUpdate   = onUpdate;
        _data       = new SpellProjectileData(this);
        _spawnTime  = Time.time;
        _isAlive    = true;

        Rb2d.position         = startPos;
        Rb2d.linearVelocity   = direction.normalized * speed;

        var attacker  = ctx.Caster as IAttackable;
        _attackLayer  = attacker != null ? attacker.AttackLayer : Physics2D.AllLayers;
    }

    public override void OnDespawn()
    {
        _isAlive            = false;
        _ctx                = null;
        _mana               = null;
        _onHit              = null;
        _onUpdate           = null;
        _data               = null;
        Rb2d.linearVelocity = Vector2.zero;
        base.OnDespawn();
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private void ReturnToPool()
    {
        _isAlive = false;
        SpellProjectilePool.Instance.Return(this);
    }

    // SpellFunction(onHit, onUpdate)을 격리된 Interpreter로 실행한다.
    // 마나는 시전자와 공유한다.
    private void RunCallback(SpellFunction fn, List<object> args)
    {
        if (_ctx == null || fn == null) return;

        var extraVars = new Dictionary<string, object>();
        for (int i = 0; i < fn.Params.Count && i < args.Count; i++)
            extraVars[fn.Params[i]] = args[i];

        var interp = new Interpreter(_ctx, _mana, extraVars);
        try
        {
            interp.Run(fn.Body);
        }
        catch (ReturnSignal)
        {
            // 콜백에서의 return은 무시
        }
        catch (OutOfManaException)
        {
            // 마나 부족 시 조용히 중단
            ReturnToPool();
        }
        catch (RuntimeException ex)
        {
            Debug.LogWarning($"[SpellProjectile] 콜백 오류: {ex.Message}");
            ReturnToPool();
        }
    }
}
