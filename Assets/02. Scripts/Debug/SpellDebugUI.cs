using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 마법 언어 인게임 디버그 콘솔
// 빈 게임 오브젝트에 컴포넌트로 추가하면 즉시 사용 가능하다.
//
// 조작:
//   ` (백쿼트) — 패널 토글
//   Ctrl+Enter  — 실행 (코드 입력 중)
//   [Run]       — 실행 버튼
//   [Clear]     — 로그 지우기
//   [Reset Mana]— 마나 초기화
//
// DSL 전용 디버그 명령어:
//   Print(value) — 콘솔에 값 출력
//
// Inspector 설정:
//   Caster / Target — 씬의 Entity 연결 (없으면 Run 시 경고)
//   Max Mana        — 실행마다 초기화되는 마나 총량
public class SpellDebugUI : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────

    [SerializeField] private Entity   caster;
    [SerializeField] private Entity   target;
    [SerializeField] private float    maxMana    = 500f;
    [SerializeField] private KeyCode  toggleKey  = KeyCode.BackQuote;
    [SerializeField] private Vector2  panelSize  = new Vector2(680, 520);

    // ─── 색상 팔레트 (One Dark) ───────────────────────────────

    private static readonly Color BgPanel    = Hex("282C34E0");
    private static readonly Color BgHeader   = Hex("21252BE0");
    private static readonly Color BgInput    = Hex("1E2127FF");
    private static readonly Color BgLog      = Hex("1A1D23FF");
    private static readonly Color BgButton   = Hex("3E4451FF");
    private static readonly Color BgButtonHov= Hex("4B5263FF");
    private static readonly Color ColRun     = Hex("98C379FF");
    private static readonly Color ColClear   = Hex("E06C75FF");
    private static readonly Color ColReset   = Hex("61AEEFFF");
    private static readonly Color ColText    = Hex("ABB2BFFF");
    private static readonly Color ColMuted   = Hex("5C6370FF");

    // ─── 내부 상태 ────────────────────────────────────────────

    private GameObject      panel;
    private RectTransform   panelRect;
    private TMP_InputField  codeInput;
    private TextMeshProUGUI logText;
    private ScrollRect      logScroll;
    private TextMeshProUGUI manaLabel;
    private TextMeshProUGUI casterLabel;
    private TextMeshProUGUI targetLabel;

    private readonly StringBuilder log = new();
    private Resource   manaResource;
    private bool       isDragging;
    private Vector2    dragOffset;

    // ─── 기본 예시 코드 ───────────────────────────────────────

    private const string DefaultCode =
        "// 투사체 발사 예시 (basicBolt는 기본 등록된 ProjectileObject)\n" +
        "Fire(basicBolt, self.position, Vec2(1, 0), 10)\n" +
        "\n" +
        "// Bolt / Heal 예시\n" +
        "// Bolt(target, Fire, power)\n" +
        "// Heal(self, 20)\n";

    // ─────────────────────────────────────────────────────────
    // Unity 생명주기
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureEventSystem();
        BuildUI();
    }

    private void Update()
    {
        // 토글
        if (Input.GetKeyDown(toggleKey))
            panel.SetActive(!panel.activeSelf);

        // Ctrl + Enter — 실행
        if (panel.activeSelf &&
            Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKeyDown(KeyCode.Return))
        {
            RunCode();
        }

        // 드래그
        if (isDragging)
        {
            Vector2 mouse = Input.mousePosition;
            panelRect.anchoredPosition = mouse + dragOffset;
        }
    }

    // ─────────────────────────────────────────────────────────
    // UI 구성
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────
        var canvasGo = new GameObject("SpellDebugCanvas");
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();  // ConstantPixelSize (기본값) — 드래그 좌표 일치
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Panel ─────────────────────────────────────────────
        panel = MakeRect("DebugPanel", canvasGo.transform);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta        = panelSize;
        panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = BgPanel;

        // 세로 레이아웃
        var vLayout = panel.AddComponent<VerticalLayoutGroup>();
        vLayout.childControlWidth      = true;
        vLayout.childControlHeight     = true;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing                = 0;
        vLayout.padding                = new RectOffset(0, 0, 0, 0);

        // ── Header ────────────────────────────────────────────
        BuildHeader(panel.transform);

        // ── Context bar ───────────────────────────────────────
        BuildContextBar(panel.transform);

        // ── Code editor ───────────────────────────────────────
        BuildCodeEditor(panel.transform);

        // ── Button bar ────────────────────────────────────────
        BuildButtonBar(panel.transform);

        // ── Log panel ─────────────────────────────────────────
        BuildLogPanel(panel.transform);

        // 기본 코드 삽입
        codeInput.text = DefaultCode;

        AppendLog("<color=#5C6370>─── Spell Debug Console ────────────────────────</color>");
        AppendLog($"<color=#5C6370>토글: {toggleKey}  실행: Ctrl+Enter  디버그 명령어: Print(value)</color>");
        RefreshLog();
    }

    // ─── 헤더 (타이틀 + 드래그 + 닫기) ──────────────────────

    private void BuildHeader(Transform parent)
    {
        var header = MakeRect("Header", parent);
        SetHeight(header, 32);
        var img = header.AddComponent<Image>();
        img.color = BgHeader;

        // 타이틀
        var title = MakeText("Title", header.transform, "🔮 Spell Debug Console", 13, ColText);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-40, 0);

        // 드래그 핸들 (헤더 전체)
        var trigger = header.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
        {
            isDragging = true;
            // ConstantPixelSize Canvas: mousePosition == anchoredPosition 좌표계
            dragOffset = panelRect.anchoredPosition - (Vector2)Input.mousePosition;
        });
        AddTrigger(trigger, EventTriggerType.PointerUp, _ => isDragging = false);

        // 닫기 버튼
        var closeBtn = MakeButton("CloseBtn", header.transform, "✕",
            12, Color.white, new Color(0.8f, 0.3f, 0.3f));
        var closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 0);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot     = new Vector2(1, 0.5f);
        closeBtnRect.offsetMin = new Vector2(-32, 4);
        closeBtnRect.offsetMax = new Vector2(-4, -4);
        closeBtn.GetComponent<Button>().onClick.AddListener(() => panel.SetActive(false));
    }

    // ─── 컨텍스트 바 (Caster / Target / Mana) ────────────────

    private void BuildContextBar(Transform parent)
    {
        var bar = MakeRect("ContextBar", parent);
        SetHeight(bar, 26);
        bar.GetComponent<RectTransform>(); // ensure rect exists
        var img = bar.AddComponent<Image>();
        img.color = new Color(0.14f, 0.16f, 0.19f, 0.9f);

        var hLayout = bar.AddComponent<HorizontalLayoutGroup>();
        hLayout.childControlHeight     = true;
        hLayout.childControlWidth      = true;
        hLayout.childForceExpandHeight = true;
        hLayout.childForceExpandWidth  = false;
        hLayout.spacing  = 0;
        hLayout.padding  = new RectOffset(8, 8, 4, 4);

        casterLabel = MakeText("CasterLabel", bar.transform,
            GetEntityLabel("Caster", caster), 11, ColMuted);
        SetWidth(casterLabel.gameObject, 210);

        targetLabel = MakeText("TargetLabel", bar.transform,
            GetEntityLabel("Target", target), 11, ColMuted);
        SetWidth(targetLabel.gameObject, 210);

        // 오른쪽 마나 표시
        var spacer = MakeRect("Spacer", bar.transform);
        var spacerEl = spacer.AddComponent<LayoutElement>();
        spacerEl.flexibleWidth = 1;

        manaLabel = MakeText("ManaLabel", bar.transform,
            $"Mana: {maxMana:F0}/{maxMana:F0}", 11, new Color(0.78f, 0.67f, 0.94f));
        SetWidth(manaLabel.gameObject, 150);
        manaLabel.alignment = TextAlignmentOptions.Right;
    }

    // ─── 코드 에디터 ─────────────────────────────────────────

    private void BuildCodeEditor(Transform parent)
    {
        var wrapper = MakeRect("EditorWrapper", parent);
        var wrapperLayout = wrapper.AddComponent<LayoutElement>();
        wrapperLayout.preferredHeight = panelSize.y * 0.52f;
        wrapperLayout.flexibleHeight  = 1;

        var wrapperImg = wrapper.AddComponent<Image>();
        wrapperImg.color = BgInput;

        codeInput = BuildInputField(wrapper.transform,
            "// DSL 코드를 입력하세요...\n// 예: Fire(self.position, Vec2(1,0), Fire, 50, 10)");
        var inputRect = codeInput.GetComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = new Vector2(4, 4);
        inputRect.offsetMax = new Vector2(-4, -4);
    }

    // ─── 버튼 바 ─────────────────────────────────────────────

    private void BuildButtonBar(Transform parent)
    {
        var bar = MakeRect("ButtonBar", parent);
        SetHeight(bar, 34);
        bar.AddComponent<Image>().color = BgHeader;

        var hLayout = bar.AddComponent<HorizontalLayoutGroup>();
        hLayout.childControlHeight     = true;
        hLayout.childControlWidth      = true;
        hLayout.childForceExpandHeight = true;
        hLayout.childForceExpandWidth  = false;
        hLayout.spacing = 6;
        hLayout.padding = new RectOffset(8, 8, 4, 4);

        // Run
        var runBtn = MakeButton("RunBtn", bar.transform,
            "▶  Run  (Ctrl+Enter)", 12, Color.black, ColRun);
        SetWidth(runBtn, 160);
        runBtn.GetComponent<Button>().onClick.AddListener(RunCode);

        // Clear
        var clearBtn = MakeButton("ClearBtn", bar.transform, "Clear Log", 12, Color.white, ColClear);
        SetWidth(clearBtn, 90);
        clearBtn.GetComponent<Button>().onClick.AddListener(ClearLog);

        // Reset Mana
        var resetBtn = MakeButton("ResetBtn", bar.transform, "Reset Mana", 12, Color.white, ColReset);
        SetWidth(resetBtn, 100);
        resetBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            manaResource?.SetToMax();
            UpdateManaLabel();
            AppendLog("<color=#61AEEF>  마나 초기화됨.</color>");
            RefreshLog();
        });

        // 오른쪽에 단축키 힌트
        var spacer = MakeRect("Spacer", bar.transform);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        var hint = MakeText("Hint", bar.transform, "` 토글", 10, ColMuted);
        SetWidth(hint.gameObject, 60);
        hint.alignment = TextAlignmentOptions.Right;
    }

    // ─── 로그 패널 ────────────────────────────────────────────

    private void BuildLogPanel(Transform parent)
    {
        var wrapper = MakeRect("LogWrapper", parent);
        var wrapperLayout = wrapper.AddComponent<LayoutElement>();
        wrapperLayout.preferredHeight = panelSize.y * 0.33f;
        wrapperLayout.flexibleHeight  = 1;
        wrapper.AddComponent<Image>().color = BgLog;

        // ScrollRect 설정
        logScroll = wrapper.AddComponent<ScrollRect>();
        logScroll.horizontal = false;
        logScroll.vertical   = true;
        logScroll.scrollSensitivity = 20f;
        logScroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewport = MakeRect("Viewport", wrapper.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = new Vector2(6, 4);
        vpRect.offsetMax = new Vector2(-6, -4);
        viewport.AddComponent<RectMask2D>();
        logScroll.viewport = vpRect;

        // Content
        var content = MakeRect("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vl = content.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth   = true;
        vl.childControlHeight  = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        logScroll.content = contentRect;

        // 로그 텍스트
        var logGo = MakeRect("LogText", content.transform);
        logText = logGo.AddComponent<TextMeshProUGUI>();
        logText.fontSize     = 12;
        logText.color        = ColText;
        logText.richText     = true;
        logText.enableWordWrapping = false;
        logText.overflowMode = TextOverflowModes.Overflow;
    }

    // ─────────────────────────────────────────────────────────
    // 실행 로직
    // ─────────────────────────────────────────────────────────

    private void RunCode()
    {
        log.Clear();

        string code = codeInput.text;
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

        // 마나 리소스 초기화
        manaResource = new Resource(maxMana, 0f);
        var manaTracker = new ManaTracker(manaResource);
        UpdateManaLabel();

        AppendLog($"<color=#5C6370>──────────────────────────────────────────────────</color>");
        AppendLog($"<color=#ABB2BF>▶ 실행  Caster: {caster.name}  " +
                  $"Target: {(target != null ? target.name : "none")}  " +
                  $"Mana: {maxMana}</color>");

        float t0 = Time.realtimeSinceStartup;

        try
        {
            // ── Lex ──────────────────────────────────────────
            var tokens = new Lexer(code).Tokenize();

            // ── Parse ─────────────────────────────────────────
            var program = new Parser(tokens).Parse();

            // ── Context ───────────────────────────────────────
            var ctx = new SpellContext(caster, target);
            var lib = SpellLibrary.CreateDefault();
            lib.UnlockTier(1); // 디버그 콘솔은 모든 티어 해금
            lib.ApplyToContext(ctx);
            RegisterDebugCommands(ctx);

            // ── Interpret ─────────────────────────────────────
            var interp = new Interpreter(ctx, manaTracker);
            interp.Run(program);

            // ── 결과 ──────────────────────────────────────────
            float ms    = (Time.realtimeSinceStartup - t0) * 1000f;
            float spent = maxMana - manaResource.CurrentValue;

            AppendLog($"<color=#98C379>✓ 완료</color>  " +
                      $"<color=#5C6370>({ms:F1} ms)</color>");
            if (spent > 0.01f)
                AppendLog($"<color=#C678DD>  마나 소모: {spent:F0}  " +
                           $"(남음: {manaResource.CurrentValue:F0}/{maxMana})</color>");
        }
        catch (OutOfManaException ex)
        {
            AppendLog($"<color=#E06C75>[마나 부족]</color>  " +
                      $"<color=#ABB2BF>필요 {ex.Required:F0},  남음 {ex.Available:F0}</color>");
        }
        catch (RuntimeException ex)
        {
            // 오류 위치를 코드에서 하이라이트해서 표시
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

        UpdateManaLabel();
        RefreshLog();
    }

    // ─── 디버그 전용 명령어 등록 ──────────────────────────────

    private void RegisterDebugCommands(SpellContext ctx)
    {
        // Print(value) — 콘솔에 값 출력
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

        // 다음 프레임에 스크롤 최하단 이동
        Canvas.ForceUpdateCanvases();
        logScroll.verticalNormalizedPosition = 0f;
    }

    private void UpdateManaLabel()
    {
        if (manaLabel == null || manaResource == null) return;
        manaLabel.text =
            $"Mana: {manaResource.CurrentValue:F0}/{maxMana:F0}";
    }

    // ─────────────────────────────────────────────────────────
    // UI 헬퍼
    // ─────────────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent,
        string text, float size, Color color)
    {
        var go = MakeRect(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    private static GameObject MakeButton(string name, Transform parent,
        string label, float fontSize, Color textColor, Color bgColor)
    {
        var go  = MakeRect(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor     = bgColor * 0.8f;
        btn.colors = colors;

        var txt = MakeText("Label", go.transform, label, fontSize, textColor);
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        txt.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static TMP_InputField BuildInputField(Transform parent, string placeholder)
    {
        var root = MakeRect("InputField", parent);
        var input = root.AddComponent<TMP_InputField>();

        // Text Area (클리핑)
        var textArea = MakeRect("Text Area", root.transform);
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(4, 4);
        taRect.offsetMax = new Vector2(-4, -4);
        textArea.AddComponent<RectMask2D>();
        input.textViewport = taRect;

        // Placeholder
        var ph    = MakeRect("Placeholder", textArea.transform);
        var phRect = ph.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var phText = ph.AddComponent<TextMeshProUGUI>();
        phText.text     = placeholder;
        phText.fontSize = 13;
        phText.color    = ColMuted;
        phText.enableWordWrapping = false;
        input.placeholder = phText;

        // 실제 텍스트
        var txt     = MakeRect("Text", textArea.transform);
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var tmpText = txt.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 13;
        tmpText.color    = new Color(0.84f, 0.88f, 0.96f);
        tmpText.enableWordWrapping = false;
        tmpText.overflowMode      = TextOverflowModes.Overflow;
        input.textComponent = tmpText;

        input.lineType          = TMP_InputField.LineType.MultiLineNewline;
        input.richText          = false;
        input.scrollSensitivity = 20f;

        return input;
    }

    private static void SetHeight(GameObject go, float h)
    {
        var el = go.AddComponent<LayoutElement>();
        el.minHeight       = h;
        el.preferredHeight = h;
    }

    private static void SetWidth(GameObject go, float w)
    {
        var el = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        el.minWidth       = w;
        el.preferredWidth = w;
    }

    private static void AddTrigger(EventTrigger trigger,
        EventTriggerType type, Action<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action(data));
        trigger.triggers.Add(entry);
    }

    // ─────────────────────────────────────────────────────────
    // 문자열 유틸
    // ─────────────────────────────────────────────────────────

    private static string GetEntityLabel(string role, Entity e)
        => e != null
            ? $"<color=#98C379>{role}: {e.name}</color>"
            : $"<color=#5C6370>{role}: (없음)</color>";

    private static string EscapeRich(string s)
        => s?.Replace("<", "〈").Replace(">", "〉") ?? "";

    // "[1:3] 메시지" → "메시지" (앞의 위치 정보 제거)
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
        null          => "null",
        bool b        => b ? "true" : "false",
        double d      => d % 1 == 0 ? ((long)d).ToString() : d.ToString("F4"),
        Vector2 v     => $"Vec2({v.x:F2}, {v.y:F2})",
        Entity e      => $"[Entity: {e.name}]",
        ElementType el => el.ToString(),
        SpellProjectileData p => $"[Projectile @ {p.Position}]",
        SpellFunction f => $"[Function: {f.Name}]",
        _             => val.ToString()
    };

    private static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
        return Color.white;
    }

    // EventSystem 없으면 자동 생성
    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }
    }
}
