---
name: Player Movement Architecture
description: Player이동 시스템 구조 및 테스트 시 고려사항
type: project
---

## Player 이동 시스템 구조

### 클래스 계층도
- **Player** (MonoBehaviour, Entity 상속, IAttackable + IMovable 구현)
  - `MoveStat moveStat` (MoveStatType.MOVE_SPEED, JUMP_FORCE)
  - `PlayerInputHandler InputHandler` (MoveInput, DashPressed, JumpPressed)
  - `Rigidbody2D Rb2d` (물리 엔진)
  - `StateMachine stateMachine` (PlayerState.Idle, PlayerState.Move)
  - `FacingDirection` (1f 또는 -1f)

### 상태 계층 구조
```
StateMachine (Player.stateMachine)
├── PlayerState.Idle (PlayerIdle)
│   └── FixedExecute: 속도 X축 = 0
└── PlayerState.Move (PlayerCompositeMove extends CompositeStateBase)
    ├── PlayerMoveState.Walk (PlayerWalk)
    │   └── FixedExecute: 속도 = InputHandler.MoveInput × MOVE_SPEED
    ├── PlayerMoveState.Dash (PlayerDash)
    │   ├── Enter: DashCoroutine 시작 (AnimationCurve로 속도 감속)
    │   └── Exit: 코루틴 정리
    └── PlayerMoveState.Jump (PlayerJump)
        └── Enter: AddForce(Vector2.up × JUMP_FORCE, Impulse)
```

### 주요 동작 흐름
1. **대기 → 이동**: MoveInput ≠ 0 OR JumpPressed = true
2. **이동 → 대기**: MoveInput = 0 AND JumpPressed = false AND IsGrounded AND Vy ≤ 0
3. **Walk ↔ Dash**: DashPressed 입력 AND 기존 Dash 없음 ↔ Dash 완료 (dashCor = null)
4. **Walk ↔ Jump**: JumpPressed AND IsGrounded ↔ IsGrounded AND Vy ≤ 0

### MoveStat 시스템
- **공식**: `(baseValue + addValue) × multiplier`
- **LateUpdate 초기화**: `InitStat()` 호출 → addValue=0, multiplier=1 (매 프레임)
- **HardCC 영향**: FearState.Execute()에서 매 프레임 `MOVE_SPEED.Multiply(0.5f)`

### 테스트 시 고려사항
1. **MockRigidbody2D**: linearVelocity는 테스트에서 직접 설정 불가 → Reflection으로 mock 필요
2. **InputHandler**: 테스트에서 MoveInput, DashPressed, JumpPressed 직접 설정 가능
3. **StateMachine**: 상태 전환 조건이 Update/FixedUpdate 호출 후에 검사됨
4. **그라운드 접촉**: OnCollisionEnter2D/Exit2D로 groundContactCount 관리 → mock CollisionContact 필요

## 코드 패턴

### PlayerWalk.FixedExecute
```csharp
float moveSpeed = player.MoveStat.Get(MoveStatType.MOVE_SPEED);
Vector2 vel = caster.Rb2d.linearVelocity;
vel.x = player.InputHandler.MoveInput.x * moveSpeed;
caster.Rb2d.linearVelocity = vel;
```
→ X축만 입력에 의해 변경, Y축 유지 (중력 영향)

### FearState HardCC 배율 적용
```csharp
(caster as IMovable)?.MoveStat.Multiply(MoveStatType.MOVE_SPEED, 0.5f);
```
→ 매 프레임 재적용 (InitStat이 multiplier을 1로 초기화하기 때문)

## PlayMode vs EditMode 테스트
- **EditMode**: Stat 계산, MoveStat 공식, 상태 전환 로직 (MonoBehaviour 불필요)
- **PlayMode**: 실제 Physics2D, Coroutine(Dash), Rigidbody2D 상호작용
