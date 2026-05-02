using System;
using System.Collections;

// 조건 평가 후 TrueDispatcher 또는 FalseDispatcher를 발화한다.
// Condition이 null이면 항상 FalseDispatcher를 발화한다 (안전 기본값).
public class BranchFlowNode : SpellNode
{
    public Func<SpellNodeContext, bool> Condition { get; set; }

    public SpellEventDispatcher TrueDispatcher  { get; } = new SpellEventDispatcher();
    public SpellEventDispatcher FalseDispatcher { get; } = new SpellEventDispatcher();

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        bool result = Condition != null && Condition(ctx);
        var  branch = result ? TrueDispatcher : FalseDispatcher;

        yield return ctx.Runner.StartCoroutine(branch.Fire(ctx));

        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
