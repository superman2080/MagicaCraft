# Plan — SpellEditorUI 코드 입력 UX 개선

## 목표

1. **Enter 줄바꿈**: Enter 키로 코드 입력창에서 다음 줄로 이동
2. **자동 괄호 닫기**: `(` → `()`, `{` → `{}` 입력 시 닫는 괄호 자동 삽입, 커서는 괄호 사이
3. **에디터 열림/닫힘 시 플레이어 조작 차단**: 에디터 UI가 열리면 `PlayerInputHandler` 비활성화, 닫히면 복원

---

## 영향 파일

| 파일 | 변경 내용 |
|---|---|
| `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs` | lineType 보장, 자동 괄호, Show/Hide 오버라이드 |
| `Assets/02. Scripts/InputSystem/PlayerInputHandler.cs` | `SetInputEnabled(bool)` 퍼블릭 메서드 추가 |

---

## 단계

### 단계 1 — Enter 키 줄바꿈 보장 [대기]

**원인:**
`lineType = MultiLineNewline`이 Builder/Prefab에 설정되어 있지만 런타임에 재적용하지 않아 무시될 수 있음.

**변경 내용 (`SpellEditorUI.Start()`):**
```csharp
if (codeInput != null)
    codeInput.lineType = TMP_InputField.LineType.MultiLineNewline;
```

`Update()`의 Ctrl+Enter 로직은 그대로 유지. 순수 Enter는 TMP 내부가 처리.

**영향 파일:** `SpellEditorUI.cs`

---

### 단계 2 — 자동 괄호 닫기 구현 [대기]

**방식:** `onValueChanged`에서 이전/현재 텍스트를 비교해 단일 문자 삽입을 감지하고, 닫는 괄호를 커서 뒤에 삽입.

**삽입 쌍:**

| 입력 문자 | 자동 삽입 |
|---|---|
| `(` | `)` |
| `{` | `}` |

**추가 필드:**
```csharp
private string prevInputText = "";
private bool   isAutoClosing = false;  // 재귀 호출 방지 가드
```

**이벤트 등록 (`Start()`):**
```csharp
codeInput.onValueChanged.AddListener(OnCodeInputChanged);
```

**`OnCodeInputChanged` 로직:**
1. `isAutoClosing == true`이면 즉시 리턴 (재귀 방지)
2. 현재 text 길이가 prevInputText보다 정확히 1 증가 → 단일 문자 삽입 감지
3. 삽입된 문자 = `codeInput.text[codeInput.stringPosition - 1]`
4. 해당 문자가 `(` 또는 `{`이면:
   - `isAutoClosing = true`
   - `codeInput.text = text.Insert(stringPosition, closingChar)`
   - `codeInput.stringPosition`을 닫는 괄호 앞으로 복원
   - `isAutoClosing = false`
5. `prevInputText = codeInput.text`로 갱신

**영향 파일:** `SpellEditorUI.cs`

---

### 단계 3 — 에디터 열림/닫힘 시 플레이어 조작 차단 [대기]

**방식:**

`PlayerInputHandler`에 `SetInputEnabled(bool)` 메서드를 추가해 `playerInput.Enable()/Disable()`을 외부에서 호출 가능하게 한다.

`SpellEditorUI`에서 `Show()`/`Hide()`를 오버라이드하여 에디터가 열릴 때 차단, 닫힐 때 복원한다.

**`PlayerInputHandler.cs` 변경:**
```csharp
public void SetInputEnabled(bool enabled)
{
    if (enabled) playerInput.Enable();
    else         playerInput.Disable();
}
```

**`SpellEditorUI.cs` 변경:**

필드 추가:
```csharp
private PlayerInputHandler playerInputHandler;
```

`Start()`에 참조 획득 추가:
```csharp
playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
```

`Show()` / `Hide()` 오버라이드 추가:
```csharp
public override void Show()   // SubUIManager.Show()는 virtual이 아니면 new 키워드 사용
{
    base.Show();
    playerInputHandler?.SetInputEnabled(false);
}

public override void Hide()
{
    base.Hide();
    playerInputHandler?.SetInputEnabled(true);
}
```

> **주의**: `SubUIManager.Show()`/`Hide()`가 `virtual`인지 확인 후 `override` 또는 `new` 키워드 선택.

**영향 파일:** `PlayerInputHandler.cs`, `SpellEditorUI.cs`

---

## 완료 기준

- [x] Enter 키 → 코드 입력창에서 줄바꿈
- [x] `(` 입력 → `()` 삽입, 커서가 두 괄호 사이
- [x] `{` 입력 → `{}` 삽입, 커서가 두 괄호 사이
- [x] Ctrl+Enter → 기존과 동일하게 코드 실행
- [x] 자동 삽입 중 무한 루프 없음 (isAutoClosing 가드)
- [x] SpellEditorUI 열림 → 플레이어 이동/점프/대시 입력 차단
- [x] SpellEditorUI 닫힘 → 플레이어 입력 복원
