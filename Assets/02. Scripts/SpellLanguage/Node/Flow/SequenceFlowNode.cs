using System.Collections;
using System.Collections.Generic;

// 내부 children 목록을 순서대로 실행한 후 OnExitDispatcher를 발화한다.
public class SequenceFlowNode : SpellNode
{
    private readonly List<SpellNode> children = new();

    public void Add(SpellNode node)    => children.Add(node);
    public void Remove(SpellNode node) => children.Remove(node);

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        foreach (var child in children)
            yield return ctx.Runner.StartCoroutine(child.Execute(ctx));

        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
