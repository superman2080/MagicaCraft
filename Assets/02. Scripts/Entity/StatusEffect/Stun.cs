using UnityEngine;

public class Stun : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.HARD_CC;

    public override int Priority => 3;

    public Stun(int level, float duration, Entity caster = null, params object[] datas) : base(level, duration, caster, datas)
    {
    }

    // ���� ��ȯ�� HardCCState�� Priority�� ���� �ڵ� ó���ϹǷ� ���⼭ ���� �������� �ʴ´�.
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
    }

    public override void Execute()
    {
        base.Execute();
    }

    public override void FixedExecute()
    {
        base.FixedExecute();
        Vector2 vel = caster.Rb2d.linearVelocity;
        vel.x = 0f;
        caster.Rb2d.linearVelocity = vel;
    }

    public override void Exit()
    {
        base.Exit();
    }
}