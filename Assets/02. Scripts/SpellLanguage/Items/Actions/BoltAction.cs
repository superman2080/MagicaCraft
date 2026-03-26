using System.Collections.Generic;

// Bolt(target, element, damage)
// 단일 대상에게 속성 데미지를 준다.
// 마나 비용: damage * ManaCostPerDamage
public class BoltAction : SpellAction
{
    public const float ManaCostPerDamage = 1.0f;

    public override string DSLName => "Bolt";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 3)
            throw new RuntimeException("Bolt(target, element, damage): 인자 3개 필요", line, col);
        return (float)RequireDouble(args[2], "Bolt(target, element, damage)") * ManaCostPerDamage;
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Bolt(target, element, damage)";
        var   target  = RequireEntity (args[0], sig);
        var   element = RequireElement(args[1], sig);
        float damage  = (float)RequireDouble(args[2], sig);
        target.TakeDamage(ctx.Caster as IAttackable, damage, element);
        return null;
    }
}
