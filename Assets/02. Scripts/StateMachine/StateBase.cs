using UnityEngine;

// [FSM] 단일 상태 베이스 클래스
// IState를 구현하는 모든 상태의 공통 베이스.
// 각 메서드는 virtual이므로 필요한 것만 override하여 사용한다.
public abstract class StateBase<T> : IState<T> where T : MonoBehaviour
{
    public virtual void Enter(T caster) { }

    public virtual void Execute(T caster) { }

    public virtual void FixedExecute(T caster) { }

    public virtual void Exit(T caster) { }
}
