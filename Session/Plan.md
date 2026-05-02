# Plan — 마법 시스템 노드 기반 재설계

## 개요

언리얼 엔진의 **C++ + Blueprint** 구조를 참고해 마법 시스템을 재설계한다.

| 언리얼 | MagicaCraft |
|---|---|
| C++ 코드 | C# 네이티브 (SpellNode, SpellEventDispatcher 등) |
| Blueprint 그래프 | DSL 코드 (플레이어가 직접 작성) |
| Blueprint 이벤트 노드 | SpellNode의 OnEnter / OnExecute / OnExit 디스패처 |
| Blueprint 에셋 | SpellGraph / SpellGraphSO |

- **C# 측(개발자)**: `SpellNode` 계층, `SpellEventDispatcher`, 내장 Effect/Object/Flow 노드를 구현한다.
- **DSL 측(플레이어)**: 각 노드의 이벤트 디스패처에 DSL 코드를 등록해 스킬 동작을 직접 구현한다.
- 기존 `Lexer` / `Parser` / `Interpreter` / `ManaTracker` 는 그대로 유지한다.

### 실행 모델

```
[C# 네이티브]                          [DSL — 플레이어 작성]
SpellGraph.CastEntryNode.Execute()
  → OnEnterDispatcher.Fire()    ----→  let speed = 15;
  → OnExecuteDispatcher.Fire()  ----→  Fire(basicBolt, self.position, dir, speed)
  → OnExitDispatcher.Fire()     ----→  (다음 노드 연결 또는 추가 코드)

ProjectileNode.OnProjectileHitDispatcher.Fire()  ----→  Bolt(victim, Fire, 30)
```

---

## 파일 구조

```
Assets/02. Scripts/SpellLanguage/
├── (기존 유지) AST/, Core/, Interpreter/, Items/, Library/, Runtime/
│
└── Node/                               ← 신규
    ├── Core/
    │   ├── SpellNode.cs                C# 기반 — 모든 노드의 추상 기반
    │   ├── SpellEventDispatcher.cs     C# 기반 — DSL 슬롯 + 연결 노드
    │   ├── SpellNodeContext.cs         C# 기반 — 런타임 컨텍스트 + Blackboard
    │   └── SpellGraph.cs              C# 기반 — 노드 그래프 컨테이너
    ├── Trigger/
    │   ├── CastTriggerNode.cs          시전 진입점
    │   └── HitTriggerNode.cs           명중 진입점
    ├── Effect/
    │   ├── BoltEffectNode.cs           LockedCode: Bolt(...)
    │   ├── HealEffectNode.cs           LockedCode: Heal(...)
    │   ├── ExplodeEffectNode.cs        LockedCode: Explode(...)
    │   └── ShieldEffectNode.cs         LockedCode: Shield(...)
    ├── Object/
    │   └── ProjectileNode.cs           투사체 스폰 + Hit/Expired 디스패처
    ├── Flow/
    │   ├── SequenceFlowNode.cs
    │   ├── DelayFlowNode.cs
    │   └── BranchFlowNode.cs
    ├── Skill/
    │   ├── SpellGraphSkill.cs          SpellSkill 대체
    │   └── SpellGraphRunner.cs         MonoBehaviour 드라이버
    └── Serialization/
        ├── SpellNodeData.cs
        ├── SpellParamData.cs
        └── SpellGraphSO.cs             ScriptableObject — Blueprint 에셋
```

---

## 단계별 구현 계획

### ✅ [완료] 단계 1 — SpellNodeContext

**파일**: `Node/Core/SpellNodeContext.cs` (신규)  
**영향**: 기존 `SpellContext` 대체

기존 `SpellContext` 기능(명령어/SpellObject 레지스트리)을 유지하면서 노드 간 데이터 전달을 위한 **Blackboard**를 추가한다.

