# 마법 명령어 개발 가이드

새로운 마법 명령어를 추가할 때 참고하는 문서입니다.

---

## 1. 명령어란?

플레이어가 DSL 코드에서 호출하는 함수입니다.

```c
Bolt(target, Fire, power * 1.5);   // 명령어 호출
Heal(self, 30);
```

내부적으로는 `SpellContext`의 명령어 레지스트리에 등록된 C# 함수가 실행됩니다.

---

## 2. 명령어 시그니처

```csharp
Func<List<object>, SpellContext, object>
//   ───────────   ────────────  ──────
//   평가된 인자    실행 컨텍스트   반환값 (없으면 null)
```

---

## 3. 명령어 작성 방법

### 기본 구조

```csharp
private static object MyCommand(List<object> args, SpellContext ctx)
{
    const string sig = "MyCommand(target, element, damage)"; // 오류 메시지용

    // 1. 인자 개수 확인
    if (args.Count != 3)
        throw new RuntimeException($"{sig}: 인자 3개 필요");

    // 2. 인자 타입 검증 및 추출
    var target   = RequireEntity(args[0], sig);
    var element  = RequireElement(args[1], sig);
    float damage = (float)RequireDouble(args[2], sig);

    // 3. 게임 로직 실행
    target.TakeDamage(ctx.Caster as IAttackable, damage, element);

    return null; // 반환값이 없는 명령어는 null
}
```

### 인자 검증 헬퍼

`SpellCommands` 내부에 정의된 헬퍼를 사용합니다.

| 헬퍼 | 기대 타입 | 실패 시 |
|---|---|---|
| `RequireEntity(val, sig)` | `Entity` | RuntimeException |
| `RequireElement(val, sig)` | `ElementType` | RuntimeException |
| `RequireDouble(val, sig)` | `double` | RuntimeException |
| `RequireVector2(val, sig)` | `Vector2` or `Entity.position` | RuntimeException |

---

## 4. SpellContext에서 사용 가능한 것들

```csharp
ctx.Caster          // 마법을 시전한 Entity
ctx.Target          // 대상 Entity (null 가능)
ctx.Caster as IAttackable  // IAttackable 캐스팅 (구현 안 했으면 null)
```

---

## 5. 데미지 적용 패턴

### 단일 대상

```csharp
target.TakeDamage(ctx.Caster as IAttackable, damage, element);
```

### 범위 대상

```csharp
var attacker  = ctx.Caster as IAttackable;
var layerMask = attacker != null ? (int)attacker.AttackLayer : Physics2D.AllLayers;
var hits      = Physics2D.OverlapCircleAll(center, radius, layerMask);

foreach (var hit in hits)
{
    if (hit.TryGetComponent<IDamagable>(out var damagable))
        damagable.TakeDamage(attacker, damage, element);
}
```

### 회복

```csharp
target.Heal(ctx.Caster, amount);
```

---

## 6. 속성(ElementType) 처리

플레이어 코드에서 `Fire`, `Water` 등은 환경에 미리 주입된 `ElementType` 상수입니다.

```c
Bolt(target, Fire, 30);      // args[1] = ElementType.Fire
Bolt(target, None, 30);      // args[1] = ElementType.None → 1.0배
```

`ElementChart.GetMultiplier(attacker, defender)`로 배율 확인 가능.

---

## 7. 등록 및 티어 관리

모든 명령어는 `SpellCommands`의 `RegisterTierX` 메서드에서 등록합니다.

```csharp
public static void RegisterTier0(SpellContext ctx)
{
    ctx.RegisterCommand("Bolt",      Bolt);
    ctx.RegisterCommand("Heal",      Heal);
    // 새 티어 0 명령어 추가
    ctx.RegisterCommand("NewCommand", NewCommand);
}

public static void RegisterTier1(SpellContext ctx)
{
    ctx.RegisterCommand("Explode", Explode);
    ctx.RegisterCommand("Shield",  Shield);
}
```

미등록 명령어를 플레이어가 호출하면 자동으로 RuntimeException이 발생합니다.
해금 시스템은 `RegisterTierX`를 적절한 시점에 호출하는 것으로 구현합니다.

---

## 8. 네이밍 규칙

| 항목 | 규칙 | 이유 |
|---|---|---|
| 명령어 이름 | **PascalCase** | C# 메서드 컨벤션 |
| C# private 메서드 | **PascalCase** (static private) | 일관성 |
| 오류 메시지의 시그니처 | 실제 호출 형태 그대로 | 플레이어 친화적 |

---

## 9. 반환값

반환값이 있는 명령어는 플레이어 코드에서 변수에 할당할 수 있습니다.

```csharp
// 명령어 구현
private static object Nearest(List<object> args, SpellContext ctx)
{
    // ...
    return nearestEntity; // Entity 반환
}
```

```c
// 플레이어 코드
let nearest = Nearest(self, 10);
Bolt(nearest, Fire, power);
```

반환값이 없으면 반드시 `null`을 반환합니다.

---

## 10. 주의사항

- **예외는 `RuntimeException`만 사용** — 플레이어에게 `[줄:열] 메시지` 형식으로 표시됩니다.
- **Unity API 호출 주의** — `Physics2D`, `Object.FindObjectOfType` 등은 메인 스레드에서만 동작합니다.
- **무한 루프 금지** — 명령어 내부에서 루프가 필요하다면 반드시 상한을 둡니다.
- **마나 비용** — 명령어 자체의 마나 비용은 현재 인터프리터가 관리하지 않습니다. 필요 시 `ctx`를 통해 `ManaTracker`에 접근해 직접 차감합니다.
