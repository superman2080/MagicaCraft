# Research — DSL 코드 저장/불러오기

## 목적

인게임 DSL 에디터(`SpellEditorUI`)에서 작성한 코드를 파일로 저장하고, 저장된 코드를 다시 불러오는 기능 추가.

---

## 관련 파일 분석

### `SpellEditorUI.cs` (UI/Layers/)

현재 구조:
- `TMP_InputField codeInput` — 코드 입력 필드 (`[SerializeField] private`)
- Inspector 버튼: `runButton`, `clearButton`, `closeButton` — 모두 `[SerializeField] private Button`
- ButtonBar는 `SpellEditorUIBuilder`에서 `HorizontalLayoutGroup`으로 생성
- 저장/불러오기 관련 코드 **전혀 없음**

저장 대상: `codeInput.text` (단순 문자열) + 사용자 입력 이름 + 저장 시각

### `SpellEditorUIBuilder.cs` (Editor/)

- `ButtonBar` (HorizontalLayoutGroup)에 Run/Clear 버튼 + Spacer + HintText 배치
- `so.FindProperty(...)` 패턴으로 `SpellEditorUI` SerializedField에 참조 연결
- **Save/Load 버튼 현재 없음** → Builder에도 추가 필요

---

## 저장 방식

### 옵션 비교

| 방식 | 장점 | 단점 |
|---|---|---|
| `PlayerPrefs` | 간단 | 코드 길이 제한, 목록 관리 불편 |
| `persistentDataPath` + JSON | 파일 단위 관리, 빌드 호환 | 파일 I/O 필요 |
| `ScriptableObject` | Inspector 연동 | 런타임 저장 불가 |

**결론: `Application.persistentDataPath/Spells/` 에 JSON 파일로 저장**

---

## 데이터 구조

```csharp
[Serializable]
private class SpellSaveData   // SpellEditorUI 내 중첩 클래스
{
    public string name;       // 표시용 이름
    public string code;       // DSL 코드 원문
    public string savedAt;    // 저장 시각 (yyyy-MM-dd HH:mm)
}
```

파일 경로: `{persistentDataPath}/Spells/{sanitized_name}.json`
파일명 정제: `/ \ : * ? " < > |` 제거

---

## UI 설계

### 저장 흐름
1. Save 버튼 클릭 → 이름 입력 팝업 표시 (런타임 생성)
2. Confirm → JSON 저장 (동일 이름 있으면 덮어쓰기)
3. 로그에 저장 성공/실패 메시지 출력

### 불러오기 흐름
1. Load 버튼 클릭 → 저장 목록 팝업 표시
2. 항목 클릭 → `codeInput.text` 교체, 팝업 닫기
3. 저장 파일이 없으면 로그에 안내 메시지

### 팝업 UI

두 팝업 모두 자동완성 팝업과 동일 패턴(런타임 생성, Canvas 자식):

**저장 이름 입력 팝업 (`savePopup`):**
```
[TMP_InputField: 이름 입력] [저장] [취소]
```
- 너비 350, 키보드 Submit(Enter) → 저장 트리거

**불러오기 목록 팝업 (`loadPopup`):**
```
■ MySpell1     2026-05-30
■ FireBolt     2026-05-29
...
[취소]
```
- ScrollRect 내 VerticalLayoutGroup, 최대 10개 표시

---

## 영향 범위

| 파일 | 변경 내용 |
|---|---|
| `SpellEditorUI.cs` | saveButton/loadButton 필드 추가, 팝업 빌더·저장·불러오기 로직 |
| `SpellEditorUIBuilder.cs` | ButtonBar에 Save/Load 버튼 추가, SerializedObject 연결 |

**신규 파일 없음** — `SpellSaveData`는 `SpellEditorUI` 내 중첩 클래스

---

## 주의사항

- `Application.persistentDataPath`는 에디터에서도 정상 동작 (빌드 없이 테스트 가능)
- 파일명에 사용 불가 문자 정제 필수
- 불러오기 팝업은 `BuildLoadPopup()`으로 `Show()` 시 목록 갱신 (매번 파일 스캔)
- `SpellEditorUIBuilder` 재실행 시 기존 씬 오브젝트를 `DestroyImmediate`하고 새로 생성하므로 변경사항이 반영됨
