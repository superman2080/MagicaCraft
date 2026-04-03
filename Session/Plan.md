# PlayerJump 구현 계획

> Research.md 분석 기반. 현재 `PlayerJump`의 버그·미구현 항목을 단계별로 정리.

---

## 현재 상태 요약 (문제점)

| # | 문제 | 위치 |
|---|---|---|
| 1 | `AddForce(vel)`에 현재 x속도 포함 → 점프 시 수평 이중 임펄스 | `PlayerJump.Enter` |
| 2 | Jump → Walk/Dash 복귀 전환 조건 없음 → 착지 후 Jump 상태 고착 | `PlayerCompositeMove` 생성자 |
| 3 | `isGrounded` 감지를 FixedUpdate마다 OverlapBox 쿼리 | `PlayerJump.FixedExecute` |

---

## Step 1 — Enter 버그 수정 (점프력 방향 정리)

### 문제
```csharp
// 현재 코드
Vector2 vel = caster.Rb2d.linearVelocity;   // 현재 속도 복사 (x 성분 포함)
vel.y = caster.DefaultStat.Get(DefaultStatType.JUMP_FORCE);
caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);  // x 속도까지 임펄스로 추가됨
```

오른쪽으로 이동 중(vel.x > 0) 점프하면 오른쪽으로도 임펄스가 추가되어 수평 속도가 튀는 현상.

### 해결
```csharp
// 수정 후
caster.Rb2d.AddForce(Vector2.up * caster.DefaultStat.Get(DefaultStatType.JUMP_FORCE), ForceMode2D.Impulse);
```
- y 방향만 임펄스 적용
- 수평 이동은 `PlayerWalk.FixedExecute`가 계속 담당하므로 분리 유지

---

## Step 2 — 착지 후 Walk 복귀 전환 조건 추가

### 문제
```
[Jump] ──(?)──> ???   ← 조건 없음, Jump 상태 고착
```

### 조건 설계
```
Jump → Walk : player.IsGrounded == true && Rb2d.linearVelocity.y <= 0
```

**`vel.y <= 0` 조건이 필요한 이유**: 점프 직후에도 물리 연산 타이밍에 따라
`IsGrounded`가 잠깐 true로 남아있을 수 있음. `vel.y <= 0` (하강 중 또는 정지)일 때만
착지로 판정하면 점프 직후 즉시 복귀를 방지.

### 구현 위치
`PlayerCompositeMove` 생성자에 추가:
```csharp
SubStateMachine.RegisterCondition(
    PlayerMoveState.Jump,
    PlayerMoveState.Walk,
    () => player.IsGrounded && caster.Rb2d.linearVelocity.y <= 0
);
```

---

## Step 3 — isGrounded 감지: OnCollision 이벤트 방식 (확정)

### 결정
OverlapBox 방식 제거 → `Player` MonoBehaviour에 충돌 이벤트 기반으로 교체.

### 설계

**`Player`에 추가할 내용:**
- `private int _groundContactCount` — 현재 접촉 중인 Ground 오브젝트 수
- `public bool IsGrounded => _groundContactCount > 0` — 외부 참조용 프로퍼티
- `OnCollisionEnter2D` — Ground 레이어 접촉 시 카운트 증가
- `OnCollisionExit2D` — Ground 레이어 이탈 시 카운트 감소

**카운트 방식을 쓰는 이유**: bool 단순 토글은 여러 Ground 오브젝트 경계에 걸쳤을 때
Exit 한 번으로 false가 되는 오판 발생. 카운트는 모든 접촉이 끊길 때만 false.

**`PlayerJump` 변경:**
- `isGrounded` 필드 제거
- `FixedExecute` 제거 (OverlapBox 쿼리 불필요)
- `Enter` 내 점프력 적용만 유지

**전환 조건 참조 변경:**
```
// 변경 전
() => player.InputHandler.JumpPressed && jumpState.isGrounded
() => jumpState.isGrounded && caster.Rb2d.linearVelocity.y <= 0

// 변경 후
() => player.InputHandler.JumpPressed && player.IsGrounded
() => player.IsGrounded && caster.Rb2d.linearVelocity.y <= 0
```

### 퍼포먼스 비교
| | OverlapBox (기존) | OnCollision (확정) |
|---|---|---|
| FixedUpdate 비용 | 쿼리 1회 | 0회 |
| 호출 시점 | 매 FixedFrame | 충돌 발생/해제 시만 |
| 다중 엔티티 확장 | 쿼리 × 수 | 이벤트 기반이라 무관 |

---

## Step 4 — 이중 점프 방지 확인

OnCollision 방식에서는 점프 `Enter` 직후 `OnCollisionExit2D`가 발생하며
`_groundContactCount`가 0으로 떨어져 `IsGrounded = false`가 됨.

따라서 AnyState → Jump 조건:
```csharp
() => player.InputHandler.JumpPressed && player.IsGrounded
```
이 자연스럽게 이중 점프를 차단함. 별도 플래그 불필요.

---

## 최종 구현 체크리스트

- [ ] **Step 1**: `PlayerJump.Enter` — `AddForce`를 `Vector2.up * JUMP_FORCE`로 수정
- [ ] **Step 2**: `PlayerCompositeMove` 생성자 — Jump → Walk 전환 조건 추가 (`player.IsGrounded && vel.y <= 0`)
- [ ] **Step 3-a**: `Player`에 `_groundContactCount`, `IsGrounded`, `OnCollisionEnter2D`, `OnCollisionExit2D` 추가
- [ ] **Step 3-b**: `PlayerJump`에서 `isGrounded` 필드 및 `FixedExecute` 제거
- [ ] **Step 3-c**: 전환 조건 참조를 `jumpState.isGrounded` → `player.IsGrounded`로 변경
- [ ] **Step 4**: 씬에 `Ground` 레이어 설정 확인 (`LayerMask.GetMask("Ground")` 유효성)

---

## 완료 후 예상 FSM 흐름

```
서브 FSM (Move 내부):

  [Walk] ──(DashPressed)──────────────────────────────────────> [Dash]
  [Dash] ──(!DashPressed && dashCor!=null)────────────────────> [Walk]
  [Any]  ──(JumpPressed && player.IsGrounded)─────────────────> [Jump]
  [Jump] ──(player.IsGrounded && vel.y <= 0)──────────────────> [Walk]
```

---

## 보류 사항 (점프 외)

- `PlayerDash`: 코루틴 미구현으로 Dash 상태 진입 후 복귀 불가 — 별도 Plan 필요
- `Player.Attack`: NotImplementedException — 마법 스킬 시스템 연동 시 구현