- 프로퍼티: `Caster`, `Target`, `Mana(ManaTracker)`, `Runner(MonoBehaviour)`
- 내부에 `SpellContext`를 포함하거나 직접 대체 — DSL 실행에 필요한 명령어 레지스트리 접근 제공
- Blackboard: `Set(key, value)` / `Get(key)` / `TryGet<T>(key, out T)` — `Dictionary<string, object>`
- `Fork()`: Blackboard 얕은 복사로 독립 컨텍스트 반환 (투사체 히트 등 분기 실행에 사용)
- 표준 키 상수: `KeyCaster`, `KeyTarget`, `KeyVictim`, `KeyHitPosition`

---

### ✅ [완료] 단계 2 — SpellEventDispatcher

**파일**: `Node/Core/SpellEventDispatcher.cs` (신규)
이 시스템의 핵심. UE의 이벤트 디스패처처럼 동작하되, **DSL 코드(Blueprint)** 와 **연결 노드** 를 함께 보유한다.

- `CodeSlot`: `SkillCodeSlot` — 이 이벤트 발화 시 실행할 DSL 코드 (LockedCode + Parameters + UserCode)
- `ConnectedNodes`: `List<SpellNode>` — DSL 실행 후 순서대로 Execute할 다음 노드들
- `Connect(SpellNode)` / `Disconnect(SpellNode)` / `HasConnections`
- `Fire(SpellNodeContext ctx)` 실행 순서:
  1. `CodeSlot.BuildCode()`로 DSL 코드 조합
  2. `Interpreter`로 DSL 실행 (마나 소비 포함, 코드가 비어 있으면 스킵)
  3. `ConnectedNodes`를 순서대로 `yield return node.Execute(ctx)`
- `FireParallel(ctx)`: ConnectedNodes를 동시 StartCoroutine, 전부 완료 대기
- **주의**: `FireParallel`에서 `ref int` 카운터를 코루틴에 넘기면 컴파일 오류 — `int[]` 래핑으로 우회

---

### ✅ [완료] 단계 3 — SpellNode 추상 기반

**파일**: `Node/Core/SpellNode.cs` (신규)

UE의 C++ Actor/Component에 해당. 모든 노드의 C# 기반.

- `NodeId(string)`: `Guid.NewGuid().ToString()` 자동 생성 — Blueprint SO 직렬화 키
- `EditorPosition(Vector2)`: 추후 Blueprint 시각 편집기에서 노드 위치 저장용
- 디스패처 3종: `OnEnterDispatcher`, `OnExecuteDispatcher`, `OnExitDispatcher`
- `abstract IEnumerator Execute(SpellNodeContext ctx)` — 서브클래스가 구현. 내부에서 디스패처를 적절히 발화
- `Then(SpellNode next)`: `OnExitDispatcher.Connect(next)` 편의 체이닝 메서드

---

### ✅ [완료] 단계 4 — SpellGraph

**파일**: `Node/Core/SpellGraph.cs` (신규)

UE의 Blueprint 그래프에 해당하는 노드 컨테이너.

- `Name(string)`
- 진입 노드 3종: `CastEntryNode`, `UpdateEntryNode`, `EndEntryNode` (null = 미사용)
- `Nodes(IReadOnlyList<SpellNode>)`: 전체 노드 목록 (Blueprint SO 직렬화·순회용)
- `AddNode(SpellNode)` / `RemoveNode(SpellNode)`

---

### ✅ [완료] 단계 5 — Trigger 노드 2종

**파일**: `Node/Trigger/CastTriggerNode.cs`, `HitTriggerNode.cs` (신규)

UE의 Event Begin Play / Event On Hit에 해당하는 그래프 진입점.
자체 C# 로직 없이 `OnEnterDispatcher → OnExitDispatcher` 를 순서대로 발화한다.

- `CastTriggerNode`: `SpellGraph.CastEntryNode`에 할당. 플레이어가 스킬을 시전할 때 실행 시작
- `HitTriggerNode`: `ProjectileNode.OnProjectileHitDispatcher`에 연결. `Blackboard[KeyVictim]`을 다음 체인으로 전달

---

### ✅ [완료] 단계 6 — Effect 노드 4종

**파일**: `Node/Effect/` 아래 4개 파일 (신규)

UE에서 C++로 구현된 기능 함수 노드에 해당.
`OnExecuteDispatcher.CodeSlot.LockedCode`에 핵심 DSL 명령어가 미리 설정되며,
플레이어는 `Parameters`와 `UserCode`만 편집해 동작을 조정한다.

