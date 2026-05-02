using System.Collections;
using UnityEngine;

// 투사체를 스폰하고 즉시 OnExitDispatcher를 발화한다 (fire-and-forget).
// 명중/만료는 OnProjectileHitDispatcher / OnProjectileExpiredDispatcher로 비동기 관리한다.
public class ProjectileNode : SpellNode
{
    public ElementType Element            { get; set; } = ElementType.None;
    public float       Damage             { get; set; } = 10f;
    public float       Speed              { get; set; } = 10f;
    public bool        UseTargetDirection { get; set; } = true;
    public Vector2     FixedDirection     { get; set; } = Vector2.right;

    // 명중 시 발화 (Blackboard에 victim, hitPosition 주입)
    public SpellEventDispatcher OnProjectileHitDispatcher     { get; } = new SpellEventDispatcher();
    // 수명 만료 또는 마나 소진 시 발화
    public SpellEventDispatcher OnProjectileExpiredDispatcher { get; } = new SpellEventDispatcher();

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        Vector2 startPos  = (Vector2)ctx.Caster.transform.position;
        Vector2 direction = ResolveDirection(ctx);

        var proj = Pool.Instance.Get<SpellProjectile>(PoolKey.SpellProjectile, p =>
        {
            p.InitForNode(ctx, startPos, direction, Speed, Element, Damage);
        });

        // 이벤트 람다가 ctx를 캡처 — 반드시 Fork() 사용
        proj.OnHit += victim =>
        {
            var fork = ctx.Fork();
            fork.Set(SpellNodeContext.KeyVictim, victim);
            fork.Set(SpellNodeContext.KeyHitPosition, (Vector2)victim.transform.position);
            ctx.Runner.StartCoroutine(OnProjectileHitDispatcher.Fire(fork));
        };

        proj.OnExpire += () =>
        {
            ctx.Runner.StartCoroutine(OnProjectileExpiredDispatcher.Fire(ctx.Fork()));
        };

        // fire-and-forget: 투사체 날아가는 동안 다음 노드 즉시 실행
        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }

    private Vector2 ResolveDirection(SpellNodeContext ctx)
    {
        if (!UseTargetDirection || ctx.Target == null)
            return FixedDirection.normalized;
        return ((Vector2)ctx.Target.transform.position - (Vector2)ctx.Caster.transform.position).normalized;
    }
}
