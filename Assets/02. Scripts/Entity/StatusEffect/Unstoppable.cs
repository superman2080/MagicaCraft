using UnityEngine;

public class Unstoppable : StatusEffect
{
    public Unstoppable(int level, float duration, Entity caster = null) : base(level, duration, caster)
    {
    }

    public override StatusEffectType Type => StatusEffectType.BUFF;

    public override void Exit(Entity target)
    {
    }

    public override void Enter(Entity target)
    {
    }

    public override void Execute(Entity target)
    {
    }
}
