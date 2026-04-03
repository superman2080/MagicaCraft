# PlayerJump 구현 Research

## 현재 PlayerJump 코드

```csharp
public class PlayerJump : StateBase
{
    private Player player;
    public bool isGrounded = true;  // 초기값 true

    public override void Enter()
    {
        Vector2 vel = caster.Rb2d.linearVelocity;
        vel.y = player.MoveStat.Get(MoveStatType.JUMP_FORCE);
        caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);  // ← 버그
    }

    public override void FixedExecute()
    {
        // OverlapBox로 착지 판정 → isGrounded 갱신
        isGrounded = Physics2D.OverlapBox(..., LayerMask.GetMask("Ground"));
    }
}
```

---

## 문제점 분석

### 버그 1 — Enter(): x 속도가 임펄스에 포함됨

```csharp
Vector2 vel = caster.Rb2d.linearVelocity; // x 성분 포함
vel.y = JUMP_FORCE;
caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);
// → 현재 수평 이동속도까지 임펄스로 추가됨 (수평 이중 적용)
```

**수정**: `Vector2.up * JUMP_FORCE` 만 임펄스로 적용

---

### 버그 2 — Jump 상태에서 빠져나오는 전환 조건 없음

`PlayerCompositeMove` 생성자의 전환 등록:
```csharp
// Walk → Dash
// Dash → Walk
// AnyState → Jump (JumpPressed && jumpState.isGrounded)
// Jump → ??? ← 없음
```

착지해도 Jump 상태에서 Walk로 돌아오는 조건이 없어 Jump 상태 고착 발생.

**수정**: `Jump → Walk` 조건 추가 — 착지 시 (`IsGrounded && vel.y <= 0`)

---

### 설계 문제 — isGrounded가 Jump 상태 내부에만 존재

`jumpState.isGrounded`는 `PlayerJump.FixedExecute()`가 호출될 때만 갱신됨.
Walk/Dash 등 다른 상태일 때는 FixedExecute가 호출되지 않아 `isGrounded`가 **마지막 Jump 상태 종료 시 값으로 고정**.

- 초기값이 `true`라 첫 점프는 가능
- 착지 후 Walk로 돌아온 뒤 다음 점프 조건도 마지막 isGrounded 값에 의존
- 이론적으로 동작하지만, 착지 판정이 올바르게 이루어지지 않을 수 있음

**수정**: `IsGrounded`를 `Player`에 이동시켜 항상 최신 상태 유지

---

## isGrounded 판정 방식 비교

| 방식 | 장점 | 단점 |
|---|---|---|
| **OverlapBox** (현재) | 간단 | FixedUpdate마다 쿼리 발생, Jump 상태에서만 갱신 |
| **OnCollision 이벤트** | 이벤트 기반(비용 없음), 항상 최신 | 복수 Ground 오브젝트 처리 필요 (카운트 방식) |

→ **OnCollision 카운트 방식** 채택: `Player`에 `_groundContactCount` + `IsGrounded` 프로퍼티 추가

---

## 관련 전환 조건 (변경 전/후)

```
// AnyState → Jump
변경 전: () => player.InputHandler.JumpPressed && jumpState.isGrounded == true
변경 후: () => player.InputHandler.JumpPressed && player.IsGrounded

// Jump → Walk (신규)
추가:    () => player.IsGrounded && caster.Rb2d.linearVelocity.y <= 0f
```

`vel.y <= 0` 조건이 필요한 이유:
점프 직후 물리 연산 타이밍에 따라 `IsGrounded`가 한 프레임 true로 남아있을 수 있음.
하강 중 또는 정지(`vel.y <= 0`)일 때만 착지로 판정해 점프 직후 즉시 복귀 방지.

---

## 이중 점프 방지

OnCollision 방식에서 점프 Enter 직후 `OnCollisionExit2D`가 발생 → `_groundContactCount = 0` → `IsGrounded = false`.
AnyState → Jump 조건 `JumpPressed && player.IsGrounded`가 자연스럽게 이중 점프 차단.

---

## 영향 파일

| 파일 | 변경 |
|---|---|
| `Player.cs` | `IsGrounded` 프로퍼티, `OnCollisionEnter/Exit2D` 추가 |
| `Player.cs` | `PlayerJump.Enter()` 버그 수정 |
| `Player.cs` | `PlayerJump.FixedExecute()` 제거 (OverlapBox 불필요) |
| `Player.cs` | `PlayerJump.isGrounded` 필드 제거 |
| `Player.cs` | AnyState→Jump 전환 조건 수정, Jump→Walk 전환 추가 |
