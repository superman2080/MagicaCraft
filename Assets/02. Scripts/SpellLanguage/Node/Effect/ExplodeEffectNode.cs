using System.Collections;
using UnityEngine;

// 범위 폭발 데미지 노드.
// 마나 비용: Damage × (1.0 + Radius × 0.5)
// 중심: Blackboard[hitPosition] 또는 Caster 위치
public class ExplodeEffectNode : SpellNode
{
    public float       Radius  { get; set; } = 3f;
    public ElementType Element { get; set; } = ElementType.None;
    public float       Damage  { get; set; } = 10f;

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        ctx.Mana.Spend(Damage * (1.0f + Radius * 0.5f));

        Vector2 center = ctx.TryGet<Vector2>(SpellNodeContext.KeyHitPosition, out var pos)
            ? pos
            : (Vector2)ctx.Caster.transform.position;

        var attacker  = ctx.Caster as IAttackable;
        var layerMask = attacker != null ? (int)attacker.AttackLayer : Physics2D.AllLayers;
        var hits      = Physics2D.OverlapCircleAll(center, Radius, layerMask);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamagable>(out var damagable))
                damagable.TakeDamage(attacker, Damage, Element);
        }

        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