| 노드 | LockedCode (플레이어 변경 불가) | 플레이어 편집 가능 파라미터 |
|---|---|---|
| `BoltEffectNode` | `Bolt(target, element, damage)` | element, damage |
| `HealEffectNode` | `Heal(target, amount)` | amount |
| `ExplodeEffectNode` | `Explode(center, radius, element, damage)` | radius, element, damage |
| `ShieldEffectNode` | `Shield(target, amount)` | amount |

- `target` 파라미터 기본값: `"target"` (ctx.Target). Blackboard에 `victim`이 있으면 `"victim"`으로 교체
- 마나 부족 시 `OutOfManaException` — Runner에서 catch 후 `Skill.Cancel()` 처리
- **주의**: `ExplodeEffectNode`의 `center` 기본값은 `"target.position"`. 투사체 명중 시 `Blackboard[KeyHitPosition]`으로 자동 교체

---

### ✅ [완료] 단계 7 — ProjectileNode

**파일**: `Node/Object/ProjectileNode.cs` (신규)

투사체를 스폰하고 **즉시** `OnExitDispatcher`를 발화 (fire-and-forget).
투사체의 명중·만료 시점은 추가 디스패처가 비동기로 처리한다.

- 파라미터: `Element`, `Damage`, `Speed`, `UseTargetDirection(bool)`, `FixedDirection`
- 추가 디스패처 (UE의 OnComponentHit / OnLifeSpanExpired에 해당):
  - `OnProjectileHitDispatcher`: 투사체가 대상에 명중했을 때. `Blackboard[KeyVictim]` + `Blackboard[KeyHitPosition]` 자동 주입
  - `OnProjectileExpiredDispatcher`: 투사체 수명 만료 시
- Execute() 흐름:
  1. `SpellProjectilePool`에서 투사체 획득
  2. `proj.OnHit += victim => Runner.StartCoroutine(OnProjectileHitDispatcher.Fire(ctx.Fork() with victim))`
  3. `proj.OnExpire += () => Runner.StartCoroutine(OnProjectileExpiredDispatcher.Fire(ctx.Fork()))`
  4. 투사체 발사 → `OnExitDispatcher.Fire(ctx)` 즉시 실행 (투사체가 날아가는 동안 다음 노드 실행 가능)
- **주의**: 람다가 `ctx`를 캡처하므로 반드시 `ctx.Fork()` 사용. 원본 ctx 오염 방지

---

### ✅ [완료] 단계 8 — Flow 노드 3종

**파일**: `Node/Flow/` 아래 3개 파일 (신규)

UE의 Sequence / Delay / Branch 노드에 해당.

| 노드 | 동작 | 비고 |
|---|---|---|
| `SequenceFlowNode` | `children` 목록을 순서대로 실행 후 `OnExitDispatcher` 발화 | DSL 실행 후 순차 체인 |
| `DelayFlowNode` | N초 대기 후 `OnExitDispatcher` 발화 | `DelaySeconds` 프로퍼티 |
| `BranchFlowNode` | Condition 평가 → `OnTrueDispatcher` 또는 `OnFalseDispatcher` 발화 | 두 분기 모두 DSL 슬롯 보유 |

- `DelayFlowNode` 대기: `for (float t = 0; t < DelaySeconds; t += Time.deltaTime) yield return null;`
- `BranchFlowNode.Condition`: `Func<SpellNodeContext, bool>` — null이면 항상 `OnFalseDispatcher`

---

### ✅ [완료] 단계 9 — SpellProjectile 수정

**파일**: `Projectile/SpellProjectile.cs` (수정)

`ProjectileNode`가 구독할 C# 이벤트 두 개를 추가한다.

- `public event Action<Entity> OnHit` — `OnTriggerEnter2D`에서 `OnHit?.Invoke(victim)`
- `public event Action OnExpire` — 수명 만료·마나 소진 반환 시 `OnExpire?.Invoke()`
- Pool 반환 직전 두 이벤트를 `null`로 초기화 (구독 누수 방지)
- **주의**: 기존 `ManaTracker.OnDepleted` 구독 해제와 `OnExpire` 발화 순서 확인

