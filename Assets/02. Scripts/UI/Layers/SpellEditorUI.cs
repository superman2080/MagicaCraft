using PlayerNamespace;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpellEditorUI : SubUIManager
{
    // ─── Inspector ────────────────────────────────────────────

    [SerializeField] private int sortingOrder = 200;

    [Header("Context")]
    [SerializeField] private Entity caster;
    [SerializeField] private Entity target;

    [Header("UI References")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TextMeshProUGUI errorOverlay;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect logScroll;
    [SerializeField] private TextMeshProUGUI manaLabel;
    [SerializeField] private TextMeshProUGUI casterLabel;
    [SerializeField] private TextMeshProUGUI targetLabel;
    [SerializeField] private Button runButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    // ─── SubUIManager ─────────────────────────────────────────

    public override int SortingOrder => sortingOrder;

    // ─── 내부 상태 ────────────────────────────────────────────

    private readonly StringBuilder log = new();
    private ManaStat manaStat;

    private PlayerInputHandler playerInputHandler;
    private string prevInputText       = "";
    private bool   isAutoClosing       = false;
    private bool   suppressNextNewline = false; // 자동완성 Enter 수락 후 TMP가 삽입하는 \n 제거용

    // ─── 자동완성 상태 ────────────────────────────────────────

    private AutocompleteProvider  acProvider;
    private GameObject            acPopup;
    private RectTransform         acPopupRT;
    private readonly List<(Button btn, TextMeshProUGUI label, TextMeshProUGUI detail, Image bg)> acSlots = new();
    private List<AutocompleteEntry> acCurrentList = new();
    private int                   acSelectedIndex;
    private int                   acWordStart;        // 팝업 표시 시점의 현재 단어 시작 위치
    private const int             AcMaxSlots = 8;

    // ─────────────────────────────────────────────────────────
    // Show / Hide
    // ─────────────────────────────────────────────────────────

    public override void Show()
    {
        base.Show();
        playerInputHandler?.SetInputEnabled(false);
    }

    public override void Hide()
    {
        HideAutocomplete();
        base.Hide();
        playerInputHandler?.SetInputEnabled(true);
    }

    // ─────────────────────────────────────────────────────────
    // Unity 생명주기
    // ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        // SpellLibrary에서 전체 후보 수집 (해금 여부 무관)
        var lib = SpellLibrary.CreateDefault();
        lib.UnlockTier(1);
        acProvider = new AutocompleteProvider(lib);
        BuildAutocompletePopup();
    }

    protected override void Start()
    {
        base.Start();

        if (playerInputHandler == null)
            playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (IsVisible)
            playerInputHandler?.SetInputEnabled(false);

        if (codeInput != null)
        {
            codeInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            codeInput.onValueChanged.AddListener(OnCodeInputChanged);
            codeInput.onValueChanged.AddListener(text =>
            {
                if (!isAutoClosing)
                {
                    UpdateErrorHighlight(text);
                    UpdateAutocomplete();
                }
            });
            codeInput.onSubmit.AddListener(OnInputSubmit);
            prevInputText = codeInput.text;
        }

        runButton?.onClick.AddListener(RunCode);
        clearButton?.onClick.AddListener(ClearLog);
        closeButton?.onClick.AddListener(Hide);

        // caster가 이미 설정돼 있으면 마나 스탯을 초기에 가져온다.
        if (caster != null)
            manaStat = (caster as IManaUser)?.ManaStat;

        UpdateLabels();
        AppendLog("<color=#5C6370>─── Spell Editor ────────────────────────────────</color>");
        AppendLog("<color=#5C6370>Run 버튼 또는 Ctrl+Enter로 실행  |  Tab으로 자동완성</color>");
        RefreshLog();
    }

    private void Update()
    {
        if (acPopup == null || !acPopup.activeSelf) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame)
        {
            AcceptAutocomplete(acSelectedIndex);
        }
        else if (kb.escapeKey.wasPressedThisFrame)
        {
            HideAutocomplete();
        }
        else if (kb.upArrowKey.wasPressedThisFrame)
        {
            acSelectedIndex = (acSelectedIndex - 1 + acCurrentList.Count) % acCurrentList.Count;
            RefreshAutocompleteHighlight();
        }
        else if (kb.downArrowKey.wasPressedThisFrame)
        {
            acSelectedIndex = (acSelectedIndex + 1) % acCurrentList.Count;
            RefreshAutocompleteHighlight();
        }
    }

    // ─────────────────────────────────────────────────────────
    // 입력 처리
    // ─────────────────────────────────────────────────────────

    private void OnInputSubmit(string _)
    {
        // 자동완성 팝업이 열려 있으면 Enter로 수락 (개행 삽입 생략)
        // deferred=true: onSubmit 내 즉시 ActivateInputField 호출을 피해 SelectAll 문제 방지
        // suppressNextNewline: MultiLineNewline 타입이 onSubmit 후 자동 삽입하는 \n 제거 대비
        if (acPopup != null && acPopup.activeSelf)
        {
            suppressNextNewline = true;
            AcceptAutocomplete(acSelectedIndex, deferred: true);
            return;
        }

        var kb = Keyboard.current;
        bool ctrlHeld = kb != null && kb.ctrlKey.isPressed;

        if (ctrlHeld)
        {
            RunCode();
        }
        else
        {
            int insertPos   = codeInput.stringPosition;
            int newCaretPos = InsertNewline(insertPos);
            StartCoroutine(ReactivateInputField(newCaretPos));
        }
    }

    private System.Collections.IEnumerator ReactivateInputField(int caretPos)
    {
        yield return null;
        if (IsVisible && codeInput != null)
        {
            codeInput.ActivateInputField();
            yield return null;
            codeInput.stringPosition                = caretPos;
            codeInput.selectionStringAnchorPosition = caretPos;
            codeInput.selectionStringFocusPosition  = caretPos;
        }
    }

    private int InsertNewline(int pos)
    {
        string text = codeInput.text;

        int lineStart  = pos > 0 ? text.LastIndexOf('\n', pos - 1) + 1 : 0;
        string curLine = text.Substring(lineStart, pos - lineStart);
        string indent  = GetLeadingWhitespace(curLine);

        char before = pos > 0          ? text[pos - 1] : '\0';
        char after  = pos < text.Length ? text[pos]     : '\0';

        string insertion;
        int    newCaretPos;

        if (before == '{' && after == '}')
        {
            string inner = indent + "    ";
            insertion   = "\n" + inner + "\n" + indent;
            newCaretPos = pos + 1 + inner.Length;
        }
        else
        {
            insertion   = "\n" + indent;
            newCaretPos = pos + 1 + indent.Length;
        }

        isAutoClosing  = true;
        codeInput.text = text.Insert(pos, insertion);
        isAutoClosing  = false;
        prevInputText  = codeInput.text;

        return newCaretPos;
    }

    private static string GetLeadingWhitespace(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            i++;
        return line.Substring(0, i);
    }

    // ─────────────────────────────────────────────────────────
    // 자동 괄호 닫기
    // ─────────────────────────────────────────────────────────

    private void OnCodeInputChanged(string newText)
    {
        if (isAutoClosing) return;

        // 자동완성 Enter 수락 후 TMP가 삽입한 \n을 제거한다.
        if (suppressNextNewline)
        {
            suppressNextNewline = false;
            int nlPos = codeInput.stringPosition;
            if (nlPos > 0 && newText.Length > 0 && newText[nlPos - 1] == '\n')
            {
                isAutoClosing            = true;
                codeInput.text           = newText.Remove(nlPos - 1, 1);
                codeInput.stringPosition = nlPos - 1;
                isAutoClosing            = false;
                prevInputText            = codeInput.text;
                return;
            }
        }

        if (newText.Length != prevInputText.Length + 1)
        {
            prevInputText = newText;
            return;
        }

        int caretPos = codeInput.stringPosition;
        if (caretPos <= 0)
        {
            prevInputText = newText;
            return;
        }

        char inserted = newText[caretPos - 1];
        char closing;

        if      (inserted == '(') closing = ')';
        else if (inserted == '{') closing = '}';
        else
        {
            prevInputText = newText;
            return;
        }

        isAutoClosing            = true;
        codeInput.text           = newText.Insert(caretPos, closing.ToString());
        codeInput.stringPosition = caretPos;
        isAutoClosing            = false;

        prevInputText = codeInput.text;
    }

    // ─────────────────────────────────────────────────────────
    // 자동완성 팝업 UI 빌더
    // ─────────────────────────────────────────────────────────

    private void BuildAutocompletePopup()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var popupGo = new GameObject("AutocompletePopup", typeof(RectTransform));
        popupGo.transform.SetParent(canvas.transform, false);

        acPopupRT           = popupGo.GetComponent<RectTransform>();
        acPopupRT.pivot     = new Vector2(0f, 1f);
        acPopupRT.anchorMin = new Vector2(0.5f, 0.5f);
        acPopupRT.anchorMax = new Vector2(0.5f, 0.5f);
        acPopupRT.sizeDelta = new Vector2(300f, 0f);

        var bg = popupGo.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.145f, 0.17f, 0.97f);

        var vl = popupGo.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(0, 0, 2, 2);
        vl.spacing = 1f;

        var csf = popupGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        acPopup = popupGo;

        for (int i = 0; i < AcMaxSlots; i++)
        {
            int capturedIdx = i;

            var itemGo = new GameObject($"Item{i}", typeof(RectTransform));
            itemGo.transform.SetParent(popupGo.transform, false);
            var le = itemGo.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;

            var itemBg  = itemGo.AddComponent<Image>();
            itemBg.color = Color.clear;
            var btn = itemGo.AddComponent<Button>();
            btn.targetGraphic = itemBg;
            btn.onClick.AddListener(() => AcceptAutocomplete(capturedIdx));

            // Label (좌측 70%)
            var labelGo = MakeChild("Label", itemGo.transform);
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0.65f, 1f);
            labelRT.offsetMin = new Vector2(6f, 0f);
            labelRT.offsetMax = Vector2.zero;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.fontSize           = 12f;
            labelTmp.color              = new Color(0.84f, 0.88f, 0.96f);
            labelTmp.enableWordWrapping = false;
            labelTmp.overflowMode       = TextOverflowModes.Ellipsis;
            labelTmp.raycastTarget      = false;

            // Detail (우측 35%)
            var detailGo = MakeChild("Detail", itemGo.transform);
            var detailRT = detailGo.GetComponent<RectTransform>();
            detailRT.anchorMin = new Vector2(0.65f, 0f);
            detailRT.anchorMax = new Vector2(1f, 1f);
            detailRT.offsetMin = Vector2.zero;
            detailRT.offsetMax = new Vector2(-4f, 0f);
            var detailTmp = detailGo.AddComponent<TextMeshProUGUI>();
            detailTmp.fontSize           = 10f;
            detailTmp.color              = new Color(0.36f, 0.39f, 0.44f);
            detailTmp.alignment          = TextAlignmentOptions.MidlineRight;
            detailTmp.enableWordWrapping = false;
            detailTmp.overflowMode       = TextOverflowModes.Ellipsis;
            detailTmp.raycastTarget      = false;

            acSlots.Add((btn, labelTmp, detailTmp, itemBg));
            itemGo.SetActive(false);
        }

        acPopup.SetActive(false);
    }

    private static GameObject MakeChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    // ─────────────────────────────────────────────────────────
    // 자동완성 로직
    // ─────────────────────────────────────────────────────────

    private void UpdateAutocomplete()
    {
        if (acProvider == null || codeInput == null) return;

        string word     = GetWordBeforeCursor();
        string dotIdent = GetIdentifierBeforeDot();

        List<AutocompleteEntry> list;
        if (dotIdent != null)
            list = acProvider.GetMemberCompletions(dotIdent, word);
        else if (word.Length > 0)
            list = acProvider.GetGlobalCompletions(word);
        else
        {
            HideAutocomplete();
            return;
        }

        if (list.Count == 0)
        {
            HideAutocomplete();
            return;
        }

        acWordStart     = codeInput.stringPosition - word.Length;
        acCurrentList   = list;
        acSelectedIndex = 0;
        ShowAutocomplete();
        PositionPopup();
    }

    // 커서 직전 식별자(Letter/Digit/_) 문자열을 반환한다.
    private string GetWordBeforeCursor()
    {
        if (codeInput == null) return "";
        int pos    = codeInput.stringPosition;
        string txt = codeInput.text;
        int start  = pos;
        while (start > 0 && (char.IsLetterOrDigit(txt[start - 1]) || txt[start - 1] == '_'))
            start--;
        return txt.Substring(start, pos - start);
    }

    // 커서 직전 단어 바로 앞에 '.'가 있을 경우 그 앞 식별자를 반환한다.
    // 없으면 null을 반환한다.
    private string GetIdentifierBeforeDot()
    {
        if (codeInput == null) return null;
        int pos    = codeInput.stringPosition;
        string txt = codeInput.text;

        // 현재 단어의 시작 위치
        int wordStart = pos;
        while (wordStart > 0 && (char.IsLetterOrDigit(txt[wordStart - 1]) || txt[wordStart - 1] == '_'))
            wordStart--;

        if (wordStart <= 0 || txt[wordStart - 1] != '.') return null;

        int dotPos    = wordStart - 1;
        int identEnd  = dotPos;
        int identStart = identEnd;
        while (identStart > 0 && (char.IsLetterOrDigit(txt[identStart - 1]) || txt[identStart - 1] == '_'))
            identStart--;

        return identStart < identEnd ? txt.Substring(identStart, identEnd - identStart) : null;
    }

    private void ShowAutocomplete()
    {
        for (int i = 0; i < AcMaxSlots; i++)
        {
            var (btn, label, detail, bg) = acSlots[i];
            bool active = i < acCurrentList.Count;
            btn.gameObject.SetActive(active);
            if (!active) continue;

            label.text  = acCurrentList[i].Label;
            detail.text = acCurrentList[i].Detail;
            bg.color    = i == acSelectedIndex
                ? new Color(0.2f, 0.26f, 0.36f, 1f)
                : Color.clear;
        }
        acPopup.SetActive(true);
    }

    // 선택 인덱스가 바뀔 때 배경색만 갱신한다 (ShowAutocomplete 전체 재호출 없이).
    private void RefreshAutocompleteHighlight()
    {
        for (int i = 0; i < acCurrentList.Count && i < AcMaxSlots; i++)
        {
            var (_, _, _, bg) = acSlots[i];
            bg.color = i == acSelectedIndex
                ? new Color(0.2f, 0.26f, 0.36f, 1f)
                : Color.clear;
        }
    }

    private void HideAutocomplete()
    {
        if (acPopup != null) acPopup.SetActive(false);
        acCurrentList.Clear();
    }

    // 선택된 후보로 현재 단어를 치환한다.
    // acWordStart(팝업 표시 시점에 저장)를 사용해 클릭 시 stringPosition 리셋 문제를 방지한다.
    //
    // deferred=true : onSubmit 핸들러에서 호출할 때 사용.
    //   ActivateInputField()를 onSubmit 내에서 즉시 호출하면 TMP가 submit 후 SelectAll→전체 선택
    //   상태로 만들어 내용이 사라지므로, InsertNewline과 동일하게 다음 프레임에 재활성화한다.
    private void AcceptAutocomplete(int index, bool deferred = false)
    {
        if (index < 0 || index >= acCurrentList.Count) return;

        string insert = acCurrentList[index].Label;
        string txt    = codeInput.text;

        // acWordStart에서 식별자 끝까지를 교체 범위로 사용
        int wordEnd = acWordStart;
        while (wordEnd < txt.Length && (char.IsLetterOrDigit(txt[wordEnd]) || txt[wordEnd] == '_'))
            wordEnd++;

        isAutoClosing            = true;
        codeInput.text           = txt.Substring(0, acWordStart) + insert + txt.Substring(wordEnd);
        codeInput.stringPosition = acWordStart + insert.Length;
        isAutoClosing            = false;
        prevInputText            = codeInput.text;

        HideAutocomplete();

        if (deferred)
            StartCoroutine(ReactivateInputField(codeInput.stringPosition));
        else
            codeInput.ActivateInputField();
    }

    // TMP characterInfo를 이용해 팝업을 커서 아래에 위치시킨다.
    // 팝업이 Canvas 하단을 벗어나면 커서 위쪽으로 반전한다.
    private void PositionPopup()
    {
        if (acPopupRT == null || codeInput == null) return;

        var textComp = codeInput.textComponent;
        textComp.ForceMeshUpdate();

        var textInfo   = textComp.textInfo;
        int caretIdx   = Mathf.Max(0, codeInput.stringPosition - 1);
        Vector3 localPos = Vector3.zero;

        if (textInfo.characterCount > 0 && caretIdx < textInfo.characterCount)
        {
            var charInfo = textInfo.characterInfo[caretIdx];
            localPos = new Vector3(charInfo.bottomRight.x, charInfo.bottomRight.y, 0f);
        }

        // 텍스트 컴포넌트 로컬 좌표 → 월드 좌표
        Vector3 worldPos = textComp.transform.TransformPoint(localPos);

        // 월드 좌표 → Canvas 로컬 좌표
        var canvas   = GetComponentInParent<Canvas>();
        var canvasRT = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos),
            canvas.worldCamera,
            out var canvasLocalPos);

        // 팝업 예상 높이 계산 (ContentSizeFitter가 아직 갱신 전일 수 있으므로 슬롯 수로 추정)
        float slotH     = 23f;
        float popupH    = acCurrentList.Count * slotH + 4f;
        float canvasBot = -canvasRT.rect.height * 0.5f;

        // 팝업은 pivot=(0,1)이므로 anchoredPosition.y가 팝업 상단.
        // 팝업 하단 = anchoredPosition.y - popupH 가 canvasBot 아래면 커서 위쪽으로 반전.
        float finalY = canvasLocalPos.y - popupH < canvasBot
            ? canvasLocalPos.y + popupH   // 커서 위쪽으로 반전
            : canvasLocalPos.y;           // 커서 아래

        acPopupRT.anchoredPosition = new Vector2(canvasLocalPos.x, finalY);
    }

    // ─────────────────────────────────────────────────────────
    // 에러 오버레이
    // ─────────────────────────────────────────────────────────

    private void UpdateErrorHighlight(string code)
    {
        if (errorOverlay == null) return;

        if (string.IsNullOrEmpty(code))
        {
            ClearErrorOverlay();
            return;
        }

        try
        {
            var tokens = new Lexer(code).Tokenize();
            new Parser(tokens).Parse();
            ClearErrorOverlay();
        }
        catch (LexerException ex)
        {
            SetErrorOverlay(code, ex.Line, ex.Column);
        }
        catch (ParserException ex)
        {
            SetErrorOverlay(code, ex.Line, ex.Column);
        }
        catch
        {
            ClearErrorOverlay();
        }
    }

    private void SetErrorOverlay(string source, int line, int col)
    {
        if (errorOverlay == null) return;
        int charIndex = LineColToIndex(source, line, col);
        int length    = GetTokenLength(source, charIndex);
        errorOverlay.text = BuildOverlayText(source, charIndex, length);
    }

    private void ClearErrorOverlay()
    {
        if (errorOverlay == null) return;
        errorOverlay.text = "";
    }

    private static int LineColToIndex(string source, int line, int col)
    {
        int idx = 0, curLine = 1;
        while (idx < source.Length && curLine < line)
        {
            if (source[idx] == '\n') curLine++;
            idx++;
        }
        return Math.Min(idx + col - 1, source.Length - 1);
    }

    private static int GetTokenLength(string source, int charIndex)
    {
        if (charIndex >= source.Length) return 1;
        char c = source[charIndex];
        if (char.IsLetterOrDigit(c) || c == '_')
        {
            int end = charIndex;
            while (end < source.Length && (char.IsLetterOrDigit(source[end]) || source[end] == '_'))
                end++;
            return Math.Max(1, end - charIndex);
        }
        return 1;
    }

    private static string BuildOverlayText(string source, int errorStart, int errorLength)
    {
        int errorEnd = Math.Min(errorStart + errorLength, source.Length);
        var sb = new StringBuilder(source.Length * 3);
        bool inError = false;

        sb.Append("<alpha=#00>");

        for (int i = 0; i < source.Length; i++)
        {
            char c           = source[i];
            bool shouldError = i >= errorStart && i < errorEnd;

            if (shouldError && !inError)
            {
                sb.Append("<alpha=#FF><color=#E06C75><u>");
                inError = true;
            }
            else if (!shouldError && inError)
            {
                sb.Append("</u></color><alpha=#00>");
                inError = false;
            }

            if (c == '\n')
            {
                if (inError) sb.Append("</u></color>");
                sb.Append('\n');
                if (inError) sb.Append("<alpha=#FF><color=#E06C75><u>");
            }
            else
            {
                sb.Append(c);
            }
        }

        if (inError) sb.Append("</u></color>");

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────
    // 실행 로직
    // ─────────────────────────────────────────────────────────

    private void RunCode()
    {
        log.Clear();

        string code = codeInput != null ? codeInput.text : "";
        if (string.IsNullOrWhiteSpace(code))
        {
            AppendLog("<color=#E5C07B>코드가 비어 있습니다.</color>");
            RefreshLog();
            return;
        }

        if (caster == null)
        {
            AppendLog("<color=#E06C75>[오류] Caster Entity가 연결되지 않았습니다.</color>");
            AppendLog("<color=#5C6370>  → Inspector에서 Caster 필드에 Entity를 연결하세요.</color>");
            RefreshLog();
            return;
        }

        manaStat = (caster as IManaUser)?.ManaStat;
        if (manaStat == null)
        {
            AppendLog("<color=#E06C75>[오류] Caster가 IManaUser를 구현하지 않습니다.</color>");
            RefreshLog();
            return;
        }

        var manaTracker = new ManaTracker(manaStat);
        UpdateLabels();

        AppendLog("<color=#5C6370>──────────────────────────────────────────────────</color>");
        AppendLog($"<color=#ABB2BF>▶ 실행  Caster: {caster.name}  " +
                  $"Target: {(target != null ? target.name : "none")}  " +
                  $"Mana: {manaStat.Mana.CurrentValue:F0}/{manaStat.Mana.MaxValue:F0}</color>");

        float t0 = Time.realtimeSinceStartup;

        try
        {
            var tokens  = new Lexer(code).Tokenize();
            var program = new Parser(tokens).Parse();

            var ctx = new SpellContext(caster, target);
            var lib = SpellLibrary.CreateDefault();
            lib.UnlockTier(1);
            lib.ApplyToContext(ctx);
            RegisterDebugCommands(ctx);

            var interp = new Interpreter(ctx, manaTracker);
            float lastVal = manaStat.Mana.CurrentValue;
            interp.Run(program);

            float ms    = (Time.realtimeSinceStartup - t0) * 1000f;
            float spent = lastVal - manaStat.Mana.CurrentValue;

            ClearErrorOverlay();
            AppendLog($"<color=#98C379>✓ 완료</color>  <color=#5C6370>({ms:F1} ms)</color>");
            if (spent > 0.01f)
                AppendLog($"<color=#C678DD>  마나 소모: {spent:F0}  " +
                           $"(남음: {manaStat.Mana.CurrentValue:F0}/{manaStat.Mana.MaxValue:F0})</color>");
        }
        catch (OutOfManaException ex)
        {
            AppendLog($"<color=#E06C75>[마나 부족]</color>  " +
                      $"<color=#ABB2BF>필요 {ex.Required:F0},  남음 {ex.Available:F0}</color>");
        }
        catch (LexerException ex)
        {
            SetErrorOverlay(code, ex.Line, ex.Column);
            AppendLog($"<color=#E06C75>[렉서 오류  {ex.Line}:{ex.Column}]</color>  " +
                      $"<color=#E06C75>{EscapeRich(StripLocation(ex.Message))}</color>");
            string srcLine = GetSourceLine(code, ex.Line);
            if (!string.IsNullOrEmpty(srcLine))
                AppendLog($"<color=#5C6370>  → {EscapeRich(srcLine.Trim())}</color>");
        }
        catch (ParserException ex)
        {
            SetErrorOverlay(code, ex.Line, ex.Column);
            AppendLog($"<color=#E06C75>[파서 오류  {ex.Line}:{ex.Column}]</color>  " +
                      $"<color=#E06C75>{EscapeRich(StripLocation(ex.Message))}</color>");
            string srcLine = GetSourceLine(code, ex.Line);
            if (!string.IsNullOrEmpty(srcLine))
                AppendLog($"<color=#5C6370>  → {EscapeRich(srcLine.Trim())}</color>");
        }
        catch (RuntimeException ex)
        {
            AppendLog($"<color=#E06C75>[오류  {ex.Line}:{ex.Column}]</color>  " +
                      $"<color=#E06C75>{EscapeRich(StripLocation(ex.Message))}</color>");
            string srcLine = GetSourceLine(code, ex.Line);
            if (!string.IsNullOrEmpty(srcLine))
                AppendLog($"<color=#5C6370>  → {EscapeRich(srcLine.Trim())}</color>");
        }
        catch (Exception ex)
        {
            AppendLog($"<color=#E06C75>[예외]</color> {EscapeRich(ex.Message)}");
        }

        UpdateLabels();
        RefreshLog();
    }

    private void RegisterDebugCommands(SpellContext ctx)
    {
        ctx.RegisterCommand("Print", (args, _, line, col) =>
        {
            string val = args.Count > 0 ? FormatDslValue(args[0]) : "null";
            AppendLog($"<color=#61AEEF>  [Print]</color>  {EscapeRich(val)}");
            return null;
        });
    }

    // ─────────────────────────────────────────────────────────
    // 로그 유틸
    // ─────────────────────────────────────────────────────────

    public void Log(string message)
    {
        AppendLog(message); RefreshLog();
    }

    private void AppendLog(string line) => log.AppendLine(line);

    private void ClearLog()
    {
        log.Clear();
        RefreshLog();
    }

    private void RefreshLog()
    {
        if (logText == null) return;
        logText.text = log.ToString();
        Canvas.ForceUpdateCanvases();
        if (logScroll != null)
            logScroll.verticalNormalizedPosition = 0f;
    }

    private void UpdateLabels()
    {
        if (casterLabel != null)
            casterLabel.text = caster != null
                ? $"<color=#98C379>Caster: {caster.name}</color>"
                : "<color=#5C6370>Caster: (없음)</color>";

        if (targetLabel != null)
            targetLabel.text = target != null
                ? $"<color=#98C379>Target: {target.name}</color>"
                : "<color=#5C6370>Target: (없음)</color>";

        if (manaLabel != null)
            manaLabel.text = manaStat != null
                ? $"Mana: {manaStat.Mana.CurrentValue:F0}/{manaStat.Mana.MaxValue:F0}"
                : "Mana: --/--";
    }

    // ─────────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────────

    private static string EscapeRich(string s)
        => s?.Replace("<", "〈").Replace(">", "〉") ?? "";

    private static string StripLocation(string msg)
    {
        if (msg != null && msg.StartsWith("[") && msg.Contains("]"))
            return msg.Substring(msg.IndexOf(']') + 1).TrimStart();
        return msg ?? "";
    }

    private static string GetSourceLine(string code, int lineNum)
    {
        if (lineNum <= 0) return "";
        var lines = code.Split('\n');
        return lineNum <= lines.Length ? lines[lineNum - 1] : "";
    }

    private static string FormatDslValue(object val) => val switch
    {
        null                  => "null",
        bool b                => b ? "true" : "false",
        double d              => d % 1 == 0 ? ((long)d).ToString() : d.ToString("F4"),
        Vector2 v             => $"Vec2({v.x:F2}, {v.y:F2})",
        Entity e              => $"[Entity: {e.name}]",
        ElementType el        => el.ToString(),
        SpellProjectileData p => $"[Projectile @ {p.Position}]",
        SpellFunction f       => $"[Function: {f.Name}]",
        _                     => val.ToString()
    };
}
