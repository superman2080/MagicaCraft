# Plan — DSL 실행 UI (SpellEditorUI)

## 개요

`SubUIManager`를 상속한 `SpellEditorUI`를 구현하고, Unity MCP로 씬에 Prefab을 구성한다.
플레이어가 DSL 코드를 입력·실행할 수 있으며, `UIManager`를 통해 외부에서 Show/Hide 가능하다.

---

## 단계별 구현 계획

### ✅ [완료] 단계 1 — SpellEditorUI.cs 스크립트 작성

**파일**: `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs` (신규)

`SubUIManager` 상속. Inspector에서 UI 요소를 연결받아 DSL 코드를 실행하는 클래스.

**멤버 (Inspector 연결)**
```
[SerializeField] private int sortingOrder = 200;
[SerializeField] private Entity caster;
[SerializeField] private Entity target;
[SerializeField] private float maxMana = 500f;

// UI 참조
[SerializeField] private TMP_InputField codeInput;
[SerializeField] private TextMeshProUGUI logText;
[SerializeField] private ScrollRect logScroll;
[SerializeField] private TextMeshProUGUI manaLabel;
[SerializeField] private TextMeshProUGUI casterLabel;
[SerializeField] private TextMeshProUGUI targetLabel;
[SerializeField] private Button runButton;
[SerializeField] private Button clearButton;
[SerializeField] private Button closeButton;
```

**생명주기**
- `Awake()`: `base.Awake()` 호출
- `Start()`: `base.Start()` 호출, 버튼 리스너 등록, 레이블 갱신

**핵심 메서드**
- `RunCode()`: Lexer → Parser → SpellContext + SpellLibrary → Interpreter 실행
  - `OutOfManaException`, `RuntimeException`, `Exception` 예외 처리
  - 실행 전 manaResource 초기화
- `ClearLog()`: logText 초기화
- `RefreshLog()`: logText 갱신 + ScrollRect 최하단 이동
- `UpdateLabels()`: CasterLabel, TargetLabel, ManaLabel 텍스트 갱신
- `RegisterDebugCommands(SpellContext ctx)`: `Print(value)` 명령어 등록

**코딩 컨벤션**
- `override int SortingOrder => sortingOrder;`
- Allman 스타일 중괄호
- private 필드 camelCase (언더스코어 없음)

---

### ✅ [완료] 단계 2 — Unity MCP로 씬에 GameObject 구성

MCP 도구 사용 순서:

1. `manage_gameobject` — 씬에 `SpellEditorCanvas` GameObject 생성
2. `manage_components` — Canvas, CanvasScaler, GraphicRaycaster 추가
3. 하위 Panel, Header, ContextBar, CodeInput, ButtonBar, LogScroll 구조 구성
   - Panel: Image 컴포넌트 (배경 색)
   - Header: TextMeshProUGUI ("Spell Editor")
   - ContextBar: HorizontalLayoutGroup + CasterLabel, TargetLabel, ManaLabel
   - CodeInput: TMP_InputField (MultiLineNewline 모드)
   - ButtonBar: HorizontalLayoutGroup + Run / Clear / Close 버튼
   - LogScroll: ScrollRect + Viewport + Content + LogText
4. `manage_components` — SpellEditorCanvas에 `SpellEditorUI` 컴포넌트 추가
5. `manage_components` — Inspector 직렬화 필드에 각 자식 참조 연결

**주의**: `read_console`로 컴파일 에러 확인 후 컴포넌트 추가 진행

---

### ✅ [완료] 단계 3 — 컴파일 확인 및 Inspector 연결 검증

1. `read_console` — 컴파일 에러 없음 확인
2. `manage_components` — SpellEditorUI의 SerializeField 필드에 UI 오브젝트 참조 연결
3. `manage_editor` — Play Mode 진입 후 `UIManager.Instance.Get<SpellEditorUI>()` 정상 반환 확인
4. Run 버튼 클릭 → DSL 실행 결과 로그 출력 확인

---

## 완료 기준

- `SpellEditorUI : SubUIManager` 컴파일 성공
- `UIManager.Instance.Get<SpellEditorUI>()` 가 null이 아닌 인스턴스 반환
- `Show()` / `Hide()` 로 패널 표시·숨김 동작
- DSL 코드 입력 후 Run 버튼 → 실행 결과 로그에 출력
- `OutOfManaException`, `RuntimeException` 각각 구분된 메시지로 로그 출력
- `Print(value)` 명령어 로그에 출력
