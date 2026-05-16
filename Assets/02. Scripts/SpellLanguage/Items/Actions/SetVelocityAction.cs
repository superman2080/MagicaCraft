using System.Collections.Generic;

// SetVelocity(proj, velocity)
// OnUpdate 안에서 투사체 이동 방향·속도를 설정한다.
// 마나 비용: 없음
//
// 사용 예:
//   SetVelocity(proj, Normalize(target.position - proj.position) * proj.speed)
public class SetVelocityAction : SpellAction
{
    public override string DSLName  => "SetVelocity";
    public override string Signature => "SetVelocity(proj, vel)";

    protected override float GetManaCost(List<object> args, int line, int col) => 0f;

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "SetVelocity(proj, velocity)";
        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);
        if (args[0] is not SpellProjectileData proj)
            throw new RuntimeException($"{sig}: 첫 번째 인자는 투사체여야 합니다.", line, col);
        proj.SetVelocity(RequireVector2(args[1], sig));
        return null;
    }
}
