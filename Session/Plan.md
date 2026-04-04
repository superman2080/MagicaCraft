# Plan — Dash 상태 구현

## 개요

`PlayerDash` 상태를 완전히 구현한다.
코루틴 기반으로 `DashConfig.dashSpeed` 커브와 `DASH_SPEED` 스탯을 조합해 대시 속도를 제어하고,
대시 종료 시 자동으로 Walk로 복귀한다.

---

## 단계별 구현 계획

### [대기] 단계 1 — `MoveStatType`에 `DASH_SPEED` 추가

**파일**: `Assets/02. Scripts/Entity/Stat/Stat.cs`

```csharp
public enum MoveStatType
{
    MOVE_SPEED,
    JUMP_FORCE,
    DASH_SPEED,  // 추가
}
```

**주의**: `MoveStatDataSO` 에셋에서 Inspector로 `DASH_SPEED` 값 설정 필요 (코드 외 작업).
`Stat<T>` 생성자의 `InitStatData`는 StatData에 없는 키를 건너뛰기 때문에,
에셋에 값이 없으면 `Get(DASH_SPEED)` 시 KeyNotFoundException 발생.
→ 에셋 설정은 구현 완료 후 사용자가 직접 수행.

---

### [대기] 단계 2 — `Player`에 `dashConfig`, `FacingDirection`, `IsDashing` 추가

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`Player` 클래스 필드 영역에 추가:

```csharp
[SerializeField] private PlayerCompositeMove.DashConfig dashConfig;
public PlayerCompositeMove.DashConfig DashConfig => dashConfig;

public float FacingDirection { get; set; } = 1f;
public bool IsDashing { get; set; } = false;
```

`PlayerWalk.FixedExecute()`에서 방향 업데이트 추가:

```csharp
public override void FixedExecute()
{
    base.FixedExecute();
    float moveSpeed = player.MoveStat.Get(MoveStatType.MOVE_SPEED);

    Vector2 vel = caster.Rb2d.linearVelocity;
    vel.x = player.InputHandler.MoveInput.x * moveSpeed;
    caster.Rb2d.linearVelocity = vel;

    // 이동 중이면 facing 방향 갱신
    if (player.InputHandler.MoveInput.x != 0f)
        player.FacingDirection = Mathf.Sign(player.InputHandler.MoveInput.x);
}
```

> `FacingDirection`과 `IsDashing`은 같은 네임스페이스 내 중첩 클래스(PlayerWalk, PlayerDash)가 접근해야 하므로 public 프로퍼티로 선언.

---

### [대기] 단계 3 — Move→Idle 전환 조건에 `!IsDashing` 추가

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`
**위치**: `Player.InitializeStateMachine()`

대시 중 `MoveInput == Vector2.zero` 상태(입력 없이 대시 중)가 되면
현재 조건이 즉시 참이 되어 Idle로 전환되어 코루틴이 중단되는 버그 방지.

```csharp
// 변경 전
stateMachine.RegisterCondition(PlayerState.Move, PlayerState.Idle,
    () => InputHandler.MoveInput == Vector2.zero && InputHandler.JumpPressed == false && IsGrounded && Rb2d.linearVelocity.y <= 0f);

// 변경 후
stateMachine.RegisterCondition(PlayerState.Move, PlayerState.Idle,
    () => InputHandler.MoveInput == Vector2.zero && InputHandler.JumpPressed == false && IsGrounded && Rb2d.linearVelocity.y <= 0f && !IsDashing);
```

---

### [대기] 단계 4 — `PlayerDash` 전체 구현

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`PlayerCompositeMove` 생성자에서 `DashConfig` 전달:

```csharp
SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster, caster.DashConfig));
dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;
```

`PlayerDash` 클래스 전체 교체:

```csharp
public class PlayerDash : StateBase
{
    public Coroutine dashCor;
    private Player player;
    private DashConfig dashConfig;
    private float dashDir;

    public PlayerDash(Player caster, DashConfig config) : base(caster)
    {
        player = caster;
        dashConfig = config;
    }

    public override void Enter()
    {
        base.Enter();
        // 입력 방향 우선, 없으면 마지막 facing 방향
        dashDir = player.InputHandler.MoveInput.x != 0f
            ? Mathf.Sign(player.InputHandler.MoveInput.x)
            : player.FacingDirection;

        player.IsDashing = true;
        dashCor = player.StartCoroutine(DashCoroutine());
    }

    public override void Exit()
    {
        base.Exit();
        player.IsDashing = false;
        if (dashCor != null)
        {
            player.StopCoroutine(dashCor);
            dashCor = null;
        }
    }

    private IEnumerator DashCoroutine()
    {
        float elapsed = 0f;
        float dashSpeed = player.MoveStat.Get(MoveStatType.DASH_SPEED);

        while (elapsed < dashConfig.dashTime)
        {
            float t = elapsed / dashConfig.dashTime;
            float speed = dashConfig.dashSpeed.Evaluate(t) * dashSpeed;

            Vector2 vel = caster.Rb2d.linearVelocity;
            vel.x = dashDir * speed;
            caster.Rb2d.linearVelocity = vel;

            elapsed += Time.deltaTime;
            yield return null;
        }

        dashCor = null;  // 종료 → Dash→Walk 조건 충족
        player.IsDashing = false;
    }
}
```

---

### [대기] 단계 5 — Dash→Walk 전환 조건 수정

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`
**위치**: `PlayerCompositeMove` 생성자

```csharp
// 변경 전 (버그: DashPressed는 다음 프레임에 이미 false)
SubStateMachine.RegisterCondition(PlayerMoveState.Dash, PlayerMoveState.Walk,
    () => player.InputHandler.DashPressed == false && dashState.dashCor != null);

// 변경 후
SubStateMachine.RegisterCondition(PlayerMoveState.Dash, PlayerMoveState.Walk,
    () => dashState.dashCor == null);
```

---

## 완료 기준

- 대시 입력 시 `dashTime`동안 `dashSpeed` 커브에 따라 X속도 제어
- 대시 종료 후 자동으로 Walk 복귀
- 대시 중 입력이 없어도 Idle로 빠지지 않음 (`IsDashing` 가드)
- 대시 중 재대시 불가 (Walk→Dash 조건에 `dashCor == null` 이미 포함)
- 대시 중 외부에서 상태 변경 시 코루틴 정리됨 (Exit에서 StopCoroutine)
- Unity 컴파일 에러 없음

---

## 구현 후 수동 작업 (사용자)

- `MoveStatDataSO` 에셋에서 `DASH_SPEED` 값 설정 (예: 15.0)
- `Player` Inspector에서 `DashConfig.dashTime`, `DashConfig.dashSpeed` 커브 설정
