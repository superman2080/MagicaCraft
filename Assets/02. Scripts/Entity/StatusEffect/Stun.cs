using UnityEngine;

public class Stun : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.HARD_CC;

    // 공포는 기절(Priority 0)보다 우선순위가 높다.
    public override int Priority => 1;

    public Stun(int level, float duration, Entity caster = null, params object[] datas) : base(level, duration, caster, datas)
    {
    }

    // 상태 전환은 HardCCState가 Priority에 따라 자동 처리하므로 여기서 직접 변경하지 않는다.
    public override void Enter(Entity target) { }

    public override void Execute(Entity target) { }

    public override void Exit(Entity target) { }
}

public class StunState : StateBase
{
    private Entity fearCaster;
    private Vector2 direction;

    public StunState(Entity caster) : base(caster) { }

    public override void Enter()
    {
        base.Enter();
        caster.SpriteRenderer.color = Color.yellow;
    }

    public override void Execute()
    {
        base.Execute();
        caster.SpriteRenderer.color = Color.yellow;
    }

    public override void FixedExecute()
    {
        base.FixedExecute();
        caster.SpriteRenderer.color = Color.yellow;
        Vector2 vel = caster.Rb2d.linearVelocity;
        vel.x = 0f;
        caster.Rb2d.linearVelocity = vel;
    }

    public override void Exit()
    {
        base.Exit();
        caster.SpriteRenderer.color = Color.white;
    }
}