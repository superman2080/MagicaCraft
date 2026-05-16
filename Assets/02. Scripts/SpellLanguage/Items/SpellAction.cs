using System.Collections.Generic;
using UnityEngine;

// 단발성 마법 효과(Bolt, Heal, Explode 등)의 기반 클래스.
// SpellContext에 명령어로 등록되어 DSL에서 함수처럼 호출된다.
// 플레이어는 호출만 할 수 있고 구현을 수정할 수 없다.
public abstract class SpellAction : SpellItem
{
    // DSL에서 이 명령어를 호출할 때 사용하는 이름 (예: "Bolt", "Heal")
    public abstract string DSLName { get; }

    // 자동완성에서 표시할 시그니처 힌트 (예: "Bolt(target, element, damage)")
    // 새 SpellAction 추가 시 반드시 override한다.
    public virtual string Signature => DSLName;

    // SpellContext에 이 명령어를 등록한다.
    public void RegisterTo(SpellContext ctx)
        => ctx.RegisterCommand(DSLName, Invoke);

    // 마나 차감 흐름 보장. 서브클래스는 Execute를 구현한다.
    public object Invoke(List<object> args, SpellContext ctx, int line, int col)
    {
        float cost = GetManaCost(args, line, col);
        if (cost > 0f)
            ctx.Mana.Spend(cost, line, col);
        return Execute(args, ctx, line, col);
    }

    protected abstract float  GetManaCost(List<object> args, int line, int col);
    protected abstract object Execute    (List<object> args, SpellContext ctx, int line, int col);

    // ─── 인자 검증 헬퍼 ───────────────────────────────────────

    protected Entity RequireEntity(object val, string sig)
    {
        if (val is Entity e) return e;
        throw new RuntimeException($"{sig}: Entity 타입 인자가 필요합니다. (self 또는 target 사용)");
    }

    protected ElementType RequireElement(object val, string sig)
    {
        if (val is ElementType el) return el;
        throw new RuntimeException($"{sig}: 속성(Fire, Water 등) 인자가 필요합니다.");
    }

    protected double RequireDouble(object val, string sig)
    {
        if (val is double d) return d;
        throw new RuntimeException($"{sig}: 숫자 인자가 필요합니다.");
    }

    protected Vector2 RequireVector2(object val, string sig)
    {
        if (val is Vector2 v) return v;
        if (val is Entity  e) return e.Rb2d != null ? e.Rb2d.position : Vector2.zero;
        throw new RuntimeException($"{sig}: Vec2 또는 Entity 타입 인자가 필요합니다.");
    }

    protected SpellObject RequireSpellObject(object val, string sig)
    {
        if (val is SpellObject obj) return obj;
        throw new RuntimeException(
            $"{sig}: SpellObject가 필요합니다. SpellLibrary에 등록 후 사용하세요.");
    }

    protected IBarriorUser RequireBarriorUsable(object val, string sig)
    {
        if (val is  IBarriorUser barrior) return barrior;
        throw new RuntimeException(
            $"{sig}: BarriorUsable이 필요합니다.");
    }
}
