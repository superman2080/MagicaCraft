using System;
using System.Collections;
using UnityEngine;

// 모든 노드의 추상 기반.
// NodeId와 EditorPosition은 Blueprint 직렬화·UI 배치에 사용된다.
public abstract class SpellNode
{
    public string  NodeId         { get; } = Guid.NewGuid().ToString();
    public Vector2 EditorPosition { get; set; }

    // UE 이벤트 디스패처 3종 (OnEnter/OnExecute/OnExit)
    public SpellEventDispatcher OnEnterDispatcher   { get; } = new SpellEventDispatcher();
    public SpellEventDispatcher OnExecuteDispatcher { get; } = new SpellEventDispatcher();
    public SpellEventDispatcher OnExitDispatcher    { get; } = new SpellEventDispatcher();

    // 노드 핵심 로직 — 서브클래스가 구현
    public abstract IEnumerator Execute(SpellNodeContext ctx);

    // OnExitDispatcher에 다음 노드를 연결하는 편의 체이닝 메서드
    public SpellNode Then(SpellNode next)
    {
        OnExitDispatcher.Connect(next);
        return next;
    }
}
