using System.Collections.Generic;

// SetSpeed(proj, speed)
// 이동 방향은 유지한 채 속도 크기만 변경한다.
// 마나 비용: 없음
//
// 사용 예:
//   SetSpeed(proj, proj.speed * 1.5)
public class SetSpeedAction : SpellAction
{
    public override string DSLName => "SetSpeed";

    protected override float GetManaCost(List<object> args, int line, int col) => 0f;

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "SetSpeed(proj, speed)";
        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);
        if (args[0] is not SpellProjectileData proj)
            throw new RuntimeException($"{sig}: 첫 번째 인자는 투사체여야 합니다.", line, col);
        proj.SetSpeed((float)RequireDouble(args[1], sig));
        return null;
    }
}
