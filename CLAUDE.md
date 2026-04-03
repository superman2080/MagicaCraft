# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고하는 가이드입니다.

## 프로젝트 개요

**MagicaCraft**는 **Unity 6000.0.25f1(Unity 6)** 로 개발 중인 2D 로그라이크 게임입니다.
플레이어가 인게임 에디터에서 마법을 직접 코딩하여 설계하는 것이 핵심 메카닉입니다.

주요 패키지: URP 17.0.3, Input System 1.11.2, 2D Feature Set, Unity Test Framework 1.4.5

모든 C# 스크립트는 `Assets/02. Scripts/`에 위치하며, 씬 파일은 `Assets/01. Scenes/SampleScene.unity`입니다.

## ⚠️ Claude Code 작업 파이프라인 (최우선 규칙)

**모든 설계·구현 작업은 반드시 아래 단계를 순서대로 따른다. 예외 없음.**

### 1단계 — Research.md 생성

설계 요청이 들어오면 **가장 먼저** 관련 파일들을 분석하여 `Session/Research.md`를 생성한다.

- 설계와 연관된 모든 파일을 읽고 현재 구조·인터페이스·의존성을 정리
- 변경이 파급되는 범위(영향 분석) 포함
- 코드 예시, 기존 패턴 등을 문서화
- Research 없이 Plan을 작성하거나 코드를 수정하지 않는다

### 2단계 — Plan.md 작성 및 피드백 루프

`Session/Research.md`를 바탕으로 `Session/Plan.md`를 작성한다.

- 구현 단계를 번호 목록으로 명확히 분리
- 각 단계에 영향 파일, 변경 내용, 주의사항 명시
- Plan 작성 후 사용자에게 검토 요청

**피드백 방식**: 사용자가 Plan.md 안에 `>>>` 로 시작하는 줄로 코멘트를 남긴다.

```
## 단계 2 — 스탯 연산 수정
...
>>> 이 부분은 addValue가 아니라 multiplier를 바꿔야 함
```

Claude는 `>>>` 코멘트를 반영하여 Plan.md를 수정하고 다시 검토를 요청한다.
이 루프는 사용자가 "구현해줘" 또는 명시적 승인을 할 때까지 반복한다.

### 3단계 — 구현

사용자가 Plan 승인 후 구현을 요청하면:

1. Plan.md의 단계를 순서대로 **전부** 구현한다 — 중간에 멈추지 않는다
2. 각 단계 완료 시 Plan.md에 완료 표시를 업데이트한다:
   - ✅ `[완료]` — 구현됨
   - ⬜ `[대기]` — 아직 구현 안 됨
   - ❌ `[실패]` — 오류 발생, 사유 기록
3. 모든 단계가 `[완료]` 또는 `[실패]`가 될 때까지 멈추지 않는다

### 4단계 — MCP 안전 규칙

MCP(Unity Editor 연동)를 통해 작업할 때:

- 스크립트 생성·수정 후에는 **반드시** `read_console`로 컴파일 에러 확인
- 컴파일 에러가 있으면 즉시 수정하고 다시 확인 — 에러를 쌓아두지 않는다
- 새 컴포넌트·타입은 컴파일 성공 후에만 씬에서 사용한다
- 변경 전 `editor_state` 리소스로 현재 상태를 확인한다

---

## 개발 워크플로우

빌드·실행·테스트 모두 **Unity Editor**에서 진행합니다. CLI 빌드 파이프라인 없음.

- **테스트:** Window > General > Test Runner
- **빌드:** File > Build Settings
- **IDE:** Rider 및 Visual Studio 연동 (`.sln` / `.csproj` 자동 생성)

---

## 핵심 아키텍처

### 1. 엔티티 시스템 (`Entity/`)

모든 게임 오브젝트는 추상 클래스 `Entity`(MonoBehaviour)를 상속합니다. `Rigidbody2D` 필수.

```
Entity (abstract, IDamagable)
├── DefaultStat        — HP, HP재생, 이동속도
├── StateMachine<Entity>
├── ElementType[]      — 속성 (이중 속성 지원)
└── GetDamageMultiplier(ElementType) → float
```

`TakeDamage(IAttackable caster, float amount, ElementType element = None)`
→ `ElementChart`로 배율 계산 후 HP 차감. `element` 생략 시 1.0배.

인터페이스: `IDamagable` (피해/회복), `IAttackable` (공격 수행, AttackStat·AttackLayer 포함).

### 2. 속성 시스템 (`Entity/Element/`)

| 파일 | 역할 |
|---|---|
| `ElementType.cs` | 9가지 속성 열거형 (None, Fire, Water, Ice, Lightning, Earth, Wind, Light, Dark) |
| `ElementChart.cs` | `GetMultiplier(attacker, defender)` → 2.0 / 1.0 / 0.5 / 0.0 배율 반환 |

