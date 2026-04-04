# Plan — Dash 상태 구현

## 개요

`PlayerDash` 상태를 완전히 구현한다.
대시 속도는 `MoveStatType`에 추가하지 않고 `DashConfig`에 `baseSpeed` 필드를 두어 관리한다.
코루틴이 `DashConfig.dashSpeed` 커브 × `DashConfig.baseSpeed`로 X속도를 제어하고,
코루틴 종료 시 자동으로 Walk로 복귀한다.

---

## 단계별 구현 계획

### [완료] 단계 1 — `DashConfig`에 `baseSpeed` 추가

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`DashConfig` 클래스에 기본 속도 필드 추가:

```csharp
[Serializable]
public class DashConfig
{
    [Tooltip("대시 속도 커브 (X=정규화 시간 0~1, Y=배율)")] 
    public AnimationCurve dashSpeed = AnimationCurve.Linear(0, 1, 1, 0);
    public float dashTime;
    public float baseSpeed;  // 추가 — 커브 배율에 곱할 기본 속도
}
```

---

### [완료] 단계 2 — `Player`에 `dashConfig`, `facingDirection` 추가

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

    if (player.InputHandler.MoveInput.x != 0f)
        player.facingDirection = Mathf.Sign(player.InputHandler.MoveInput.x);
}
```

> `facingDirection`은 `PlayerCompositeMove` 내부 중첩 클래스에서도 접근하므로
> `Player`의 `private` 필드로 선언하고 프로퍼티로 노출한다.

---

### [완료] 단계 3 — `PlayerDash` 전체 구현

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`

`PlayerCompositeMove` 생성자에서 `DashConfig` 전달:

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

        while (elapsed < dashConfig.dashTime)
        {
            float t = elapsed / dashConfig.dashTime;
            float speed = dashConfig.dashSpeed.Evaluate(t) * dashConfig.baseSpeed;

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

### [완료] 단계 4 — Dash→Walk 전환 조건 수정

**파일**: `Assets/02. Scripts/Entity/Player/Player.cs`  
**위치**: `PlayerCompositeMove` 생성자

```csharp
// 변경 전 (버그 — DashPressed는 다음 프레임에 false가 되어 즉시 복귀)
() => player.InputHandler.DashPressed == false && dashState.dashCor != null

// 변경 후
() => dashState.dashCor == null
```

---

## 완료 기준

- 대시 입력 시 `dashTime`동안 커브(`dashSpeed`) × `baseSpeed`에 따라 X속도 제어
- 대시 종료 후 자동으로 Walk 복귀
- 대시 중 재대시 불가 (Walk→Dash 조건의 `dashCor == null` 유지)
- 대시 중 외부에서 상태 변경 시 코루틴 정리됨 (Exit → StopCoroutine)
- `MoveStatType`에 변경 없음 — 대시 속도는 `DashConfig.baseSpeed`로만 관리
- Unity 컴파일 에러 없음
