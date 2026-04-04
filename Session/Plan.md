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

**주의**: 추가 후 `MoveStatDataSO` 에셋에 새 키가 자동 등록됨.
Inspector에서 값을 설정해야 런타임에 사용 가능 (코드 외 작업, 사용자 확인 필요).

---

### [대기] 단계 2 — `Player`에 `dashConfig`, `facingDirection` 추가

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`Player` 클래스 필드 영역에 추가:

```csharp
[SerializeField] private PlayerCompositeMove.DashConfig dashConfig;
public PlayerCompositeMove.DashConfig DashConfig => dashConfig;

private float facingDirection = 1f;
public float FacingDirection => facingDirection;
```

`PlayerWalk.FixedExecute()`에서 방향 업데이트:

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
        player.facingDirection = Mathf.Sign(player.InputHandler.MoveInput.x);
}
```

> `facingDirection`은 `Player`에서 직접 접근하므로 `private` → `internal` 또는 프로퍼티 setter 활용.
> `Player` 클래스가 같은 파일이므로 `private`으로도 중첩 클래스에서 접근 가능 — 확인 필요.

---

### [대기] 단계 3 — `PlayerDash` 전체 구현

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`PlayerDash` 생성자에 `DashConfig` 전달:

```csharp
SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster, caster.DashConfig));
dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;
```

`PlayerDash` 클래스 구현:

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
        // 입력 방향 우선, 없으면 facing 방향
        dashDir = player.InputHandler.MoveInput.x != 0f
            ? Mathf.Sign(player.InputHandler.MoveInput.x)
            : player.FacingDirection;

        dashCor = player.StartCoroutine(DashCoroutine());
    }

    public override void Exit()
    {
        base.Exit();
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
    }
}
```

---

### [대기] 단계 4 — Dash→Walk 전환 조건 수정

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`  
**위치**: `PlayerCompositeMove` 생성자

```csharp
// 변경 전 (버그)
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
- 대시 중 재대시 불가 (Walk→Dash 조건에 `dashCor == null` 이미 포함)
- 대시 중 외부에서 상태 변경 시 코루틴 정리됨 (Exit에서 StopCoroutine)
- Unity 컴파일 에러 없음