이중 속성은 각 배율을 곱함 (포켓몬 방식). Inspector에서 `affinities` 배열로 설정.

### 3. 계층형 상태 머신 (`StateMachine/`)

제네릭 `StateMachine<T>` — 일반 FSM + `CompositeStateBase`를 통한 HFSM 지원.

- 상태 생명주기: `Enter()` → `Execute()` / `FixedExecute()` → `Exit()`
- 전이는 predicate 조건으로 작동. AnyState 전이가 일반 전이보다 우선
- 상태는 `Dictionary<Enum, StateBase<T>>`에 저장

새 상태 추가: `StateBase` 상속 → `RegisterState()` → `RegisterCondition()`.

#### HardCC 우선순위 시스템

`HardCCState`(`CompositeStateBase` 상속)가 모든 제어 불가능 상태이상을 통합 관리한다.

- `StatusEffect.Priority` (virtual, 기본 0) — 높을수록 우선 적용
- 동시에 여러 HardCC가 걸리면 Priority 최고값의 서브 상태가 활성화된다
- 메인 FSM은 `HardCCStates.HardCC`(복합 상태)만 알고, 내부 전환은 `HardCCState`가 담당

`HardCCStates` 열거형:

| 값 | 용도 |
|---|---|
| `HardCC` | 메인 FSM 등록 키 (CompositeStateBase) |
| `Fear` | SubStateMachine 서브 상태 키, Priority 100 |
| `Stun` | SubStateMachine 서브 상태 키, Priority 0 (추후 구현) |

새 HardCC 추가 방법 (`Entity.cs`):
1. `HardCCStates`에 열거값 추가
2. `HardCCState.effectToState` 딕셔너리에 `{ typeof(MyCC), HardCCStates.MyCC }` 추가
3. `HardCCState` 생성자에서 `SubStateMachine.RegisterState(HardCCStates.MyCC, new MyCCState(entity))` 추가

**스탯 배율 주의사항**: `Stat<T>.InitStat()`이 LateUpdate마다 `multipleValue`를 1로 초기화한다.
상태이상에 의한 배율 수정은 `Enter`가 아닌 `Execute`에서 매 프레임 재적용해야 한다.

### 4. 스탯 시스템 (`Entity/Stat/`)

`Stat<T>` 계산식: `(baseValue + addValue) * multiplier`

- `DefaultStat` — `MAX_HP`, `HP_REGEN`, `MOVE_SPEED` + HP `Resource` (틱 재생)
- `AttackStat` — `ATTACK_DISTANCE`, `ATTACK_SPEED`, `DAMAGE`
- `Resource` — `ClampedFloat` 래퍼, `OnValueChanged` / `OnDepleted` / `OnFilled` 이벤트

### 5. 마법 스킬 시스템 (`SpellSkill`)

플레이어가 버튼에 배치하여 발동하는 최상위 스킬 개념.

#### SkillType

| 타입 | 트리거 | 실행 슬롯 |
|---|---|---|
| `OneShot` | 버튼 1회 입력 | OnStart만 (1회) |
| `Continuous` | 버튼 다운 / 홀드 / 업 | OnStart / OnUpdate(매프레임) / OnEnd |

#### 슬롯 구조 (`SkillCodeSlot`)

각 슬롯은 세 부분을 순서대로 실행한다.

```
// 1. 매개변수 주입 (자동 생성)
let speed = 10;
let direction = target.position - self.position;

// 2. 고정 코드 (수정 불가 — 스킬의 핵심 동작)
Fire(basicBolt, self.position, direction, speed)

// 3. 플레이어 추가 코드 (자유 작성)
Bolt(target, Fire, 10)
```

- `SkillParameter`: `Name`, `DefaultValue`, `Value` (DSL 표현식)
- `SkillCodeSlot.BuildCode()`: 세 부분을 조합해 최종 DSL 코드 문자열 반환

#### 마나 소진 시 자동 중단

`RunOnStart` 시 `ManaTracker.OnDepleted` 구독 → 마나 소진 시 `Cancel()` 호출.
`IsCancelled == true`이면 `RunOnUpdate`/`RunOnEnd` 즉시 반환.

---

### 6. 마법 아이템 시스템 (`SpellLanguage/Items/`)

마법 아이템은 두 종류로 분리된다.

| 종류 | 기반 클래스 | 설명 | 플레이어 역할 |
|---|---|---|---|
| **SpellObject** | `SpellObject : SpellItem` | 라이프사이클 객체 (투사체 등) | Start/Update/Finish 코드 구현 |
| **SpellAction** | `SpellAction : SpellItem` | 단발성 효과 (Bolt, Heal 등) | 함수처럼 호출만 가능 |

