using UnityEngine;

/// <summary>
/// 캐스팅 스킬 베이스, CC에도 안 끊김
/// </summary>

public class CastingSkillConfig
{
    
}

public class CastingSkillBase : ActiveSkillBase
{
    public CastingSkillBase(Entity caster) : base(caster)
    {
    }

    public override ActiveSkillType Type => ActiveSkillType.Casting;


}
