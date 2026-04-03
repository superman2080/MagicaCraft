# Plan — PlayerJump 구현

## 개요

현재 `PlayerJump`는 세 가지 문제를 가진다.
1. `Enter()`에서 x 속도가 임펄스에 포함되는 버그
2. 착지 후 Walk로 돌아오는 전환 조건 없어 Jump 상태 고착
3. `isGrounded`가 Jump 상태 내부에만 존재해 다른 상태에서 판정 불가

모든 수정은 `Player.cs` 한 파일에서 이루어진다.

---

## 단계별 구현 계획

### [완료] 단계 1 — `Player`에 `IsGrounded` 추가 (OnCollision 방식)

**위치**: `Player` 클래스 본체

```csharp
private int _groundContactCount = 0;
public bool IsGrounded => _groundContactCount > 0;

private void OnCollisionEnter2D(Collision2D col)
{
    if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        _groundContactCount++;
}

private void OnCollisionExit2D(Collision2D col)
{
    if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        _groundContactCount--;
}
```

- 카운트 방식: 여러 Ground 오브젝트 경계에 걸쳤을 때 한 쪽 Exit으로 오판 방지
- 점프 Enter 직후 OnCollisionExit2D 발생 → `IsGrounded = false` → 이중 점프 자동 차단

---

### [완료] 단계 2 — `PlayerJump.Enter()` 버그 수정

**위치**: `PlayerJump.Enter()`

```csharp
// 변경 전
Vector2 vel = caster.Rb2d.linearVelocity;
vel.y = player.MoveStat.Get(MoveStatType.JUMP_FORCE);
caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);

// 변경 후
caster.Rb2d.AddForce(Vector2.up * player.MoveStat.Get(MoveStatType.JUMP_FORCE), ForceMode2D.Impulse);
```

y 방향 임펄스만 적용. 수평 이동은 `PlayerWalk.FixedExecute()`가 담당.

---

### [완료] 단계 3 — `PlayerJump.FixedExecute()` 및 `isGrounded` 제거

**위치**: `PlayerJump` 클래스

- `isGrounded` 필드 제거
- `FixedExecute()` 메서드 제거 (OverlapBox 쿼리 불필요)

---

### [완료] 단계 4 — 전환 조건 수정 및 Jump→Walk 추가

**위치**: `PlayerCompositeMove` 생성자

```csharp
// 변경 전
SubStateMachine.RegisterAnyStateTransition(PlayerMoveState.Jump,
    () => player.InputHandler.JumpPressed && jumpState.isGrounded == true);

// 변경 후
SubStateMachine.RegisterAnyStateTransition(PlayerMoveState.Jump,
    () => player.InputHandler.JumpPressed && player.IsGrounded);

// 신규 추가 — 착지 후 Walk 복귀
SubStateMachine.RegisterCondition(PlayerMoveState.Jump, PlayerMoveState.Walk,
    () => player.IsGrounded && caster.Rb2d.linearVelocity.y <= 0f);
```

`vel.y <= 0` 조건: 점프 직후 한 프레임 IsGrounded가 true로 남는 타이밍 문제 방지.

---

## 완료 기준

- 점프 시 수평 속도 이중 적용 없음
- 착지 후 Walk 상태로 정상 복귀
- 이중 점프 불가 (IsGrounded = false 동안 Jump 전환 조건 차단)
- Unity 컴파일 에러 없음 (MCP read_console 확인)
