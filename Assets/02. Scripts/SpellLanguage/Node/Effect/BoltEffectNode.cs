using System.Collections;

// 단일 대상 속성 데미지 노드.
// 마나 비용: Damage × 1.0
public class BoltEffectNode : SpellNode
{
    public ElementType Element     { get; set; } = ElementType.None;
    public float       Damage      { get; set; } = 10f;
    public TargetSource TargetSrc  { get; set; } = TargetSource.ContextTarget;

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        ctx.Mana.Spend(Damage * 1.0f);

        var target = ResolveTarget(ctx);
        if (target != null)
            target.TakeDamage(ctx.Caster as IAttackable, Damage, Element);

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
