# Research — SpellEditorUI 자동완성

## 목표
코드 입력창에서 DSL 식별자를 타이핑할 때 후보 목록을 팝업으로 표시하고, 선택 시 단어를 치환한다.

---

## 자동완성 후보 전체 목록

### 전역 후보 (식별자 앞 부분 일치)

| 카테고리 | 항목 |
|---|---|
| 키워드 | `let`, `if`, `else`, `loop`, `while`, `def`, `return`, `true`, `false` |
| SpellAction 명령어 | `Bolt`, `Heal`, `Fire`, `SetVelocity`, `SetSpeed`, `Explode`, `Barrior`, `Print` |
| 내장 함수 | `Vec2`, `Distance`, `Abs`, `Min`, `Max`, `Floor`, `Ceil`, `Sqrt`, `Normalize`, `GetMousePosition` |
| 내장 변수 | `self`, `target`, `power` |
| SpellObject 변수 | `basicBolt` (+ 추가 등록분) |
| 속성 상수 | `None`, `Fire`, `Water`, `Ice`, `Lightning`, `Earth`, `Wind`, `Light`, `Dark` |

### 멤버 후보 (`.` 뒤)

| 앞 식별자 | 멤버 목록 |
|---|---|
| `self`, `target` (Entity) | `hp`, `maxHp`, `hpRatio`, `moveSpeed`, `position` |
| `proj` (SpellProjectileData) | `position`, `velocity`, `speed`, `damage`, `element` |
| Vec2 변수 (`.x` 컨텍스트) | `x`, `y`, `magnitude` |

---

## 트리거 조건

- **전역**: 커서 직전에 식별자 문자(Letter/Digit/`_`)가 1자 이상 → prefix 일치 후보 존재 시 표시
- **멤버**: 커서 직전 단어 앞에 `.`가 붙어 있으면 → 앞 식별자 종류에 따라 멤버 후보
- **숨김**: prefix가 비거나 후보 0개 → 팝업 숨김; Escape 키; 입력 포커스 상실

---

## 입력 처리

| 키 | 동작 |
|---|---|
| Tab | 첫 번째(강조된) 후보 수락 |
| Click | 클릭한 항목 수락 |
| Escape | 팝업 닫기 |
| Up/Down | 비충돌 → 구현 생략 (Tab·클릭으로 충분) |

수락 시 커서 직전 단어 전체를 선택한 후보로 치환하고 팝업을 닫는다.

---

## 기술 제약

- `TMP_InputField.richText = false` → 입력창 내부 서식 불가
- Tab 키는 `TMP_InputField`가 소비하지 않으므로 `Update()`에서 `Keyboard.current.tabKey.wasPressedThisFrame`으로 감지 가능
- Escape는 `TMP_InputField`가 deactivate를 트리거하므로 `onSelect` / `onDeselect`로 별도 처리 필요
- 팝업 위치: TMP `textInfo.characterInfo`로 커서 위치를 얻어 팝업을 커서 바로 아래에 배치

---

## 신규 클래스

### `AutocompleteProvider` (pure C#, non-MonoBehaviour)
- 전역 후보 리스트 + 멤버 후보 딕셔너리 보관
- `GetGlobalCompletions(string prefix)` → 최대 8개 반환 (prefix 앞글자 일치)
- `GetMemberCompletions(string identifier)` → 멤버 후보

### `AutocompleteEntry` (record)
```csharp
public record AutocompleteEntry(string Label, string Detail);
// Label: 삽입 텍스트, Detail: 괄호형 시그니처 등 설명
```

---

## UI 구조 (런타임 동적 생성)

팝업은 `SpellEditorUI.Awake()`에서 코드로 생성 — Builder 재실행 불필요.

```
AutocompletePopup (RectTransform, 팝업 루트)
├── Background (Image, #21252B)
└── ItemContainer (VerticalLayoutGroup)
    ├── Item0 (Button + TextMeshProUGUI) ← 강조 배경
    ├── Item1
    ...
    └── Item7 (최대 8개 고정 슬롯)
```

팝업 위치: `SpellEditorUI`의 Canvas 기준으로 TMP `characterInfo`에서 얻은 커서 localPosition 아래에 배치.

---

## 영향 범위

| 파일 | 변경 유형 |
|---|---|
| `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs` | 자동완성 로직 통합, Update() 탭키 처리 |
| `Assets/02. Scripts/UI/Autocomplete/AutocompleteProvider.cs` | 신규 (pure C#) |
| Builder / Prefab | **변경 없음** — 팝업 UI 전부 런타임 생성 |
