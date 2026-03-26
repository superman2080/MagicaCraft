using System.Collections.Generic;

// Shield(target, amount)
// 대상에게 임시 HP 보호막을 부여한다.
// 마나 비용: amount * ManaCostPerAmount
//
// TODO: 보호막 시스템 구현 후 즉시 회복 처리 교체
public class ShieldAction : SpellAction
{
    public const float ManaCostPerAmount = 0.7f;

    public override string DSLName => "Shield";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 2)
            throw new RuntimeException("Shield(target, amount): 인자 2개 필요", line, col);
        return (float)RequireDouble(args[1], "Shield(target, amount)") * ManaCostPerAmount;
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Shield(target, amount)";
        var   target = RequireEntity(args[0], sig);
        float amount = (float)RequireDouble(args[1], sig);
        // TODO: 보호막 시스템 구현 후 교체. 임시로 즉시 회복으로 처리
        target.Heal(ctx.Caster, amount);
        return null;
    }
}
