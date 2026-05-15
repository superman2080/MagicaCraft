using System;
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
    [SerializeField] private float maxMana = 500f;

    [Header("UI References")]
    [SerializeField] private TMP_InputField codeInput;
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
    private string prevInputText = "";
    private bool   isAutoClosing = false;

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
        base.Hide();
        playerInputHandler?.SetInputEnabled(true);
    }

    // ─────────────────────────────────────────────────────────
    // Unity 생명주기
    // ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        // Show()가 Start() 이전에 호출될 때를 대비해 여기서 참조 획득
        playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
    }

    protected override void Start()
    {
        base.Start();

        // Awake에서 못 찾았을 경우 재시도 (플레이어가 늦게 초기화되는 경우)
        if (playerInputHandler == null)
            playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();

        // 이미 표시 상태로 시작하면 즉시 차단
        if (IsVisible)
            playerInputHandler?.SetInputEnabled(false);

        if (codeInput != null)
        {
            codeInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            codeInput.onValueChanged.AddListener(OnCodeInputChanged);
            codeInput.onSubmit.AddListener(OnInputSubmit);
            prevInputText = codeInput.text;
        }

        runButton?.onClick.AddListener(RunCode);
        clearButton?.onClick.AddListener(ClearLog);
        closeButton?.onClick.AddListener(Hide);

        UpdateLabels();
        AppendLog("<color=#5C6370>─── Spell Editor ────────────────────────────────</color>");
        AppendLog("<color=#5C6370>Run 버튼 또는 Ctrl+Enter로 실행  |  Print(value) 출력</color>");
        RefreshLog();
    }

    // ─────────────────────────────────────────────────────────
    // 입력 처리
    // ─────────────────────────────────────────────────────────

    // InputSystemUIInputModule이 Enter를 Submit으로 가로채므로
    // Update()가 아닌 onSubmit 콜백에서 처리한다.
    private void OnInputSubmit(string _)
    {
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
        // DeactivateInputField 대기
        yield return null;
        if (IsVisible && codeInput != null)
        {
            codeInput.ActivateInputField();
            // ActivateInputField 초기화 대기 후 선택 해제
            yield return null;
            codeInput.stringPosition              = caretPos;
            codeInput.selectionStringAnchorPosition = caretPos;
            codeInput.selectionStringFocusPosition  = caretPos;
        }
    }

    private int InsertNewline(int pos)
    {
        string text = codeInput.text;

        // 현재 줄 들여쓰기 계산
        int lineStart  = pos > 0 ? text.LastIndexOf('\n', pos - 1) + 1 : 0;
        string curLine = text.Substring(lineStart, pos - lineStart);
        string indent  = GetLeadingWhitespace(curLine);

        // 커서가 { } 사이에 있으면 Allman 스타일 확장
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

        isAutoClosing      = true;
        codeInput.text     = text.Insert(pos, insertion);
        isAutoClosing      = false;
        prevInputText      = codeInput.text;

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

        // 단일 문자 삽입만 처리
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

        isAutoClosing = true;
        codeInput.text = newText.Insert(caretPos, closing.ToString());
        codeInput.stringPosition = caretPos;
        isAutoClosing = false;

        prevInputText = codeInput.text;
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

        var manaData = ScriptableObject.CreateInstance<ManaStatDataSO>();
        manaData.StatDict[ManaStatType.MaxMana]  = maxMana;
        manaData.StatDict[ManaStatType.ManaRegen] = 0f;
        manaStat = new ManaStat(manaData);
        var manaTracker = new ManaTracker(manaStat);
        UpdateLabels();

        AppendLog("<color=#5C6370>──────────────────────────────────────────────────</color>");
        AppendLog($"<color=#ABB2BF>▶ 실행  Caster: {caster.name}  " +
                  $"Target: {(target != null ? target.name : "none")}  " +
                  $"Mana: {maxMana}</color>");

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
            interp.Run(program);

            float ms    = (Time.realtimeSinceStartup - t0) * 1000f;
            float spent = maxMana - manaStat.Mana.CurrentValue;

            AppendLog($"<color=#98C379>✓ 완료</color>  <color=#5C6370>({ms:F1} ms)</color>");
            if (spent > 0.01f)
                AppendLog($"<color=#C678DD>  마나 소모: {spent:F0}  " +
                           $"(남음: {manaStat.Mana.CurrentValue:F0}/{maxMana})</color>");
        }
        catch (OutOfManaException ex)
        {
            AppendLog($"<color=#E06C75>[마나 부족]</color>  " +
                      $"<color=#ABB2BF>필요 {ex.Required:F0},  남음 {ex.Available:F0}</color>");
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
                ? $"Mana: {manaStat.Mana.CurrentValue:F0}/{maxMana:F0}"
                : $"Mana: {maxMana:F0}/{maxMana:F0}";
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
