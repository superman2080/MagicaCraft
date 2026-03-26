using System.Collections.Generic;

// Fire(projObj, startPos, direction, speed)
// ProjectileObject를 월드에 생성·발사한다.
// 마나 비용: projObj.DefaultDamage * ManaCostPerDamage
//
// 사용 예:
//   Fire(basicBolt, self.position, target.position - self.position, 10)
public class FireAction : SpellAction
{
    public const float ManaCostPerDamage = 0.5f;

    public override string DSLName => "Fire";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 4)
            throw new RuntimeException(
                "Fire(projObj, startPos, direction, speed): 인자 4개 필요", line, col);
        var obj = RequireSpellObject(args[0], "Fire(projObj, startPos, direction, speed)");
        return obj is ProjectileObject p ? p.DefaultDamage * ManaCostPerDamage : 0f;
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Fire(projObj, startPos, direction, speed)";

        if (args[0] is not ProjectileObject projObj)
            throw new RuntimeException($"{sig}: ProjectileObject 타입이 필요합니다.", line, col);

        var   startPos  = RequireVector2(args[1], sig);
        var   direction = RequireVector2(args[2], sig);
        float speed     = (float)RequireDouble(args[3], sig);

        if (SpellProjectilePool.Instance == null)
            throw new RuntimeException(
                "SpellProjectilePool이 씬에 없습니다. Pool 오브젝트를 씬에 추가하세요.", line, col);

        var proj = SpellProjectilePool.Instance.Get();
        proj.Init(ctx, ctx.Mana, startPos, direction, speed, projObj);
        return null;
    }
}
