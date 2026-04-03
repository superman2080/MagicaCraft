# Player.cs 코드 분석 (Research)

파일 경로: `Assets/02. Scripts/Entity/Player/Player.cs`
네임스페이스: `PlayerNamespace`

---

## 1. 전체 구조 개요

```
PlayerNamespace
├── enum PlayerState        — 메인 FSM 상태 키
├── enum PlayerMoveState    — Move 서브 FSM 상태 키
├── class PlayerIdle        — 메인 FSM: Idle 상태
├── class PlayerCompositeMove  — 메인 FSM: Move 상태 (HFSM)
│   ├── class PlayerWalk    — 서브 FSM: 걷기
│   ├── class DashConfig    — 대시 설정 (Serializable)
│   ├── class PlayerDash    — 서브 FSM: 대시 (미구현)
│   └── class PlayerJump    — 서브 FSM: 점프
└── class Player            — Entity + IAttackable MonoBehaviour
```

---

## 2. 열거형

### `PlayerState` (메인 FSM)
| 값 | 설명 |
|---|---|
| `Idle` (0) | 정지 상태 |
| `Move` (1) | 이동 상태 (복합 상태) |
| `Dash` (2) | 현재 미사용 |

### `PlayerMoveState` (서브 FSM)
| 값 | 설명 |
|---|---|
| `Walk` | 좌우 이동 |
| `Dash` | 대시 |
| `Jump` | 점프 |

---

## 3. FSM 계층 구조

### 메인 FSM
```
[Idle] ──(MoveInput != 0 || JumpPressed)──> [Move(HFSM)]
[Move] ──(MoveInput == 0 && !JumpPressed)─> [Idle]
```

### Move 서브 FSM (SubStateMachine)
```
[Walk] ──(DashPressed && dashCor==null)──> [Dash]
[Dash] ──(!DashPressed && dashCor!=null)─> [Walk]
[Any]  ──(JumpPressed && isGrounded)──────> [Jump]
```

> **주의**: Jump에서 Walk/Dash로 돌아오는 전환 조건이 없음 → 착지 후 상태 복귀 미구현

---

## 4. 각 상태 상세

### `PlayerIdle` (메인 FSM)
- **역할**: 입력 없을 때 수평 속도 강제 0으로 제동
- **FixedExecute**: `Rb2d.linearVelocity.x = 0f`
- **핵심**: 미끄러짐 방지. 물리 마찰력 대신 직접 속도 초기화

---

### `PlayerCompositeMove` (메인 FSM, `CompositeStateBase`)
- **역할**: 서브 FSM(Walk/Dash/Jump)을 소유하는 복합 상태
- **Enter**: `SubStateMachine.ChangeState(Walk)` — 항상 Walk로 시작
- **보유 레퍼런스**:
  - `player`: Player 인스턴스
  - `dashState`: 대시 코루틴 상태 확인용
  - `jumpState`: `isGrounded` 값 읽기용

---

### `PlayerWalk` (서브 FSM)
- **역할**: 좌우 이동
- **FixedExecute**:
  ```csharp
  vel.x = InputHandler.MoveInput.x * MOVE_SPEED
  ```
- **핵심**: `linearVelocity`에 직접 대입 (deltaTime 곱하지 않음 — velocity는 m/s 단위)

---

### `DashConfig` (Serializable 설정)
- `dashSpeed`: `AnimationCurve` (0→1→0 기본값) — 대시 속도 곡선
- `dashTime`: 대시 지속 시간
- **현재 상태**: 인스턴스 생성되나 `PlayerDash`에서 사용 안 함

---

### `PlayerDash` (서브 FSM)
- **역할**: 대시 (미구현)
- **보유 필드**: `dashCor` (코루틴 참조, 현재 null 유지)
- **Enter**: `DashCoroutine()` 호출 코드가 주석처리됨
- **전환 조건 문제**: `Dash → Walk` 조건이 `dashCor != null`인데, dashCor가 항상 null이므로 복귀 불가

---

### `PlayerJump` (서브 FSM)
- **역할**: 점프 및 착지 감지
- **필드**: `isGrounded = true` (초기값 true — 게임 시작부터 점프 가능)

#### Enter
```csharp
Vector2 vel = caster.Rb2d.linearVelocity;
vel.y = caster.DefaultStat.Get(DefaultStatType.JUMP_FORCE);
caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);
```
> **버그**: `vel`은 현재 `linearVelocity` 전체를 복사한 값. x 성분이 남아있을 때 `AddForce`하면 현재 수평 속도가 추가 임펄스로 중복 적용됨. 의도대로면 `Vector2.up * JUMP_FORCE`만 전달해야 함.

#### FixedExecute — isGrounded 감지
```csharp
var col = caster.Col as BoxCollider2D;
Vector2 feetPos = position + col.offset + Vector2.down * (col.size.y / 2f + 0.05f);
isGrounded = Physics2D.OverlapBox(feetPos, new Vector2(col.size.x - 0.1f, 0.1f), 0f, LayerMask.GetMask("Ground"));
```
- 콜라이더 하단 5cm 아래에 얇은 박스 쿼리
- 레이어 `"Ground"`와 겹치면 착지 판정
- `FixedUpdate`마다 1회 OverlapBox 쿼리 발생

