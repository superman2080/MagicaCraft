# Research — Dash 상태 구현

## 현재 PlayerDash 코드

```csharp
public class PlayerDash : StateBase
{
    public Coroutine dashCor;
    private Player player;
    private DashConfig dashConfig;  // 생성자에서 할당 안 됨

    public PlayerDash(Player caster) : base(caster)
    {
        player = caster;
    }

    public override void Enter()
    {
        base.Enter();
        //dashCor = player.StartCoroutine(DashCoroutine()); // 미구현
    }
}
```

## DashConfig (PlayerCompositeMove 내 중첩 클래스)

```csharp
[Serializable]
public class DashConfig
{
    [Tooltip("대시")] public AnimationCurve dashSpeed = AnimationCurve.Linear(0, 1, 1, 0);
    public float dashTime;
}
```

- `dashSpeed` 커브: X = 시간 정규화(0~1), Y = 속도 배율(0~1 등)
- `dashTime`: 대시 지속 시간(초)
- `Player`에 `dashConfig` 필드 없음 → SerializeField로 추가 필요
- `PlayerDash` 생성자에 전달 필요

## 전환 조건 현황 (PlayerCompositeMove 생성자)

```csharp
// Walk → Dash
() => player.InputHandler.DashPressed && dashState.dashCor == null

// Dash → Walk  ← 버그
() => player.InputHandler.DashPressed == false && dashState.dashCor != null
```

**버그 원인**: `DashPressed`는 LateUpdate에서 매 프레임 false로 초기화됨.
Dash 상태 진입 다음 프레임에 `DashPressed == false && dashCor != null`이 즉시 참이 되어 Walk로 복귀.

**올바른 조건**: `dashCor == null` — 코루틴이 종료되면 Walk로 복귀.

## MoveStatType (Stat.cs)

```csharp
public enum MoveStatType
{
    MOVE_SPEED,
    JUMP_FORCE,
    // DASH_SPEED 없음 → 추가 필요
}
```

대시 기본 속도를 스탯으로 관리해야 함.
추가 후 `MoveStatDataSO` 에셋에서 Inspector 값 설정 필요.

## 대시 방향 결정

- `MoveInput.x != 0`이면 해당 방향 사용
- 입력 없으면 마지막으로 이동한 방향(facing direction) 사용
- `Player`에 `float facingDirection` (1f or -1f) 유지 필요

## 대시 중 물리 처리

- Dash 상태에서는 `PlayerWalk.FixedExecute()`가 실행되지 않음
- `PlayerDash.FixedExecute()`에서 X 속도를 커브 × 기본속도 × 방향으로 직접 제어
- Y 속도는 건드리지 않음 (중력 유지)
- 대시 도중 점프 전환 방지: Walk→Jump 조건만 있어 Dash 중엔 Jump 불가 (현재 구조상 OK)

## 영향 범위

| 파일 | 변경 내용 |
|---|---|
| `Stat.cs` | `MoveStatType`에 `DASH_SPEED` 추가 |
| `Player.cs` | `dashConfig` SerializeField 추가, `facingDirection` 추가 |
| `Player.cs` | `PlayerDash` 구현 (코루틴, Enter/Exit) |
| `Player.cs` | Dash→Walk 전환 조건 수정 |
| `MoveStatDataSO` 에셋 | Inspector에서 `DASH_SPEED` 값 설정 (코드 외 작업) |
