using System.Collections.Generic;

// 인게임에서 획득하는 마법 효과 아이템.
// 플레이어는 OnStart / OnUpdate / OnEnd 슬롯에 마법 코드를 작성하여 효과 동작을 정의한다.
//
// EffectType별 라이프사이클 의미:
//   Projectile   — OnStart: 발사 시, OnUpdate: 매 프레임, OnEnd: 피격 시
//   StatusEffect — OnStart: 적용 시, OnUpdate: 매 틱,    OnEnd: 종료 시
//   ActiveSkill  — OnStart: 시전 시
//
// 슬롯별 주입 변수:
//   SpellObject OnStart  : self, proj
//   SpellObject OnUpdate : self, proj
//   SpellObject OnEnd    : self, proj, victim
public enum SpellEffectType
{
    SpellObject, // 월드에 생성되는 라이프사이클 오브젝트 (투사체, 장판, 소환물 등)
}

public class SpellEffectItem
{
    public string         Name        { get; set; } = "새 효과";
    public string         Description { get; set; } = "";
    public SpellEffectType EffectType { get; set; }

    // 이 아이템을 발동할 때 소모할 기본 마나
    public float ManaCost { get; set; } = 0f;

    // ─── 라이프사이클 코드 슬롯 ───────────────────────────────
    // 플레이어가 인게임 에디터에서 편집하는 DSL 코드
    public string OnStartCode  { get; set; } = "";
    public string OnUpdateCode { get; set; } = "";
    public string OnEndCode    { get; set; } = "";

    // ─── 캐시된 AST ───────────────────────────────────────────
    // Compile() 호출 후 사용. 코드 변경 시 InvalidateCache() 또는 재컴파일 필요.
    private ProgramNode _onStartAST;
    private ProgramNode _onUpdateAST;
    private ProgramNode _onEndAST;
    private bool        _compiled;

    // ─── 컴파일 ───────────────────────────────────────────────

    // 세 슬롯의 코드를 한꺼번에 파싱하여 AST를 캐시한다.
    // 코드를 수정한 경우 반드시 다시 호출해야 한다.
    public void Compile()
    {
        _onStartAST  = ParseCode(OnStartCode);
        _onUpdateAST = ParseCode(OnUpdateCode);
        _onEndAST    = ParseCode(OnEndCode);
        _compiled    = true;
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

    // ─── 라이프사이클 실행 ────────────────────────────────────

    // vars: 슬롯 실행 시 추가로 주입할 변수 (proj, victim, target 등)
    public void RunOnStart(SpellContext ctx, ManaTracker mana,
                           Dictionary<string, object> vars = null)
        => RunLifecycle(ref _onStartAST, OnStartCode, ctx, mana, vars);

    public void RunOnUpdate(SpellContext ctx, ManaTracker mana,
                            Dictionary<string, object> vars = null)
        => RunLifecycle(ref _onUpdateAST, OnUpdateCode, ctx, mana, vars);

    public void RunOnEnd(SpellContext ctx, ManaTracker mana,
                         Dictionary<string, object> vars = null)
        => RunLifecycle(ref _onEndAST, OnEndCode, ctx, mana, vars);

    private void RunLifecycle(ref ProgramNode ast, string code,
                              SpellContext ctx, ManaTracker mana,
                              Dictionary<string, object> vars)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        // 미컴파일 상태면 해당 슬롯만 즉시 파싱
        if (!_compiled || ast == null)
            ast = ParseCode(code);

        var interp = new Interpreter(ctx, mana, vars);
        interp.Run(ast);
    }
}
