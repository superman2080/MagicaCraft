using UnityEngine;

public class Fear : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.HARD_CC;

    // 공포는 기절(Priority 0)보다 우선순위가 높다.
    public override int Priority => 2;

    public Fear(int level, float duration, Entity caster = null, params object[] datas) : base(level, duration, caster, datas)
    {
    }

    // 상태 전환은 HardCCState가 Priority에 따라 자동 처리하므로 여기서 직접 변경하지 않는다.
    public override void Enter(Entity target) { }

    public override void Execute(Entity target) { }

    public override void Exit(Entity target) { }
}


public class FearState : StateBase<Entity>
{
    private Entity fearCaster;
    private Vector2 direction;

    public FearState(Entity caster) : base(caster) { }

    public override void Enter()
    {
        base.Enter();
        fearCaster = caster.GetEffect<Fear>()?.Caster;
        direction = fearCaster.transform.position.x > caster.transform.position.x ? Vector2.left : Vector2.right;
    }

    public override void Execute()
    {
        base.Execute();
        // Stat<T>.InitStat()이 매 LateUpdate에서 배율을 1로 초기화하므로 매 프레임 재적용한다.
        (caster as IMovable)?.MoveStat.Multiply(MoveStatType.MOVE_SPEED, 0.5f);
    }

    public override void FixedExecute()
    {
        base.FixedExecute();
        caster.Rb2d.MovePosition((Vector2)caster.transform.position + direction * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        // MOVE_SPEED 배율은 InitStat()이 다음 프레임에 자동으로 1로 복원한다.
    }
}