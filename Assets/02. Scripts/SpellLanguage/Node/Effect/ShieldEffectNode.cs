using System.Collections;

// 보호막(임시 회복) 노드.
// 마나 비용: Amount × 0.7
// TODO: 보호막 시스템 구현 후 즉시 회복 처리 교체
public class ShieldEffectNode : SpellNode
{
    public float        Amount    { get; set; } = 10f;
    public TargetSource TargetSrc { get; set; } = TargetSource.ContextTarget;

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        ctx.Mana.Spend(Amount * 0.7f);

        var target = ResolveTarget(ctx);
        if (target != null)
            target.Heal(ctx.Caster, Amount);

        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }

    private Entity ResolveTarget(SpellNodeContext ctx)
    {
        if (TargetSrc == TargetSource.BlackboardVictim &&
            ctx.TryGet<Entity>(SpellNodeContext.KeyVictim, out var victim))
            return victim;
        return ctx.Target;
    }
}