---

### ✅ [완료] 단계 10 — SpellGraphSkill + SpellGraphRunner

**파일**: `Node/Skill/SpellGraphSkill.cs`, `Node/Skill/SpellGraphRunner.cs` (신규)

#### SpellGraphSkill (SpellSkill 대체)

기존 `SkillCodeSlot` 3개를 `SpellGraph` 1개로 교체. 플레이어의 스킬 = SpellGraph.

- 프로퍼티: `Name`, `Description`, `Type(SkillType)`, `ManaCost`, `Graph(SpellGraph)`, `IsCancelled`
- `RunOnStart(ctx)`: ManaCost 선불 소모 → `OnDepleted += Cancel` 구독 → `Graph.CastEntryNode.Execute(ctx)` StartCoroutine
- `RunOnUpdate(ctx)`: `IsCancelled` 체크 → `Graph.UpdateEntryNode?.Execute(ctx)`
- `RunOnEnd(ctx)`: `IsCancelled` 체크 → `Graph.EndEntryNode?.Execute(ctx)` → 구독 해제
- `Cancel()`: `IsCancelled = true`, 구독 해제

#### SpellGraphRunner (MonoBehaviour)

- `SpellGraphSkill Skill` 보유
- `TriggerCast(Entity caster, Entity target, ManaTracker mana)`: `SpellNodeContext` 생성 → `Skill.RunOnStart(ctx)` StartCoroutine
- `TriggerUpdate(ctx)`, `TriggerEnd(ctx)` 동일 패턴
- 코루틴 예외 처리: `OutOfManaException` → `Skill.Cancel()` / `RuntimeException` → `Debug.LogWarning`

---

### ✅ [완료] 단계 11 — Serialization 레이어 (Blueprint 에셋)

**파일**: `Node/Serialization/` 아래 3개 파일 (신규)

UE의 Blueprint 에셋(`.uasset`)에 해당. 데이터 전용 직렬화 레이어.

#### SpellParamData.cs
- `name`, `type(string)`, `value(string)` — 지원 타입: `float` / `int` / `bool` / `string` / `ElementType` / `Vector2`

#### SpellNodeData.cs
- `nodeId`, `nodeType` — 풀 타입명 (리플렉션으로 인스턴스 생성)
- `editorPosition(Vector2)`
- `parameters(List<SpellParamData>)` — 각 노드의 편집 가능 파라미터
- 디스패처별 연결: `onEnterConnections`, `onExecuteConnections`, `onExitConnections` — `List<string>` (nodeId)
- 디스패처별 DSL: `onEnterLockedCode`, `onEnterUserCode`, `onExecuteLockedCode`, `onExecuteUserCode`, `onExitLockedCode`, `onExitUserCode`
- ProjectileNode 전용: `onProjectileHitConnections`, `onProjectileExpiredConnections` 및 각 DSL 필드

#### SpellGraphSO.cs
- `[CreateAssetMenu(menuName = "MagicaCraft/SpellGraph")]`
- `graphName`, `castEntryNodeId`, `nodes(List<SpellNodeData>)`
- `Build()`: `nodeType` → 리플렉션 인스턴스 → 파라미터·DSL 바인딩 → 디스패처 Connect → `SpellGraph` 반환
- `Build()` 실패 시 예외 throw (잘못된 데이터를 런타임에 숨기지 않는다)

---

## 완료 기준

- 플레이어가 DSL 코드를 `OnProjectileHitDispatcher.CodeSlot.UserCode`에 등록하면 투사체 명중 시 실행됨
- `CastTriggerNode → ProjectileNode → OnProjectileHitDispatcher(DSL: Bolt(victim, Fire, 30))` 체인이 동작함
- `BranchFlowNode`의 두 분기에 각기 다른 DSL 코드가 실행됨
- `SpellGraphSO.Build()`로 런타임 그래프 정상 복원됨
- 기존 `Lexer` / `Parser` / `Interpreter` / `ManaTracker` 코드 무수정
- 컴파일 에러 없음
