# Plan — DSL 코드 저장/불러오기

## 개요

`SpellEditorUI`에 Save/Load 버튼을 추가하고, `Application.persistentDataPath/Spells/`에 JSON 파일로 DSL 코드를 저장·불러오는 기능 구현.

---

## 단계 1 — `SpellEditorUI.cs`: Save/Load 로직 및 팝업 UI 추가 ⬜

**영향 파일:** `Assets/02. Scripts/UI/Layers/SpellEditorUI.cs`

1. Inspector 필드 추가:
   ```csharp
   [SerializeField] private Button saveButton;
   [SerializeField] private Button loadButton;
   ```

2. `[Serializable]` 중첩 클래스 추가:
   ```csharp
   [Serializable]
   private class SpellSaveData
   {
       public string name;
       public string code;
       public string savedAt;
   }
   ```

3. 저장 팝업 내부 상태 필드 추가:
   ```csharp
   private GameObject     savePopup;
   private TMP_InputField saveNameInput;
   private GameObject     loadPopup;
   ```

4. `Awake()`에 `BuildSavePopup()`, `BuildLoadPopup()` 호출 추가

5. `Start()`에 버튼 리스너 등록:
   ```csharp
   saveButton?.onClick.AddListener(OpenSavePopup);
   loadButton?.onClick.AddListener(OpenLoadPopup);
   ```

6. `BuildSavePopup()` 구현 — Canvas 자식으로 런타임 생성:
   - 배경 패널 (HorizontalLayoutGroup)
   - `TMP_InputField` (이름 입력) + `저장` 버튼 + `취소` 버튼
   - `저장` 버튼과 `saveNameInput.onSubmit` → `SaveCode()` 호출

7. `BuildLoadPopup()` 구현 — Canvas 자식으로 런타임 생성:
   - 배경 패널 + ScrollRect
   - 최대 10개 슬롯 (Button + 이름 Label + 날짜 Detail + Image)
   - `취소` 버튼

8. 저장/불러오기 메서드 구현:
   - `SaveCode()` — `Application.persistentDataPath/Spells/` 디렉토리 생성 후 JSON 쓰기
   - `OpenLoadPopup()` — `LoadSpellFiles()` 호출 후 목록 슬롯 갱신
   - `LoadSpellFiles()` — 디렉토리 내 `*.json` 파일 스캔, `SpellSaveData` 목록 반환
   - `ApplyLoad(SpellSaveData data)` — `codeInput.text` 교체

9. 파일명 정제 헬퍼:
   ```csharp
   private static string SanitizeFileName(string name)
       => System.Text.RegularExpressions.Regex.Replace(name, @"[/\\:*?""<>|]", "").Trim();
   ```

---

## 단계 2 — `SpellEditorUIBuilder.cs`: ButtonBar에 Save/Load 버튼 추가 ⬜

**영향 파일:** `Assets/02. Scripts/Editor/SpellEditorUIBuilder.cs`

1. ButtonBar에 Save/Load 버튼 추가 (Spacer 뒤, HintText 앞):
   ```
   [▶ Run] [Clear]  ---spacer---  [💾 Save] [📂 Load]  Ctrl+Enter
   ```
   - Save: 너비 70, 배경색 `#61AEEF`
   - Load: 너비 70, 배경색 `#56B6C2`

2. `SerializedObject`로 `saveButton`, `loadButton` 참조 연결:
   ```csharp
   so.FindProperty("saveButton").objectReferenceValue = saveBtn.GetComponent<Button>();
   so.FindProperty("loadButton").objectReferenceValue = loadBtn.GetComponent<Button>();
   ```

---

## 주의사항

- 저장 팝업·불러오기 팝업 모두 자동완성 팝업과 동일 패턴 (런타임 생성, `Awake()` 호출)
- 저장 파일이 없을 때 Load 누르면 로그에 "저장된 파일이 없습니다" 메시지 출력
- 동일 이름 덮어쓰기 시 별도 확인 없이 즉시 덮어씀
- 불러오기 목록은 팝업 열 때마다 파일 스캔 (변경사항 반영)
- **Builder 재실행 필수** — 씬 오브젝트 재생성 후 Save/Load 버튼이 Inspector에 연결됨
