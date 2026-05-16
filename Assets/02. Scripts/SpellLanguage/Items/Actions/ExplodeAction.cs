using System.Collections.Generic;
using UnityEngine;

// Explode(center, radius, element, damage)
// 지정 위치 주변 radius 범위 내 모든 적에게 속성 데미지를 준다.
// 마나 비용: damage * (ManaCostPerDamage + radius * RadiusMul)
public class ExplodeAction : SpellAction
{
    public const float ManaCostPerDamage = 1.0f;
    public const float RadiusMul         = 0.5f;

    public override string DSLName  => "Explode";
    public override string Signature => "Explode(center, radius, element, damage)";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 4)
            throw new RuntimeException(
                "Explode(center, radius, element, damage): 인자 4개 필요", line, col);
        float radius = (float)RequireDouble(args[1], "Explode");
        float damage = (float)RequireDouble(args[3], "Explode");
        return damage * (ManaCostPerDamage + radius * RadiusMul);
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Explode(center, radius, element, damage)";
        var   center  = RequireVector2(args[0], sig);
        float radius  = (float)RequireDouble(args[1], sig);
        var   element = RequireElement (args[2], sig);
        float damage  = (float)RequireDouble(args[3], sig);

        var attacker  = ctx.Caster as IAttackable;
        var layerMask = attacker != null ? (int)attacker.AttackLayer : Physics2D.AllLayers;
        var hits      = Physics2D.OverlapCircleAll(center, radius, layerMask);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamagable>(out var damagable))
                damagable.TakeDamage(attacker, damage, element);
        }
        return null;
    }
}
