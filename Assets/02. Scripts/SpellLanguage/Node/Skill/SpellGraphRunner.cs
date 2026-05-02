using UnityEngine;

// SpellGraphSkill의 코루틴을 구동하는 MonoBehaviour.
// SpellGraphSkill.Runner로 전달되어 StartCoroutine 위임을 담당한다.
public class SpellGraphRunner : MonoBehaviour
{
    public SpellGraphSkill Skill { get; set; }

    // 시전 트리거 — 플레이어 입력 처리에서 호출
    public void TriggerCast(Entity caster, Entity target, ManaTracker mana)
    {
        if (Skill == null) return;
        var spellCtx = new SpellContext(caster, target);
        var nodeCtx  = new SpellNodeContext(spellCtx, mana, this);
        StartCoroutine(RunCast(nodeCtx));
    }

    public void TriggerUpdate(Entity caster, Entity target, ManaTracker mana)
    {
        if (Skill == null) return;
        var spellCtx = new SpellContext(caster, target);
        var nodeCtx  = new SpellNodeContext(spellCtx, mana, this);
        StartCoroutine(RunUpdate(nodeCtx));
    }

    public void TriggerEnd(Entity caster, Entity target, ManaTracker mana)
    {
        if (Skill == null) return;
        var spellCtx = new SpellContext(caster, target);
        var nodeCtx  = new SpellNodeContext(spellCtx, mana, this);
        StartCoroutine(RunEnd(nodeCtx));
    }

    // ─── 내부 코루틴 래퍼 ─────────────────────────────────────

    private System.Collections.IEnumerator RunCast(SpellNodeContext ctx)
    {
        yield return StartCoroutine(SafeRun(Skill.RunOnStart(ctx)));
    }

    private System.Collections.IEnumerator RunUpdate(SpellNodeContext ctx)
    {
        yield return StartCoroutine(SafeRun(Skill.RunOnUpdate(ctx)));
    }

    private System.Collections.IEnumerator RunEnd(SpellNodeContext ctx)
    {
        yield return StartCoroutine(SafeRun(Skill.RunOnEnd(ctx)));
    }

    private System.Collections.IEnumerator SafeRun(System.Collections.IEnumerator routine)
    {
        bool running = true;
        while (running)
        {
            object current = null;
            try
            {
                running = routine.MoveNext();
                if (running) current = routine.Current;
            }
            catch (OutOfManaException)
            {
                Skill.Cancel();
                yield break;
            }
            catch (RuntimeException ex)
            {
                Debug.LogWarning($"[SpellGraphRunner:{Skill?.Name}] {ex.Message}");
                yield break;
            }
            if (running) yield return current;
        }
    }
}
