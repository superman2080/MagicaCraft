using System.Collections;

// 시전 진입 노드.
// SpellGraph.CastEntryNode에 할당한다.
// 자체 로직 없이 OnEnterDispatcher → OnExitDispatcher 순으로 발화한다.
public class CastTriggerNode : SpellNode
{
    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
