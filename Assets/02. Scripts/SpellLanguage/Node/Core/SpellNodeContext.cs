using System.Collections.Generic;
using UnityEngine;

// 노드 실행 런타임 컨텍스트.
// 기존 SpellContext(DSL 명령어 레지스트리)를 내포하고,
// 노드 간 데이터를 전달하는 Blackboard를 추가한다.
public class SpellNodeContext
{
    public const string KeyCaster      = "caster";
    public const string KeyTarget      = "target";
    public const string KeyVictim      = "victim";
    public const string KeyHitPosition = "hitPosition";

    public Entity        Caster      => SpellContext.Caster;
    public Entity        Target      => SpellContext.Target;
    public ManaTracker   Mana        { get; }
    public MonoBehaviour Runner      { get; }
    public SpellContext  SpellContext { get; }

    private readonly Dictionary<string, object> blackboard = new();

    // Blackboard를 DSL extraVars로 노출 (SpellEventDispatcher에서 Interpreter에 주입)
    public IReadOnlyDictionary<string, object> Blackboard => blackboard;

    public SpellNodeContext(SpellContext spellContext, ManaTracker mana, MonoBehaviour runner)
    {
        SpellContext      = spellContext;
        Mana              = mana;
        Runner            = runner;
        spellContext.Mana = mana;

        blackboard[KeyCaster] = spellContext.Caster;
        if (spellContext.Target != null)
            blackboard[KeyTarget] = spellContext.Target;
    }

    public void   Set(string key, object value) => blackboard[key] = value;
    public object Get(string key)               => blackboard.TryGetValue(key, out var v) ? v : null;

    public bool TryGet<T>(string key, out T value)
    {
        if (blackboard.TryGetValue(key, out var v) && v is T t)
        {
            value = t;
            return true;
        }
        value = default;
        return false;
    }

    // Blackboard를 얕은 복사한 독립 컨텍스트 반환.
    // 투사체 히트 등 분기 실행 시 원본 ctx 오염 방지.
    public SpellNodeContext Fork()
    {
        var fork = new SpellNodeContext(SpellContext, Mana, Runner);
        foreach (var (k, v) in blackboard)
            fork.blackboard[k] = v;
        return fork;
    }
}