#### SpellItem 공통 속성

```csharp
public abstract class SpellItem
{
    public string Name        { get; set; }
    public string Description { get; set; }
    public int    Tier        { get; set; }  // 0=기본, 1+=해금 필요
}
```

#### SpellObject

라이프사이클 객체의 동작을 정의한다. **인스턴스별 상태를 저장하지 않음** — 여러 인스턴스가 동일 객체를 안전하게 공유 가능.

```csharp
// 플레이어 작성 코드 슬롯
OnStartCode  / OnUpdateCode / OnFinishCode

// 실행 메서드 (MonoBehaviour에서 호출)
RunStart (ctx, mana, instanceVars)   // instanceVars: proj 등 인스턴스별 데이터
RunUpdate(ctx, mana, instanceVars)
RunFinish(ctx, mana, instanceVars)

// 서브클래스 확장 지점
GetStartVars() / GetUpdateVars() / GetFinishVars()  // 타입별 추가 변수
BeforeStart() / AfterStart() / …                    // Unity API 호출용 훅
```

구체 타입:

| 클래스 | 설명 | instanceVars |
|---|---|---|
| `ProjectileObject` | 투사체 | `proj` (SpellProjectileData), OnFinish에 `victim` (Entity) 추가 |
| `ControllableObject` | DSL로 이동 제어 | `vel` (Vector2), `moveSpeed` (double) |

#### SpellAction

단발성 효과. `Invoke` → `GetManaCost` → `Spend` → `Execute` 순으로 실행. 마나 차감 흐름 보장.

```csharp
public abstract string DSLName { get; }
protected abstract float  GetManaCost(List<object> args, int line, int col);
protected abstract object Execute    (List<object> args, SpellContext ctx, int line, int col);
```

등록된 액션:

| 클래스 | DSL명 | 시그니처 | 마나 비용 | 티어 |
|---|---|---|---|---|
| `BoltAction` | `Bolt` | `Bolt(target, element, damage)` | damage × 1.0 | 0 |
| `HealAction` | `Heal` | `Heal(target, amount)` | amount × 0.8 | 0 |
| `FireAction` | `Fire` | `Fire(projObj, startPos, dir, speed)` | projObj.DefaultDamage × 0.5 | 0 |
| `SetVelocityAction` | `SetVelocity` | `SetVelocity(proj, vel)` | 0 | 0 |
| `SetSpeedAction` | `SetSpeed` | `SetSpeed(proj, speed)` | 0 | 0 |
| `ExplodeAction` | `Explode` | `Explode(center, radius, element, damage)` | damage × (1.0 + radius × 0.5) | 1 |
| `ShieldAction` | `Shield` | `Shield(target, amount)` | amount × 0.7 | 1 |

### 7. SpellLibrary (`SpellLanguage/Library/`)

마법 아이템의 잠금/해금 레지스트리.

```csharp
var lib = SpellLibrary.CreateDefault();   // 티어 0 자동 해금, basicBolt 포함
lib.UnlockTier(1);                        // 티어 1 해금
lib.Unlock("myCustomSpell");              // 개별 해금
lib.ApplyToContext(ctx);                  // SpellContext에 반영
```

`ApplyToContext` 동작:
- `SpellAction` → `ctx.RegisterCommand(DSLName, action.Invoke)`
- `SpellObject` → `ctx.RegisterSpellObject(name, obj)` (DSL 변수로 주입)

### 8. 마법 언어 시스템 (`SpellLanguage/`)

인게임 DSL 파이프라인:

```
소스 문자열
  → Lexer        (문자열 → Token[])
  → Parser       (Token[] → AST)
  → Interpreter  (AST 실행 + 마나 차감)
      ├── SpellEnvironment  변수 스코프 (블록별 Push/Pop)
      ├── ManaTracker       실시간 마나 차감, 부족 시 OutOfManaException
      └── SpellContext      Entity 참조 + 명령어 레지스트리 + SpellObject 레지스트리
```

#### DSL 네이밍 컨벤션

| 종류 | 규칙 | 예 |
|---|---|---|
| 마법 명령어 | PascalCase | `Bolt`, `Heal`, `Fire`, `Explode` |
| 내장 수학 함수 | PascalCase | `Vec2`, `Abs`, `Distance`, `Min`, `Max`, `Sqrt`, `Normalize` |
| 속성 상수 | PascalCase | `Fire`, `Water`, `Lightning` |
| SpellObject 변수 | camelCase | `basicBolt`, `poisonCloud` |
| 멤버 접근 | camelCase | `target.hp`, `proj.position`, `pos.x` |
| 키워드 | lowercase | `if`, `else`, `while`, `loop`, `let`, `def`, `return` |

