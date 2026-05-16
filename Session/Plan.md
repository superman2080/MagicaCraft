# Plan — SpellEditorUI 자동완성 (확장성 설계)

## 목표

DSL 코드 입력 중 식별자 prefix 일치 후보를 팝업으로 표시하고 Tab/클릭으로 자동 완성한다.
**신규 DSL 항목 추가 시 자동완성 코드 수정 없이 자동 반영**되는 구조를 만든다.

---

## 확장성 설계 원칙

| DSL 추가 작업 | 자동완성 코드 수정 |
|---|---|
| 새 SpellAction 추가 (+ SpellLibrary 등록) | **없음** — SpellLibrary에서 자동 수집 |
| 새 SpellObject 추가 (+ SpellLibrary 등록) | **없음** — SpellLibrary에서 자동 수집 |
| 새 내장 함수 추가 | Interpreter.BuiltinFunctions 배열에 1줄 추가 |
| 새 키워드 추가 | AutocompleteProvider.Keywords 배열에 1줄 추가 |
| 새 멤버 추가 | AutocompleteProvider.MemberCompletions 딕셔너리에 1줄 추가 |

---

## 검증 목표 (MCP로 확인)

| # | 검증 항목 | MCP 확인 방법 |
|---|---|---|
| V1 | 전체 컴파일 성공 | `read_console()` → error 없음 |
| V2 | 런타임에 AutocompletePopup GO 생성됨 | Play Mode 후 `find_gameobjects("AutocompletePopup")` |
| V3 | 런타임 에러 없음 | Play Mode 후 `read_console()` |

---

## 단계 1 — SpellAction 기반 클래스에 Signature 추가 ✅ [완료]

**영향 파일:** `Assets/02. Scripts/SpellLanguage/Items/SpellAction.cs`

변경 내용:
- `public virtual string Signature => DSLName;` 추가

서브클래스들 각각 override:

| 클래스 | Signature 값 |
|---|---|
| BoltAction | `"Bolt(target, element, damage)"` |
| HealAction | `"Heal(target, amount)"` |
| FireAction | `"Fire(projObj, startPos, dir, speed)"` |
| SetVelocityAction | `"SetVelocity(proj, vel)"` |
| SetSpeedAction | `"SetSpeed(proj, speed)"` |
| ExplodeAction | `"Explode(center, radius, element, damage)"` |
| BarriorAction | `"Barrior(target, amount)"` |

**MCP 검증:** `read_console()` → 컴파일 에러 없음

---

## 단계 2 — SpellLibrary에 GetAll() 추가 ✅ [완료]

**영향 파일:** `Assets/02. Scripts/SpellLanguage/Library/SpellLibrary.cs`

변경 내용:
```csharp
// 등록된 모든 아이템을 반환 (해금 여부 무관 — 에디터 힌트용)
public IEnumerable<SpellItem> GetAll() => registry.Values;
```

**목적:** AutocompleteProvider가 SpellLibrary에서 SpellAction/SpellObject를 동적으로 수집

---

## 단계 3 — Interpreter에 BuiltinFunctions 정적 배열 추가 ✅ [완료]

**영향 파일:** `Assets/02. Scripts/SpellLanguage/Interpreter/Interpreter.cs`

변경 내용: 클래스 상단에 정적 배열 추가
```csharp
// 내장 함수 목록 — 자동완성에서 읽는다.
// 새 내장 함수 추가 시 RegisterBuiltins()와 동시에 여기도 추가한다.
public static readonly (string Name, string Signature)[] BuiltinFunctions =
{
    ("Vec2",             "Vec2(x, y)"),
    ("Distance",         "Distance(a, b)"),
    ("Abs",              "Abs(x)"),
    ("Min",              "Min(a, b)"),
    ("Max",              "Max(a, b)"),
    ("Floor",            "Floor(x)"),
    ("Ceil",             "Ceil(x)"),
    ("Sqrt",             "Sqrt(x)"),
    ("Normalize",        "Normalize(vec)"),
    ("GetMousePosition", "GetMousePosition()"),
};
```

**목적:** 내장 함수 추가 시 이 배열에만 1줄 추가하면 자동완성에 자동 반영

---

## 단계 4 — AutocompleteProvider.cs 신규 생성 ✅ [완료]

**경로:** `Assets/02. Scripts/UI/Autocomplete/AutocompleteProvider.cs`

설계:
- 생성자에서 `SpellLibrary` 수신 → 전체 후보 빌드 (1회)
- 이후 조회는 O(1) 필터링만

