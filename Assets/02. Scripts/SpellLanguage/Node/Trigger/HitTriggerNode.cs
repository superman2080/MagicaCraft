using System.Collections;

// 투사체/폭발 명중 진입 노드.
// ProjectileNode.OnProjectileHitDispatcher에 연결된다.
// Blackboard[KeyVictim]에서 피격 대상을 받아 다음 체인으로 전달한다.
public class HitTriggerNode : SpellNode
{
    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
