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
    public override void OnStart(Entity target) { }

    public override void OnUpdate(Entity target) { }

    public override void OnExit(Entity target) { }
}
