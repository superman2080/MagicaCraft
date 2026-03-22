# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고하는 가이드입니다.

## 프로젝트 개요

**MagicaCraft**는 **Unity 6000.0.25f1(Unity 6)** 로 개발 중인 2D 로그라이크 게임입니다.
플레이어가 인게임 에디터에서 마법을 직접 코딩하여 설계하는 것이 핵심 메카닉입니다.

주요 패키지: URP 17.0.3, Input System 1.11.2, 2D Feature Set, Unity Test Framework 1.4.5

모든 C# 스크립트는 `Assets/02. Scripts/`에 위치하며, 씬 파일은 `Assets/01. Scenes/SampleScene.unity`입니다.

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
Fire(fireBolt, self.position, direction, speed)

// 3. 플레이어 추가 코드 (자유 작성)
Bolt(target, Fire, 10)
```

- `SkillParameter`: `Name`, `DefaultValue`, `Value` (DSL 표현식)
- `SkillCodeSlot.BuildCode()`: 세 부분을 조합해 최종 DSL 코드 문자열 반환

#### 마나 소진 시 자동 중단

`RunOnStart` 시 `ManaTracker.OnDepleted` 구독 → 마나 소진 시 `Cancel()` 호출.
`IsCancelled == true`이면 `RunOnUpdate`/`RunOnEnd` 즉시 반환.

#### C# 사용 예시

```csharp
var skill = new SpellSkill
{
    Name = "FireShot",
    Type = SkillType.OneShot,
    OnStart = new SkillCodeSlot
    {
        LockedCode = "Fire(spellObj, self.position, direction, speed)",
        Parameters = new List<SkillParameter>
        {
            new SkillParameter("spellObj",  "fireBolt"),
            new SkillParameter("direction", "target.position - self.position"),
            new SkillParameter("speed",     "10"),
        },
        UserCode = "" // 플레이어 작성
    }
};
// 버튼 입력 시:
skill.RunOnStart(ctx, mana);
```

---

### 6. SpellEffectItem (SpellObject)

월드에 생성되는 라이프사이클 오브젝트(투사체, 장판, 소환물 등)의 동작 정의.

#### SpellEffectType

```csharp
public enum SpellEffectType
{
    SpellObject, // 월드에 생성되는 라이프사이클 오브젝트
}
```

#### 슬롯별 주입 변수

| 슬롯 | 주입 변수 |
|---|---|
| OnStart / OnUpdate | `self`, `proj` |
| OnEnd | `self`, `proj`, `victim` |

#### DSL 발동 명령어

| 명령어 | 시그니처 | 설명 |
|---|---|---|
| `Fire` | `Fire(spellObj, startPos, direction, speed)` | SpellObject 발사/생성 |

#### C# 사용 예시

```csharp
var fireBoltItem = new SpellEffectItem
{
    Name       = "FireBolt",
    EffectType = SpellEffectType.SpellObject,
    ManaCost   = 10f,
    OnEndCode  = "Bolt(victim, Fire, power * 2)"
};
fireBoltItem.Compile();
ctx.RegisterItem("fireBolt", fireBoltItem);
```

---

### 6. 마법 언어 시스템 (`SpellLanguage/`)

인게임 DSL 파이프라인:

```
소스 문자열
  → Lexer        (문자열 → Token[])
  → Parser       (Token[] → AST)
  → Interpreter  (AST 실행 + 마나 차감)
      ├── SpellEnvironment  변수 스코프 (블록별 Push/Pop)
      ├── ManaTracker       실시간 마나 차감, 부족 시 OutOfManaException
      └── SpellContext      Entity 참조 + 명령어 레지스트리 + 아이템 레지스트리
```

#### DSL 네이밍 컨벤션

| 종류 | 규칙 | 예 |
|---|---|---|
| 마법 명령어 | PascalCase | `Bolt`, `Heal`, `Fire`, `Cast`, `ApplyEffect` |
| 내장 수학 함수 | PascalCase | `Vec2`, `Abs`, `Distance`, `Min`, `Max`, `Sqrt` |
| 속성 상수 | PascalCase | `Fire`, `Water`, `Lightning` |
| 아이템 변수 | camelCase | `fireBolt`, `poisonCloud` |
| 멤버 접근 | camelCase | `target.hp`, `target.maxHp`, `pos.x` |
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
| 등록된 SpellObject 변수 | `SpellEffectItem` |
| `def`로 정의한 함수 | `SpellFunction` |

#### 마나 비용 상수 (`ManaTracker`)

`let`/`=` 1마나, `if` 2마나, `loop` 진입 2마나, `while` 반복당 1마나, 이항/단항 연산·멤버접근 1마나.
아이템 발동 비용은 `SpellEffectItem.ManaCost`에서 정의. 내부 명령어(`Bolt` 등) 비용은 각 구현에서 별도 정의.

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

pos.x / pos.y      Vec2 좌표
pos.magnitude      Vec2 크기
```

#### 티어별 해금 구조

| 티어 | 시작 여부 | 주요 명령어 |
|---|---|---|
| 0 | 처음부터 제공 | `Bolt`, `Heal`, `Fire`, `if`, `loop`, 기본 연산 |
| 1 | 해금 필요 | `Explode`, `Shield`, `while` |
| 2 | 해금 필요 | `def` (함수 정의), 추가 명령어 |

#### 안전장치

- 루프 최대 반복: 1000회 (`Interpreter.MaxIterations`)
- 마나 부족 시 `OutOfManaException` (UI에서 `RuntimeException`과 구분 처리)
- 미등록 명령어 호출 시 `RuntimeException`

#### 명령어 등록 예시

```csharp
var ctx = new SpellContext(playerEntity, targetEntity);
SpellCommands.RegisterTier0(ctx);               // 게임 시작 시
SpellCommands.RegisterTier1(ctx);               // 티어 1 해금 시
ctx.RegisterItem("fireBolt", fireBoltItem);     // 아이템 획득 시
var interp = new Interpreter(ctx, new ManaTracker(manaResource));
interp.Run(parser.Parse());
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
├── Utils/                ClampedFloat
└── SpellLanguage/
    ├── AST/              ASTNode (Expr), ASTStmt (Stmt)
    ├── Core/             Token, Lexer, Parser
    ├── Interpreter/      Interpreter, SpellEnvironment, ManaTracker (OnDepleted 이벤트)
    ├── Runtime/          SpellContext, SpellEffectItem (SpellObject), SpellProjectileData
    │                     SpellSkill (OneShot/Continuous), SkillCodeSlot, SkillParameter
    └── Commands/         SpellCommands (Bolt, Heal, Fire, Explode, Shield)
```
