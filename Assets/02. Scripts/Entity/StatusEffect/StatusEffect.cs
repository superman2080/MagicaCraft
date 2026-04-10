using System;
using UnityEngine;

[Flags]
public enum StatusEffectType
{
    NONE = 0,
    NORMAL_CC = 1 << 0,
    HARD_CC = 1 << 1,
    BUFF = 1 << 2,
}

public abstract class StatusEffect
{
    public int Level { get; protected set; }
    public float Duration { get; set; }
    public float MaxDuration { get; protected set; }
    public Entity Caster { get; protected set; }

    public abstract StatusEffectType Type { get; }

    // 동일 타입 CC가 동시에 여러 개 걸렸을 때 더 높은 값이 우선 적용된다.
    public virtual int Priority => 0;

    public StatusEffect(int level, float duration, Entity caster = null, params object[] datas)
    {
        this.Level = level;
        this.Duration = MaxDuration = duration;
        this.Caster = caster;
    }

    public abstract void Enter(Entity target);

    public abstract void Execute(Entity target);

    public abstract void Exit(Entity target);
}
