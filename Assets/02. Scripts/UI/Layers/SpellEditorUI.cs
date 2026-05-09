using System;
using System.Text;
using TMPro;
using UnityEngine;
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
    private Resource manaResource;

    // ─────────────────────────────────────────────────────────
    // Unity 생명주기
    // ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        runButton?.onClick.AddListener(RunCode);
        clearButton?.onClick.AddListener(ClearLog);
        closeButton?.onClick.AddListener(Hide);

        UpdateLabels();
        AppendLog("<color=#5C6370>─── Spell Editor ────────────────────────────────</color>");
        AppendLog("<color=#5C6370>Run 버튼 또는 Ctrl+Enter로 실행  |  Print(value) 출력</color>");
        RefreshLog();
    }

    private void Update()
    {
        if (!IsVisible) return;
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Return))
            RunCode();
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

        manaResource = new Resource(maxMana, 0f);
        var manaTracker = new ManaTracker(manaResource);
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
            float spent = maxMana - manaResource.CurrentValue;

            AppendLog($"<color=#98C379>✓ 완료</color>  <color=#5C6370>({ms:F1} ms)</color>");
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
            manaLabel.text = manaResource != null
                ? $"Mana: {manaResource.CurrentValue:F0}/{maxMana:F0}"
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
