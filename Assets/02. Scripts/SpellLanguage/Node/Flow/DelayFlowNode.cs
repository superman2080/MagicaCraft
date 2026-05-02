using System.Collections;
using UnityEngine;

// DelaySeconds 동안 대기한 후 OnExitDispatcher를 발화한다.
public class DelayFlowNode : SpellNode
{
    public float DelaySeconds { get; set; } = 1f;

    public override IEnumerator Execute(SpellNodeContext ctx)
    {
        yield return ctx.Runner.StartCoroutine(OnEnterDispatcher.Fire(ctx));

        for (float t = 0f; t < DelaySeconds; t += Time.deltaTime)
            yield return null;

        yield return ctx.Runner.StartCoroutine(OnExecuteDispatcher.Fire(ctx));
        yield return ctx.Runner.StartCoroutine(OnExitDispatcher.Fire(ctx));
    }
}