#### DSL 런타임 타입

`object`로 전달되며, C# 패턴 매칭으로 구분.

| DSL 값 | C# 타입 |
|---|---|
| `30`, `1.5` | `double` |
| `true`, `false` | `bool` |
| `"text"` | `string` |
| `Vec2(x, y)` | `Vector2` |
| `self`, `target`, `victim` | `Entity` |
| `Fire`, `Water` … | `ElementType` |
| `proj` | `SpellProjectileData` |
| 등록된 SpellObject 변수 | `SpellObject` (직접 호출 불가, Fire()에 전달) |
| `def`로 정의한 함수 | `SpellFunction` |

#### 마나 비용

마나는 마법 명령어 실행 시에만 소모 (제어문·연산자는 무료).
각 `SpellAction` 서브클래스의 `ManaCostPerXxx` 상수에서 관리.

#### 엔티티 접근 가능 멤버

```
target.hp          현재 HP (double)
target.maxHp       최대 HP (double)
target.hpRatio     HP 비율 0~1 (double)
target.moveSpeed   이동속도 (double)
target.position    위치 (Vector2)

proj.position      투사체 위치 (Vector2)
proj.velocity      투사체 속도 벡터 (Vector2)
proj.speed         투사체 속도 크기 (double)
proj.damage        투사체 기본 데미지 (double)
proj.element       투사체 속성 (ElementType)

pos.x / pos.y      Vec2 좌표
pos.magnitude      Vec2 크기
```

#### 티어별 해금 구조

| 티어 | 시작 여부 | 주요 내용 |
|---|---|---|
| 0 | 처음부터 제공 | `Bolt`, `Heal`, `Fire`, `SetVelocity`, `SetSpeed`, `basicBolt` |
| 1 | 해금 필요 | `Explode`, `Shield` |
| 2 | 해금 필요 | `def` (함수 정의), 추가 명령어 (미구현) |

#### 안전장치

- 루프 최대 반복: 1000회 (`Interpreter.MaxIterations`)
- 마나 부족 시 `OutOfManaException` (UI에서 `RuntimeException`과 구분 처리)
- 미등록 명령어 호출 시 `RuntimeException`
- `def`로 명령어/내장 함수 재정의 시 `RuntimeException`
- `SpellObject` 변수를 직접 호출하면 `RuntimeException` (Fire()에 전달해야 함)

#### 컨텍스트 초기화 예시

```csharp
var ctx = new SpellContext(playerEntity, targetEntity);
var lib = SpellLibrary.CreateDefault();   // 티어 0 해금 + basicBolt 포함
lib.UnlockTier(1);                        // 필요 시 티어 1 해금
lib.ApplyToContext(ctx);                  // 명령어 + SpellObject 변수 등록

var mana   = new ManaTracker(manaResource);
var interp = new Interpreter(ctx, mana);
interp.Run(new Parser(new Lexer(code).Tokenize()).Parse());
```

---

## 디렉토리 구조 (주요 항목)

```
Assets/02. Scripts/
├── Entity/
│   ├── Element/          ElementType, ElementChart
│   ├── Stat/             Stat<T>, DefaultStat, AttackStat, Resource
│   ├── StatusEffect/     StatusEffect, Fear, Stun, ...
│   ├── Entity.cs         추상 기반 (IDamagable, StateMachine, 속성)
│   ├── IDamagable.cs
│   └── IAttackable.cs
├── StateMachine/         StateMachine<T>, StateBase, CompositeStateBase
├── Projectile/           SpellProjectile, SpellProjectilePool
├── Utils/                ClampedFloat
├── Debug/                SpellDebugUI
└── SpellLanguage/
    ├── AST/              ASTNode (Expr), ASTStmt (Stmt)
    ├── Core/             Token, Lexer, Parser
    ├── Interpreter/      Interpreter, SpellEnvironment, ManaTracker
    ├── Runtime/          SpellContext, SpellProjectileData,
    │                     SpellSkill (OneShot/Continuous), SkillCodeSlot, SkillParameter
    ├── Items/
    │   ├── SpellItem.cs          추상 기반
    │   ├── SpellObject.cs        라이프사이클 객체 기반
    │   ├── SpellAction.cs        단발성 효과 기반
    │   ├── Objects/
    │   │   ├── ProjectileObject.cs
    │   │   └── ControllableObject.cs
    │   └── Actions/
    │       ├── BoltAction.cs
    │       ├── HealAction.cs
    │       ├── FireAction.cs
    │       ├── ExplodeAction.cs
    │       ├── ShieldAction.cs
    │       ├── SetVelocityAction.cs
    │       └── SetSpeedAction.cs
    └── Library/
        └── SpellLibrary.cs       잠금/해금 레지스트리
```
