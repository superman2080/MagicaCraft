using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이벤트 디스패처.
// UE 이벤트 디스패처처럼 동작하되, DSL 코드(Blueprint)와 연결 노드를 함께 보유한다.
// Fire() 호출 시: DSL 실행 → 연결된 노드 순서대로 실행 (직렬)
public class SpellEventDispatcher
{
    // Blueprint(DSL) 슬롯 — 플레이어가 작성하는 코드
    public SkillCodeSlot CodeSlot { get; } = new SkillCodeSlot();

    // 연결된 다음 노드 목록 (C++ 노드 체인)
    private readonly List<SpellNode> connectedNodes = new();

    public bool HasConnections => connectedNodes.Count > 0 || !string.IsNullOrWhiteSpace(CodeSlot.BuildCode());

    public void Connect(SpellNode node)    => connectedNodes.Add(node);
    public void Disconnect(SpellNode node) => connectedNodes.Remove(node);

    // 직렬 실행: DSL 먼저 → 각 노드를 순서대로 완료 후 다음 실행
    public IEnumerator Fire(SpellNodeContext ctx)
    {
        RunDSL(ctx);
        foreach (var node in connectedNodes)
            yield return ctx.Runner.StartCoroutine(node.Execute(ctx));
    }

    // 병렬 실행: DSL 먼저 → 모든 노드를 동시 시작, 전부 완료 후 반환
    public IEnumerator FireParallel(SpellNodeContext ctx)
    {
        RunDSL(ctx);

        if (connectedNodes.Count == 0) yield break;

        int[] remaining = { connectedNodes.Count };
        foreach (var node in connectedNodes)
        {
            var captured = node;
            ctx.Runner.StartCoroutine(RunAndDecrement(captured, ctx, remaining));
        }

        while (remaining[0] > 0)
            yield return null;
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private IEnumerator RunAndDecrement(SpellNode node, SpellNodeContext ctx, int[] counter)
    {
        yield return ctx.Runner.StartCoroutine(node.Execute(ctx));
        counter[0]--;
    }

    private void RunDSL(SpellNodeContext ctx)
    {
        string code = CodeSlot.BuildCode();
        if (string.IsNullOrWhiteSpace(code)) return;

        // Blackboard를 extraVars로 DSL에 노출 (victim, hitPosition 등 접근 가능)
        var extraVars = new Dictionary<string, object>(ctx.Blackboard);

        try
        {
            var tokens = new Lexer(code).Tokenize();
            var ast    = new Parser(tokens).Parse();
            var interp = new Interpreter(ctx.SpellContext, ctx.Mana, extraVars);
            interp.Run(ast);
        }
        catch (OutOfManaException)
        {
            // ManaTracker.OnDepleted → Cancel() 처리됨
        }
        catch (RuntimeException ex)
        {
            Debug.LogWarning($"[SpellEventDispatcher] DSL 런타임 오류: {ex.Message}");
        }
        catch (LexerException ex)
        {
            Debug.LogWarning($"[SpellEventDispatcher] DSL 코드 오류: {ex.Message}");
        }
        catch (ParserException ex)
        {
            Debug.LogWarning($"[SpellEventDispatcher] DSL 코드 오류: {ex.Message}");
        }
    }
}
