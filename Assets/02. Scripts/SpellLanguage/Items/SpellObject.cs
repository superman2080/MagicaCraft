using System.Collections.Generic;

// 라이프사이클 객체(투사체, 장판, 소환물 등)의 동작을 정의하는 기반 클래스.
// 플레이어는 OnStartCode / OnUpdateCode / OnFinishCode 슬롯에 DSL 코드를 작성한다.
//
// 핵심 설계 원칙: SpellObject는 인스턴스별 상태를 저장하지 않는다.
// 투사체별 데이터(proj, victim)는 MonoBehaviour가 instanceVars로 전달한다.
// 동일한 SpellObject를 여러 인스턴스가 동시에 안전하게 공유 가능하다.
public abstract class SpellObject : SpellItem
{
    // 플레이어 작성 코드 슬롯
    public string OnStartCode  { get; set; } = "";
    public string OnUpdateCode { get; set; } = "";
    public string OnFinishCode { get; set; } = "";

    // ─── AST 캐시 ─────────────────────────────────────────────
    private ProgramNode _startAST;
    private ProgramNode _updateAST;
    private ProgramNode _finishAST;
    private bool        _compiled;

    // 세 슬롯의 코드를 한꺼번에 파싱하여 AST를 캐시한다.
    public void Compile()
    {
        _startAST  = ParseCode(OnStartCode);
        _updateAST = ParseCode(OnUpdateCode);
        _finishAST = ParseCode(OnFinishCode);
        _compiled  = true;
    }

    // 캐시를 무효화한다. 다음 실행 시 해당 슬롯을 자동으로 재파싱한다.
    public void InvalidateCache() => _compiled = false;

    private static ProgramNode ParseCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ProgramNode(new List<Stmt>());
        var tokens = new Lexer(code).Tokenize();
        return new Parser(tokens).Parse();
    }

    // ─── sealed 실행 메서드 ────────────────────────────────────
    // 실행 흐름은 기반 클래스가 통제한다. 서브클래스는 훅(BeforeXxx/AfterXxx)만 재정의한다.

    public void RunStart(SpellContext ctx, ManaTracker mana,
                         IReadOnlyDictionary<string, object> instanceVars = null)
    {
        BeforeStart();
        RunLifecycle(ref _startAST, OnStartCode, ctx, mana, GetStartVars(), instanceVars);
        AfterStart();
    }

    public void RunUpdate(SpellContext ctx, ManaTracker mana,
                          IReadOnlyDictionary<string, object> instanceVars = null)
    {
        BeforeUpdate();
        RunLifecycle(ref _updateAST, OnUpdateCode, ctx, mana, GetUpdateVars(), instanceVars);
        AfterUpdate();
    }

    public void RunFinish(SpellContext ctx, ManaTracker mana,
                          IReadOnlyDictionary<string, object> instanceVars = null)
    {
        BeforeFinish();
        RunLifecycle(ref _finishAST, OnFinishCode, ctx, mana, GetFinishVars(), instanceVars);
        AfterFinish();
    }

    // ─── 서브클래스 확장 지점 ─────────────────────────────────

    // 타입별 추가 변수 제공 (인스턴스 독립적)
    protected virtual IReadOnlyDictionary<string, object> GetStartVars()  => null;
    protected virtual IReadOnlyDictionary<string, object> GetUpdateVars() => null;
    protected virtual IReadOnlyDictionary<string, object> GetFinishVars() => null;

    // Unity API 호출 등 추가 로직 삽입용 훅 (선택적 override)
    protected virtual void BeforeStart()  { }
    protected virtual void AfterStart()   { }
    protected virtual void BeforeUpdate() { }
    protected virtual void AfterUpdate()  { }
    protected virtual void BeforeFinish() { }
    protected virtual void AfterFinish()  { }

    // ─── 내부 실행 ────────────────────────────────────────────

    private void RunLifecycle(ref ProgramNode ast, string code,
                              SpellContext ctx, ManaTracker mana,
                              IReadOnlyDictionary<string, object> typeVars,
                              IReadOnlyDictionary<string, object> instanceVars)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        if (!_compiled || ast == null)
            ast = ParseCode(code);

        // 타입별 변수와 인스턴스 변수를 병합
        Dictionary<string, object> extraVars = null;
        if (typeVars != null || instanceVars != null)
        {
            extraVars = new Dictionary<string, object>();
            if (typeVars != null)
                foreach (var (k, v) in typeVars) extraVars[k] = v;
            if (instanceVars != null)
                foreach (var (k, v) in instanceVars) extraVars[k] = v;
        }

        var interp = new Interpreter(ctx, mana, extraVars);
        interp.Run(ast);
    }
}
