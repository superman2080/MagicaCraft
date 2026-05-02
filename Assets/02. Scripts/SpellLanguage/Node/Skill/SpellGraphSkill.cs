using System.Collections;
using UnityEngine;

// 노드 그래프 기반 스킬. SpellSkill을 대체한다.
// SpellGraph를 보유하고 RunOnStart/Update/End로 그래프 진입 노드를 구동한다.
public class SpellGraphSkill
{
    public string    Name        { get; set; } = "새 스킬";
    public string    Description { get; set; } = "";
    public SkillType Type        { get; set; }
    public float     ManaCost    { get; set; } = 0f;
    public SpellGraph Graph      { get; set; }

    public bool IsCancelled { get; private set; }

    private ManaTracker subscribedMana;

    // OneShot: 버튼 입력 시 / Continuous: 버튼 다운 시 호출
    public IEnumerator RunOnStart(SpellNodeContext ctx)
    {
        IsCancelled = false;

        subscribedMana = ctx.Mana;
        ctx.Mana.OnDepleted += Cancel;

        ctx.Mana.Spend(ManaCost);

        if (Graph?.CastEntryNode != null)
            yield return ctx.Runner.StartCoroutine(Graph.CastEntryNode.Execute(ctx));
    }

    // Continuous 전용: 버튼 홀드 중 매 프레임 호출
    public IEnumerator RunOnUpdate(SpellNodeContext ctx)
    {
        if (IsCancelled) yield break;
        if (Graph?.UpdateEntryNode != null)
            yield return ctx.Runner.StartCoroutine(Graph.UpdateEntryNode.Execute(ctx));
    }

    // Continuous 전용: 버튼 업 시 호출
    public IEnumerator RunOnEnd(SpellNodeContext ctx)
    {
        if (IsCancelled) yield break;
        if (Graph?.EndEntryNode != null)
            yield return ctx.Runner.StartCoroutine(Graph.EndEntryNode.Execute(ctx));
        Unsubscribe();
    }

    // 강제 중단 — ManaTracker.OnDepleted 또는 외부에서 직접 호출
    public void Cancel()
    {
        IsCancelled = true;
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (subscribedMana != null)
        {
            subscribedMana.OnDepleted -= Cancel;
            subscribedMana = null;
        }
    }
}