```csharp
public record AutocompleteEntry(string Label, string Detail);

public class AutocompleteProvider
{
    // ─── 확장 시 이 두 곳만 수정 ─────────────────────────────

    // 키워드 목록 — 새 키워드 추가 시 여기에만 추가
    private static readonly string[] Keywords =
    {
        "let", "if", "else", "loop", "while", "def", "return", "true", "false"
    };

    // 멤버 후보 — 새 멤버 추가 시 해당 키에 항목 추가
    private static readonly Dictionary<string, string[]> MemberCompletions = new()
    {
        { "self",   new[] { "hp", "maxHp", "hpRatio", "moveSpeed", "position" } },
        { "target", new[] { "hp", "maxHp", "hpRatio", "moveSpeed", "position" } },
        { "proj",   new[] { "position", "velocity", "speed", "damage", "element" } },
    };

    // ─────────────────────────────────────────────────────────

    private readonly List<AutocompleteEntry> globalList = new();

    // SpellLibrary 전달 → SpellAction/SpellObject 자동 수집
    public AutocompleteProvider(SpellLibrary library) { Build(library); }

    private void Build(SpellLibrary library)
    {
        // 키워드
        foreach (var kw in Keywords)
            globalList.Add(new AutocompleteEntry(kw, "keyword"));

        // SpellAction / SpellObject — SpellLibrary에서 자동 수집
        foreach (var item in library.GetAll())
        {
            if (item is SpellAction action)
                globalList.Add(new AutocompleteEntry(action.DSLName, action.Signature));
            else if (item is SpellObject obj)
                globalList.Add(new AutocompleteEntry(obj.Name, "SpellObject"));
        }

        // 내장 함수 — Interpreter.BuiltinFunctions에서 자동 수집
        foreach (var (name, sig) in Interpreter.BuiltinFunctions)
            globalList.Add(new AutocompleteEntry(name, sig));

        // 내장 변수 / 속성 상수
        foreach (var v in new[] { "self", "target", "power" })
            globalList.Add(new AutocompleteEntry(v, "variable"));
        foreach (var el in new[] { "None","Fire","Water","Ice","Lightning","Earth","Wind","Light","Dark" })
            globalList.Add(new AutocompleteEntry(el, "element"));
    }

    public List<AutocompleteEntry> GetGlobalCompletions(string prefix)
        → prefix로 시작하는 항목 최대 8개 반환

    public List<AutocompleteEntry> GetMemberCompletions(string identifier)
        → MemberCompletions[identifier] 기반 반환
}
```

**MCP 검증:** `read_console()` → 컴파일 에러 없음

---

## 단계 5 — SpellEditorUI.cs: 팝업 UI 생성 + 자동완성 로직 ✅ [완료]

**영향 파일:** `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs`

### 5-1. 필드 추가
```csharp
private AutocompleteProvider  acProvider;
private GameObject            acPopup;
private RectTransform         acPopupRT;
private readonly List<(Button btn, TextMeshProUGUI label, TextMeshProUGUI detail)> acSlots = new();
private int                   acSelectedIndex;
private List<AutocompleteEntry> acCurrentList = new();
private const int             AcMaxSlots = 8;
```

### 5-2. Awake() 변경
```csharp
var lib = SpellLibrary.CreateDefault();
lib.UnlockTier(1);
acProvider = new AutocompleteProvider(lib);  // SpellLibrary에서 자동 수집
BuildAutocompletePopup();
```

### 5-3. BuildAutocompletePopup()
Canvas 하위에 팝업 GO + 슬롯 8개를 코드로 생성, 각 슬롯 클릭 → `AcceptAutocomplete(i)`.

### 5-4. onValueChanged에 UpdateAutocomplete 추가
```csharp
codeInput.onValueChanged.AddListener(text => {
    if (!isAutoClosing) { UpdateErrorHighlight(text); UpdateAutocomplete(); }
});
```

### 5-5. 추가 메서드
| 메서드 | 역할 |
|---|---|
| `UpdateAutocomplete()` | 현재 단어 추출 → provider 조회 → 팝업 표시/숨김 |
| `GetWordBeforeCursor()` | 커서 직전 식별자 반환 |
| `GetIdentifierBeforeDot()` | `.` 직전 식별자 반환 |
| `ShowAutocomplete(list)` | 슬롯 텍스트 세팅, 첫 항목 강조 |
| `HideAutocomplete()` | 팝업 숨김 |
| `AcceptAutocomplete(i)` | 현재 단어를 선택 항목으로 치환 |
| `PositionPopup()` | TMP characterInfo로 커서 아래 위치 계산 |

### 5-6. Update() 추가
팝업 활성 시에만:
- Tab → `AcceptAutocomplete(acSelectedIndex)`
- Escape → `HideAutocomplete()`

### 5-7. Hide() 변경
`HideAutocomplete()` 추가 호출 (에디터 닫을 때 팝업도 닫기)

---

## 단계 6 — MCP로 컴파일 및 런타임 검증 ✅ [완료]

```
1. read_console()                           → V1 컴파일 에러 없음
2. manage_editor(enter_play_mode)
3. find_gameobjects("AutocompletePopup")    → V2 GO 존재
4. read_console()                           → V3 런타임 에러 없음
5. manage_editor(exit_play_mode)
```

---

## 주의사항

- `AutocompleteProvider` 생성은 `Awake()`에서 수행 (SpellLibrary.CreateDefault() 재활용)
- Tab 키는 TMP_InputField가 소비하지 않으므로 `Update()`에서 안전하게 감지
- `isAutoClosing = true`인 동안 UpdateAutocomplete 호출 생략
- 팝업이 Canvas 경계 아래로 넘어가면 커서 위쪽으로 반전 표시
- **RunCode()의 `SpellLibrary.CreateDefault()`와 Awake의 것은 독립 인스턴스** — 자동완성용은 Awake에서 한 번만 생성
