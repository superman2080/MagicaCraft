using System;

// 마나 추적기 - 마법 실행 중 마나를 실시간으로 차감한다.
// Entity의 마나 Resource를 래핑한다.
// 마나 비용은 각 마법 명령어 구현(SpellCommands)에서 직접 정의한다.
//
// OnDepleted: 마나가 0이 되는 순간 1회 발생.
// SpellProjectile·SpellSkill이 구독하여 마나 소진 시 즉시 중단한다.
public class ManaTracker
{
    private readonly Resource _mana;

    public float Remaining => _mana.CurrentValue;

    // 마나가 완전히 소진될 때 발생한다 (Resource.OnDepleted 위임).
    // SpellProjectile: 구독 시 ReturnToPool
    // SpellSkill(Continuous): 구독 시 Cancel
    public event Action OnDepleted;

    public ManaTracker(Resource mana)
    {
        _mana = mana;
        _mana.OnDepleted += () => OnDepleted?.Invoke();
    }

    // 마나를 소비한다. 부족하면 OutOfManaException을 던진다.
    public void Spend(float amount, int line = 0, int col = 0)
    {
        if (!_mana.TryConsume(amount))
            throw new OutOfManaException(amount, _mana.CurrentValue, line, col);
    }

    // 소비 가능 여부만 확인한다 (시전 전 UI 표시용)
    public bool CanAfford(float amount) => _mana.CurrentValue >= amount;
}

// 마나 부족 예외
// RuntimeException과 구분하여 인게임 UI에서 별도로 처리할 수 있도록 독립 클래스로 정의한다.
public class OutOfManaException : Exception
{
    public float Required  { get; }
    public float Available { get; }

    public OutOfManaException(float required, float available, int line, int col)
        : base($"[{line}:{col}] 마나 부족: {required:F0} 필요, {available:F0} 남음")
    {
        Required  = required;
        Available = available;
    }
}
