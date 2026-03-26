using System.Collections.Generic;

// Heal(target, amount)
// 대상의 HP를 회복한다.
// 마나 비용: amount * ManaCostPerAmount
public class HealAction : SpellAction
{
    public const float ManaCostPerAmount = 0.8f;

    public override string DSLName => "Heal";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 2)
            throw new RuntimeException("Heal(target, amount): 인자 2개 필요", line, col);
        return (float)RequireDouble(args[1], "Heal(target, amount)") * ManaCostPerAmount;
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Heal(target, amount)";
        var   target = RequireEntity(args[0], sig);
        float amount = (float)RequireDouble(args[1], sig);
        target.Heal(ctx.Caster, amount);
        return null;
    }
}
