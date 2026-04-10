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

    public float Cooldown { get; protected set; } = 0f;

    public bool CanUsingSkill { get; protected set; }

    public ActiveSkillBase(Entity caster)
    {
        this.caster = caster;
    }

    public virtual void Perform()
    {
        if (CanUsingSkill == false)
            return;

        CanUsingSkill = false;
        TimerManager.Instance.SetTimer(Cooldown, () => CanUsingSkill = true);
    }

    protected virtual void Enter() { }

    protected virtual void Execute() { }
    protected virtual void Exit() { }
}