---

## 5. `Player` 클래스 (MonoBehaviour)

### 상속 구조
```
Player : Entity, IAttackable
```
- `Entity`: Rigidbody2D, Collider2D, SpriteRenderer, DefaultStat, StateMachine, 속성 시스템
- `IAttackable`: `AttackStat`, `AttackLayer`, `Attack()`

### 필드/프로퍼티
| 이름 | 타입 | 설명 |
|---|---|---|
| `InputHandler` | `PlayerInputHandler` | 입력 처리 컴포넌트 (RequireComponent) |
| `AttackStat` | `AttackStat` | 공격 스탯 (ATTACK_SPEED, DAMAGE, ATTACK_DISTANCE) |
| `AttackStatData` | `AttackStatDataSO` | 공격 스탯 ScriptableObject |
| `AttackLayer` | `LayerMask` | Enemy + Entity 레이어 마스크 |
| `animator` | `Animator` | HideInInspector (현재 미활용) |
| `entryStateKey` | `PlayerState` | Inspector에서 설정하는 초기 상태 (현재 Idle=0) |

### `InitializeStateMachine`
```csharp
// 메인 FSM 상태 등록
RegisterState(PlayerState.Idle, new PlayerIdle(this));
RegisterState(PlayerState.Move, new PlayerCompositeMove(this));

// 전환 조건
Idle → Move : MoveInput != zero || JumpPressed
Move → Idle : MoveInput == zero && !JumpPressed
```
- `Entity.InitializeStateMachine()`(base)도 호출 → HardCC/Concentration 상태도 등록됨

### `Attack` 미구현
```csharp
public void Attack(IDamagable target, float amount)
{
    throw new NotImplementedException();
}
```

---

## 6. 사용 중인 스탯 (`DefaultStatType`)

| 스탯 | 사용 위치 |
|---|---|
| `MOVE_SPEED` | `PlayerWalk.FixedExecute` |
| `JUMP_FORCE` | `PlayerJump.Enter` |
| `MAX_HP`, `HP_REGEN` | Entity 공통 (Player는 직접 사용 안 함) |

---

## 7. PlayerInputHandler (참조만 존재, 파일 별도)

Player.cs에서 사용되는 프로퍼티 목록 (MCP 런타임 데이터 기준):

| 프로퍼티 | 타입 | 설명 |
|---|---|---|
| `MoveInput` | `Vector2` | 좌우 입력 (x만 사용) |
| `JumpPressed` | `bool` | 점프 입력 |
| `DashPressed` | `bool` | 대시 입력 |
| `LeftMouseButtonPressed` | `bool` | 마법 사용 (예정) |
| `RightMouseButtonPressed` | `bool` | 마법 사용 (예정) |
| `MousePos` | `Vector2` | 마우스 위치 |

---

## 8. 알려진 버그 / 미구현 사항

| # | 위치 | 내용 |
|---|---|---|
| 버그 | `PlayerJump.Enter` | `AddForce(vel)`에 현재 x속도가 포함되어 수평 이중 임펄스 발생 |
| 미구현 | `PlayerDash.Enter` | 대시 코루틴 주석처리. `dashCor`가 항상 null → Dash→Walk 복귀 불가 |
| 미구현 | `PlayerJump` | Jump 종료 후 Walk/Dash로 돌아오는 전환 조건 없음 |
| 미구현 | `Player.Attack` | `NotImplementedException` |
| 설계 이슈 | `PlayerJump.isGrounded` | Jump 서브 상태 내부에 있으나 OverlapBox는 다른 서브 상태에서도 동작해야 할 수 있음 |

---

## 9. Rigidbody2D 설정 (씬 Inspector 값)

| 항목 | 값 |
|---|---|
| `gravityScale` | 1.0 (중력 활성) |
| `freezeRotation` | **true** (Z축 회전 고정) |
| `constraints` | 4 (FreezeRotation) |
| `bodyType` | Dynamic |
| `mass` | 1.0 |

---

## 10. FSM 완전 전환 다이어그램

```
┌──────────────────────────────── Entity 공통 AnyState ────────────────────────────────┐
│  AnyState ──(HasEffect HardCC)──> [HardCCState]                                      │
│  [HardCCState] ──(!HasEffect HardCC)──> EntryState(Idle)                             │
└──────────────────────────────────────────────────────────────────────────────────────┘

메인 FSM:
  [Idle] ←──────────────────────────────────────────┐
    │ MoveInput!=0 || JumpPressed                   │ MoveInput==0 && !JumpPressed
    ▼                                               │
  [Move / PlayerCompositeMove] ───────────────────── ┘
    │ Enter → SubFSM.ChangeState(Walk)
    │
    └── 서브 FSM:
          [Walk] ──(DashPressed && dashCor==null)──> [Dash]
          [Dash] ──(!DashPressed && dashCor!=null)─> [Walk]  ← dashCor 항상 null이라 불가
          [Any]  ──(JumpPressed && isGrounded)──────> [Jump]
          [Jump] ──(?)──────────────────────────────> ???    ← 미구현
```
