using UnityEngine;

public enum ActiveSkillType
{
    Instant,
    Casting,
    Charging,
    Toggle,
}

public delegate void OnSkillCastEnter(Entity entity, ActiveSkillBase skillBase);
public delegate void OnSkillCastExecute(Entity entity, ActiveSkillBase skillBase);
public delegate void OnSkillCastExit(Entity entity, ActiveSkillBase skillBase);

public abstract class ActiveSkillBase
{
    protected Entity caster;
    public abstract ActiveSkillType Type { get; }

    public ActiveSkillBase(Entity caster)
    {
        this.caster = caster;
    }

    public abstract void Perform();

    protected virtual void Enter() { }

    protected virtual void Execute() { }
    protected virtual void Exit() { }

    protected virtual void Toggle() { }
}

