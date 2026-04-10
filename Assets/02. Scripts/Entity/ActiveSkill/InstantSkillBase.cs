using UnityEngine;

public class InstantSkillBase : ActiveSkillBase
{
    public InstantSkillBase(Entity caster) : base(caster)
    {
    }

    public override ActiveSkillType Type => ActiveSkillType.Instant;
}
