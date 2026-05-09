# Research — DSL 실행 UI (SubUIManager 기반)

## 목표

플레이어가 인게임에서 DSL 코드를 작성하고 실행할 수 있는 UI 개발.
기존 `SpellDebugUI`(MonoBehaviour 단독)를 참고하되, `SubUIManager`를 상속해 `UIManager`에 등록되는 정식 UI 레이어로 구현.

---

## 분석한 파일 목록

| 파일 | 역할 |
|---|---|
| `UI/SubUIManager.cs` | 추상 기반. `SortingOrder`, `Show()`, `Hide()`, `UIManager.Register` 자동 등록 |
| `UI/UIManager.cs` | Singleton 레이어 레지스트리. `Get<T>()` / `TryGet<T>()` |
| `UI/Layers/DialogUI.cs` | SubUIManager 구현 참고 사례. Inspector 직접 연결 패턴 |
| `Debug/SpellDebugUI.cs` | DSL 실행 로직 전체 구현. 절차적 UI 생성 방식 |
| `SpellLanguage/Runtime/SpellContext.cs` | 실행 컨텍스트. Caster 필수, Target 선택 |
| `SpellLanguage/Library/SpellLibrary.cs` | `CreateDefault()` → 티어 0 해금, `ApplyToContext(ctx)` |
| `SpellLanguage/Interpreter/ManaTracker.cs` | `Resource` 래핑, `OutOfManaException` |
| `SpellLanguage/Core/Lexer.cs`, `Parser.cs` | 토크나이징 → AST |
| `SpellLanguage/Interpreter/Interpreter.cs` | `Run(program)` |

---

## 현재 구조 파악

### SubUIManager 패턴

```csharp
public abstract class SubUIManager : MonoBehaviour
{
    public abstract int SortingOrder { get; }
    public Canvas Canvas { get; private set; }
    public bool IsVisible => gameObject.activeSelf;

    protected virtual void Awake() { Canvas = GetComponent<Canvas>(); Canvas.sortingOrder = SortingOrder; }
    protected virtual void Start()  { UIManager.Instance.Register(this); }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
```

### DialogUI 패턴 (SubUIManager 구현 선례)

- Prefab 기반: Inspector에서 자식 오브젝트 참조 (`[SerializeField]`)
- `Reset()` 으로 자동 참조 설정
- `override int SortingOrder => sortingOrder;` + `[SerializeField] private int sortingOrder;`
- `base.Awake()` 호출 필수

### SpellDebugUI DSL 실행 흐름 (재사용 가능한 부분)

```csharp
var tokens  = new Lexer(code).Tokenize();
var program = new Parser(tokens).Parse();
var ctx     = new SpellContext(caster, target);
var lib     = SpellLibrary.CreateDefault();
lib.UnlockTier(1);
lib.ApplyToContext(ctx);
var manaResource = new Resource(maxMana, 0f);
var manaTracker  = new ManaTracker(manaResource);
var interp       = new Interpreter(ctx, manaTracker);
interp.Run(program);
```

예외 처리: `OutOfManaException`, `RuntimeException`, `Exception`

---

## 영향 분석

- 신규 파일: `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs`
- 수정 없음: SubUIManager, UIManager, SpellDebugUI (별도 공존)
- 씬에 새 Canvas GameObject + `SpellEditorUI` 컴포넌트 추가 필요 (MCP로 Prefab 구성)

---

## 기존 패턴 vs 새 UI 차이점

| 항목 | SpellDebugUI | SpellEditorUI (신규) |
|---|---|---|
| 기반 클래스 | MonoBehaviour | SubUIManager |
| UIManager 등록 | 없음 | 자동 (Start에서) |
| UI 구성 방식 | 절차적 (코드에서 생성) | Prefab + Inspector 연결 |
| 토글 방식 | 백쿼트 키 | `Show()` / `Hide()` (외부에서 제어) |
| Canvas | 코드에서 생성 | Prefab의 Canvas 컴포넌트 |
| Entity 연결 | Inspector SerializeField | Inspector SerializeField |

---

## UI 구성 요소 (필요 목록)

```
Canvas (SpellEditorUI 컴포넌트, sortingOrder = 200)
└── Panel (배경 이미지)
    ├── Header (타이틀 TextMeshProUGUI)
    ├── ContextBar (HorizontalLayoutGroup)
    │   ├── CasterLabel (TextMeshProUGUI)
    │   ├── TargetLabel (TextMeshProUGUI)
    │   └── ManaLabel  (TextMeshProUGUI)
    ├── CodeInput (TMP_InputField — 다중 행)
    ├── ButtonBar (HorizontalLayoutGroup)
    │   ├── RunButton   (Button)
    │   ├── ClearButton (Button)
    │   └── CloseButton (Button)
    └── LogScroll (ScrollRect)
        └── Viewport → Content → LogText (TextMeshProUGUI)
```

---

## 주의사항

1. `base.Awake()` 호출 — Canvas 참조 및 sortingOrder 설정
2. `base.Start()` 호출 — UIManager 자동 등록
3. Prefab 방식이므로 UI 요소는 Inspector에서 연결 (`[SerializeField]`)
4. `Reset()` 메서드로 자식 참조 자동 설정 패턴 유지 (DialogUI 선례)
5. 마나는 실행마다 초기화 (SpellDebugUI 동일)
6. `Print(value)` 디버그 명령어 포함
7. `SubUIManager.Awake()` 가 `GetComponent<Canvas>()` 를 호출하므로,
   해당 GameObject에 Canvas 컴포넌트가 반드시 있어야 함
