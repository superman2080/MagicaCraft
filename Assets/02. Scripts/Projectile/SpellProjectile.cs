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
[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class SpellProjectile : Poolable
{
    [Tooltip("투사체 수명 (초). 이 시간이 지나면 자동 반환된다.")]
    [SerializeField] private float maxLifetime = 5f;

    public Rigidbody2D Rb2d { get; private set; }

    // ─── 투사체 파라미터 ──────────────────────────────────────
    // item이 null인 경우 기본 피격 처리에 사용된다.
    public float       Damage  { get; private set; }
    public float       Speed   { get; internal set; }   // SetSpeed에서 쓰기 허용
    public ElementType Element { get; private set; }

    // ─── 내부 상태 ────────────────────────────────────────────

    private SpellContext        _ctx;
    private ManaTracker         _mana;
    private ProjectileObject    _projObj;
    private SpellProjectileData _data;
    private LayerMask           _attackLayer;
    private float               _spawnTime;
    private bool                _isAlive;
    private System.Action       _returnCallback;

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
        if (!_isAlive) return;

        if (Time.time - _spawnTime > maxLifetime)
        {
            ReturnToPool();
            return;
        }

        if (_projObj == null) return;

        RunLifecycle(() => _projObj.RunUpdate(
            _ctx, _mana, new Dictionary<string, object> { ["proj"] = _data }));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isAlive) return;

        if (_ctx?.Caster != null && other.gameObject == _ctx.Caster.gameObject) return;

        if ((_attackLayer & (1 << other.gameObject.layer)) == 0) return;

        if (!other.TryGetComponent<IDamagable>(out var damagable)) return;

        if (_projObj != null && other.TryGetComponent<Entity>(out var victim))
        {
            // OnFinish: proj + victim 주입
            RunLifecycle(() => _projObj.RunFinish(
                _ctx, _mana,
                new Dictionary<string, object> { ["proj"] = _data, ["victim"] = victim }));
        }
        else
        {
            // projObj 없을 때 기본 피격 처리
            damagable.TakeDamage(_ctx?.Caster as IAttackable, Damage, Element);
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
        _ctx            = ctx;
        _mana           = mana;
        _projObj        = projObj;
        Speed           = speed;
        Element         = element;
        Damage          = damage;
        _returnCallback = returnCallback;
        _data           = new SpellProjectileData(this);
        _spawnTime      = Time.time;
        _isAlive        = true;

        Rb2d.position       = startPos;
        Rb2d.linearVelocity = direction.normalized * speed;

        var attacker = ctx.Caster as IAttackable;
        _attackLayer = attacker != null ? attacker.AttackLayer : Physics2D.AllLayers;

        // 마나 소진 시 즉시 회수
        _mana.OnDepleted += ReturnToPool;

        // OnStart 실행
        if (_projObj != null)
            RunLifecycle(() => _projObj.RunStart(
                _ctx, _mana, new Dictionary<string, object> { ["proj"] = _data }));
    }

    public override void OnDespawn()
    {
        _isAlive = false;
        if (_mana != null)
        {
            _mana.OnDepleted -= ReturnToPool;
            _mana = null;
        }
        _ctx            = null;
        _projObj        = null;
        _data           = null;
        _returnCallback = null;
        Rb2d.linearVelocity = Vector2.zero;
        base.OnDespawn();
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private void ReturnToPool()
    {
        _isAlive = false;
        _returnCallback?.Invoke();
    }

    private void RunLifecycle(System.Action runner)
    {
        if (_ctx == null) return;
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
