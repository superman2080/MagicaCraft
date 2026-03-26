# SpellLanguage 개발 가이드

마법 언어 시스템에 새로운 기능을 추가할 때 참고하는 실전 가이드입니다.

---

## 목차

1. [새 SpellAction 추가하기](#1-새-spellaction-추가하기)
2. [새 SpellObject 타입 추가하기](#2-새-spellobject-타입-추가하기)
3. [SpellLibrary로 해금 관리하기](#3-spelllibrary로-해금-관리하기)
4. [DSL 내장 함수 추가하기](#4-dsl-내장-함수-추가하기)
5. [DSL 접근 가능 멤버 추가하기](#5-dsl-접근-가능-멤버-추가하기)
6. [DSL 코드 작성 예시 모음](#6-dsl-코드-작성-예시-모음)
7. [자주 하는 실수와 주의사항](#7-자주-하는-실수와-주의사항)

---

## 1. 새 SpellAction 추가하기

`SpellAction`은 DSL에서 함수처럼 호출하는 단발성 효과입니다. (`Bolt`, `Heal` 등)

### 단계

**① `Actions/` 폴더에 새 파일 생성**

```csharp
using System.Collections.Generic;

// Poison(target, damage, duration)
// 대상에게 독 상태이상을 건다.
// 마나 비용: damage * duration * ManaCostRate
public class PoisonAction : SpellAction
{
    public const float ManaCostRate = 0.3f;

    public override string DSLName => "Poison";

    protected override float GetManaCost(List<object> args, int line, int col)
    {
        if (args.Count != 3)
            throw new RuntimeException("Poison(target, damage, duration): 인자 3개 필요", line, col);
        float damage   = (float)RequireDouble(args[1], "Poison");
        float duration = (float)RequireDouble(args[2], "Poison");
        return damage * duration * ManaCostRate;
    }

    protected override object Execute(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Poison(target, damage, duration)";
        var   target   = RequireEntity(args[0], sig);
        float damage   = (float)RequireDouble(args[1], sig);
        float duration = (float)RequireDouble(args[2], sig);

        // TODO: 상태이상 시스템 연동
        target.TakeDamage(ctx.Caster as IAttackable, damage, ElementType.None);
        return null;
    }
}
```

**② `SpellLibrary.CreateDefault()`에 등록**

```csharp
lib.Register(new PoisonAction { Tier = 1 });  // 티어에 맞게 설정
```

**③ DSL에서 사용**

```
Poison(target, 5, 3)
```

### 규칙

- `GetManaCost`에서 인자 수 검증 + 마나 계산만 수행 (실제 효과 없음)
- `Execute`에서 실제 게임 로직 수행 (마나는 이미 차감됨)
- 헬퍼 메서드: `RequireEntity`, `RequireElement`, `RequireDouble`, `RequireVector2`, `RequireSpellObject`
- `DSLName`은 PascalCase

---

## 2. 새 SpellObject 타입 추가하기

`SpellObject`는 월드에 생성되어 라이프사이클(Start/Update/Finish)을 가지는 객체입니다.

### 단계

**① `Objects/` 폴더에 데이터 클래스 생성**

```csharp
// 장판(AoE) 타입 SpellObject.
// AoeSpellBehaviour(MonoBehaviour)가 instanceVars를 주입한다:
//   "aoe" — SpellAoeData (위치, 반경, 생존시간 등)
public class AoeObject : SpellObject
{
    public float DefaultRadius   { get; set; } = 2f;
    public float DefaultDuration { get; set; } = 3f;
    public float DefaultDamage   { get; set; } = 10f;
}
```

**② 대응 MonoBehaviour 생성**

```csharp
public class AoeSpellBehaviour : MonoBehaviour
{
    private SpellContext _ctx;
    private ManaTracker  _mana;
    private AoeObject    _aoeObj;
    private SpellAoeData _data;

    public void Init(SpellContext ctx, ManaTracker mana, AoeObject aoeObj)
    {
        _ctx    = ctx;
        _mana   = mana;
        _aoeObj = aoeObj;
        _data   = new SpellAoeData(this);

        // RunStart: 생성 시 실행
        _aoeObj.RunStart(ctx, mana,
            new Dictionary<string, object> { ["aoe"] = _data });
    }

    private void Update()
    {
        // RunUpdate: 매 프레임
        _aoeObj.RunUpdate(_ctx, _mana,
            new Dictionary<string, object> { ["aoe"] = _data });
    }

    private void OnDestroy()
    {
        // RunFinish: 소멸 시
        _aoeObj.RunFinish(_ctx, _mana,
            new Dictionary<string, object> { ["aoe"] = _data });
    }
}
```

**③ Interpreter에서 새 타입 멤버 접근 추가 (필요 시)**

`Interpreter.EvalMember`에 케이스 추가:

```csharp
SpellAoeData aoe => EvalAoeMember(aoe, e.Member, e.Line, e.Column),
```

```csharp
private object EvalAoeMember(SpellAoeData aoe, string member, int line, int col)
{
    return member switch
    {
        "position" => (object)aoe.Position,
        "radius"   => (double)aoe.Radius,
        "duration" => (double)aoe.Duration,
        _ => throw new RuntimeException($"AoE에 '{member}' 필드가 없습니다.", line, col)
    };
}
```

**④ `ToStringVal`에 새 타입 케이스 추가**

`Interpreter.ToStringVal`:

```csharp
SpellAoeData a => $"[AoE @ {a.Position}]",
```

**⑤ SpellLibrary에 등록**

```csharp
lib.Register(new AoeObject
{
    Name            = "poisonCloud",
    Description     = "독 장판",
    Tier            = 1,
    DefaultRadius   = 2f,
    DefaultDuration = 3f,
    DefaultDamage   = 5f,
    OnUpdateCode    = "Explode(aoe.position, aoe.radius, None, 5)",
});
```

**⑥ DSL에서 사용**

새 명령어(`PlaceAoe`)도 만들어야 Fire처럼 월드에 배치할 수 있습니다 (FireAction 참고).

---

## 3. SpellLibrary로 해금 관리하기

### 기본 사용

```csharp
// 게임 시작 시 (GameManager 등)
var lib = SpellLibrary.CreateDefault();  // 티어 0 자동 해금
GameManager.Instance.SpellLib = lib;

// 티어 1 해금 (특정 조건 달성 시)
GameManager.Instance.SpellLib.UnlockTier(1);

// 개별 아이템 해금 (드롭 아이템 획득 시)
GameManager.Instance.SpellLib.Unlock("poisonCloud");

// 마법 실행 전 컨텍스트에 반영
var ctx = new SpellContext(caster, target);
GameManager.Instance.SpellLib.ApplyToContext(ctx);
```

### 해금 상태 확인

```csharp
if (lib.IsUnlocked("Explode"))
    // UI에서 Explode 사용 가능 표시
```

### 커스텀 아이템 등록

SpellLibrary.CreateDefault()에 없는 아이템은 직접 Register 후 Unlock:

```csharp
var customObj = new ProjectileObject
{
    Name           = "iceShard",
    Description    = "얼음 파편 투사체",
    Tier           = 1,
    DefaultDamage  = 15f,
    DefaultSpeed   = 14f,
    DefaultElement = ElementType.Ice,
    OnFinishCode   = "Bolt(victim, Ice, proj.damage)",
};
lib.Register(customObj);
lib.Unlock("iceShard");
```

---

## 4. DSL 내장 함수 추가하기

내장 함수는 `Interpreter.RegisterBuiltins()`에서 `SpellBuiltin`으로 등록합니다.

```csharp
// Clamp(value, min, max)
_env.Define("Clamp", new SpellBuiltin((args, line, col) =>
{
    if (args.Count != 3)
        throw new RuntimeException("Clamp(value, min, max): 인자 3개 필요", line, col);
    double val = ToDouble(args[0], line, col);
    double min = ToDouble(args[1], line, col);
    double max = ToDouble(args[2], line, col);
    return Math.Clamp(val, min, max);
}));
```

규칙:
- 이름은 PascalCase
- 내장 함수는 마나를 소비하지 않음
- `def`로 재정의 불가 (Interpreter가 자동 차단)

---

## 5. DSL 접근 가능 멤버 추가하기

`Interpreter.EvalEntityMember` 또는 관련 Eval 메서드에 새 케이스를 추가합니다.

예: Entity에 `mana` 멤버 추가

```csharp
// EvalEntityMember 내부
"mana"     => (double)entity.CurrentMana,
"maxMana"  => (double)entity.MaxMana,
"manaRatio"=> (double)(entity.CurrentMana / entity.MaxMana),
```

---

## 6. DSL 코드 작성 예시 모음

### 기본 투사체 발사

```
Fire(basicBolt, self.position, target.position - self.position, 10)
```

### 투사체에 피격 시 폭발 효과 (ProjectileObject.OnFinishCode)

```
Bolt(victim, Fire, proj.damage)
Explode(victim.position, 2, Fire, proj.damage * 0.5)
```

### 유도 투사체 (ProjectileObject.OnUpdateCode)

```
let dir = target.position - proj.position
SetVelocity(proj, Normalize(dir) * proj.speed)
```

### HP 비례 데미지

```
let ratio = 1 - self.hpRatio
Bolt(target, Dark, power * (1 + ratio))
```

### 조건부 치료

```
if (self.hpRatio < 0.3) {
    Heal(self, 50)
}
```

### 다중 타격 (loop)

```
loop (3) {
    Bolt(target, Lightning, 15)
}
```

### 사용자 정의 함수 (def — 티어 2)

```
def multiHit(t, n, dmg) {
    loop (n) {
        Bolt(t, None, dmg)
    }
}
multiHit(target, 5, 10)
```

### 투사체 속도 가속 (OnUpdateCode)

```
SetSpeed(proj, proj.speed * 1.01)
```

---

## 7. 자주 하는 실수와 주의사항

### SpellObject를 직접 호출하면 오류

```
// 잘못된 사용
basicBolt(self.position, ...)   // RuntimeException!

// 올바른 사용
Fire(basicBolt, self.position, Vec2(1, 0), 10)
```

### def로 명령어/내장 함수 재정의 불가

```
def Bolt(t, e, d) { ... }   // RuntimeException!
def Vec2(x, y)   { ... }    // RuntimeException!
```

### SpellObject는 상태를 저장하지 않음

같은 `ProjectileObject` 인스턴스를 여러 투사체가 공유합니다. 투사체별 상태(위치, 속도 등)는 `instanceVars`로 전달된 `SpellProjectileData`를 통해서만 접근하세요.

```
// OnUpdateCode에서 proj로 투사체 데이터 접근
let spd = proj.speed
SetVelocity(proj, Vec2(spd, 0))
```

### GetManaCost는 순수 계산만

`GetManaCost`는 마나 비용을 계산하기 위해 먼저 호출됩니다. 이 안에서 게임 상태를 변경하면 안 됩니다. 실제 효과는 반드시 `Execute`에서만 수행하세요.

### 새 SpellAction 추가 후 SpellLibrary 등록 필수

클래스만 만들어도 자동으로 등록되지 않습니다. `SpellLibrary.CreateDefault()` 또는 런타임 `lib.Register()`를 반드시 호출해야 DSL에서 사용 가능합니다.

### Interpreter의 ToStringVal 업데이트

새 런타임 타입(ex: `SpellAoeData`)을 추가했다면 `Interpreter.ToStringVal`에 해당 케이스를 추가해야 `Print(aoe)` 같은 디버그 출력이 올바르게 동작합니다.

---

## 빠른 참조

| 작업 | 파일 위치 |
|---|---|
| 새 단발성 명령어 | `SpellLanguage/Items/Actions/` |
| 새 라이프사이클 객체 타입 | `SpellLanguage/Items/Objects/` |
| 해금/잠금 관리 | `SpellLanguage/Library/SpellLibrary.cs` |
| 내장 수학 함수 추가 | `Interpreter.RegisterBuiltins()` |
| 멤버 접근 추가 | `Interpreter.EvalMember()` |
| 마법 디버그 테스트 | `Debug/SpellDebugUI.cs` (인게임 콘솔) |
