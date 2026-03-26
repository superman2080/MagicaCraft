using System;
using System.Collections.Generic;
using UnityEngine;

// DSL 인터프리터 - AST를 순회하며 실행한다.
// 마나는 제어문이 아닌 각 마법 명령어(Bolt, Heal 등)에서만 소모된다.
//
// 지원 런타임 타입 (모두 object로 전달)
//   double        - 정수·실수 (int, float 모두 double로 통일)
//   bool          - 불리언
//   string        - 문자열
//   Vector2       - 2D 좌표
//   Entity        - 게임 엔티티 (시전자·타겟)
//   SpellFunction - 플레이어가 def로 정의한 함수
//   SpellBuiltin  - 인터프리터 내장 함수 (vec2, abs 등)
//   null          - 값 없음
public class Interpreter
{
    private readonly SpellContext     _ctx;
    private readonly ManaTracker      _mana;
    private readonly SpellEnvironment _env;

    // 단일 loop/while의 최대 반복 횟수 (무한 루프 방지)
    private const int MaxIterations = 1000;

    // ─── 생성자 ───────────────────────────────────────────────

    // extraVars: 함수 호출 시 파라미터를 초기 변수로 주입하기 위해 사용
    public Interpreter(SpellContext ctx, ManaTracker mana,
                       Dictionary<string, object> extraVars = null)
    {
        _ctx  = ctx;
        _mana = mana;
        _env  = new SpellEnvironment();

        _ctx.Mana = _mana;
        _ctx.InjectBuiltins(_env);
        RegisterBuiltins();

        if (extraVars != null)
            foreach (var (key, val) in extraVars)
                _env.Define(key, val);
    }

    // ─── 진입점 ───────────────────────────────────────────────

    public void Run(ProgramNode program)
    {
        foreach (var stmt in program.Statements)
            ExecuteStmt(stmt);
    }

    // SpellFunction 본문(List<Stmt>)을 직접 실행하는 진입점
    // SpellProjectile 콜백(onHit, onUpdate)에서 사용한다.
    public void Run(List<Stmt> stmts)
    {
        foreach (var stmt in stmts)
            ExecuteStmt(stmt);
    }

    // ─── 문장 실행 ────────────────────────────────────────────

    private void ExecuteStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case LetStmt    s: ExecuteLet(s);              break;
            case AssignStmt s: ExecuteAssign(s);           break;
            case IfStmt     s: ExecuteIf(s);               break;
            case LoopStmt   s: ExecuteLoop(s);             break;
            case WhileStmt  s: ExecuteWhile(s);            break;
            case DefStmt    s: ExecuteDef(s);              break;
            case ReturnStmt s: ExecuteReturn(s);           break;
            case ExprStmt   s: EvalExpr(s.Expression);    break;
            default:
                throw new RuntimeException(
                    $"알 수 없는 문장 타입: {stmt.GetType().Name}", stmt.Line, stmt.Column);
        }
    }

    // let x = expr;
    private void ExecuteLet(LetStmt s)
    {
        _env.Define(s.Name, EvalExpr(s.Initializer));
    }

    // x = expr;
    private void ExecuteAssign(AssignStmt s)
    {
        _env.Set(s.Name, EvalExpr(s.Value), s.Line, s.Column);
    }

    // if (expr) { ... } else { ... }
    private void ExecuteIf(IfStmt s)
    {
        if (IsTruthy(EvalExpr(s.Condition), s.Condition.Line, s.Condition.Column))
            ExecuteBlock(s.ThenBranch);
        else if (s.ElseBranch != null)
            ExecuteBlock(s.ElseBranch);
    }

    // loop (n) { ... }
    private void ExecuteLoop(LoopStmt s)
    {
        int count = (int)ToDouble(EvalExpr(s.Count), s.Count.Line, s.Count.Column);

        if (count > MaxIterations)
            throw new RuntimeException(
                $"loop 횟수 {count}는 최대 {MaxIterations}을 초과합니다.", s.Line, s.Column);

        for (int i = 0; i < count; i++)
            ExecuteBlock(s.Body);
    }

    // while (expr) { ... }
    private void ExecuteWhile(WhileStmt s)
    {
        int iterations = 0;
        while (true)
        {
            if (!IsTruthy(EvalExpr(s.Condition), s.Condition.Line, s.Condition.Column))
                break;

            if (++iterations > MaxIterations)
                throw new RuntimeException(
                    $"while이 최대 반복 횟수({MaxIterations})를 초과했습니다.", s.Line, s.Column);

            ExecuteBlock(s.Body);
        }
    }

    // def name(params) { ... }
    // 함수 정의 자체는 마나를 소비하지 않는다. 호출 시점에 마나를 소비한다.
    private void ExecuteDef(DefStmt s)
    {
        // 명령어 또는 내장 함수와 동일한 이름으로 재정의하는 것을 차단한다.
        if (_ctx.HasCommand(s.Name))
            throw new RuntimeException(
                $"'{s.Name}'은 마법 명령어이므로 재정의할 수 없습니다.", s.Line, s.Column);
        if (_env.IsDefined(s.Name) && _env.Get(s.Name, s.Line, s.Column) is SpellBuiltin)
            throw new RuntimeException(
                $"'{s.Name}'은 내장 함수이므로 재정의할 수 없습니다.", s.Line, s.Column);

        _env.Define(s.Name, new SpellFunction(s.Name, s.Params, s.Body));
    }

    // return expr;  /  return;
    // ReturnSignal 예외로 호출 스택을 되감는다.
    private void ExecuteReturn(ReturnStmt s)
    {
        var value = s.Value != null ? EvalExpr(s.Value) : null;
        throw new ReturnSignal(value);
    }

    // { stmt* } — 새 스코프 생성 후 실행, 종료 시 반드시 해제
    private void ExecuteBlock(List<Stmt> stmts)
    {
        _env.PushScope();
        try
        {
            foreach (var stmt in stmts)
                ExecuteStmt(stmt);
        }
        finally
        {
            _env.PopScope();
        }
    }

    // ─── 표현식 평가 ──────────────────────────────────────────

    private object EvalExpr(Expr expr)
    {
        return expr switch
        {
            NumberExpr     e => e.Value,
            StringExpr     e => e.Value,
            BoolExpr       e => e.Value,
            IdentifierExpr e => _env.Get(e.Name, e.Line, e.Column),
            MemberExpr     e => EvalMember(e),
            CallExpr       e => EvalCall(e),
            BinaryExpr     e => EvalBinary(e),
            UnaryExpr      e => EvalUnary(e),
            _ => throw new RuntimeException(
                    $"알 수 없는 표현식 타입: {expr.GetType().Name}", expr.Line, expr.Column)
        };
    }

    // target.Hp, pos.X, self.Position, proj.velocity 등
    private object EvalMember(MemberExpr e)
    {
        var obj = EvalExpr(e.Object);

        return obj switch
        {
            Entity              entity => EvalEntityMember(entity, e.Member, e.Line, e.Column),
            Vector2             v      => EvalVector2Member(v, e.Member, e.Line, e.Column),
            SpellProjectileData proj   => EvalProjectileMember(proj, e.Member, e.Line, e.Column),
            _ => throw new RuntimeException(
                    $"'{obj?.GetType().Name ?? "null"}'은 멤버 접근('.{e.Member}')을 지원하지 않습니다.",
                    e.Line, e.Column)
        };
    }

    private object EvalProjectileMember(SpellProjectileData proj, string member, int line, int col)
    {
        return member switch
        {
            "position" => (object)proj.Position,
            "velocity" => (object)proj.Velocity,
            "speed"    => (double)proj.Speed,
            "damage"   => (double)proj.Damage,
            "element"  => (object)proj.Element,
            _ => throw new RuntimeException($"Projectile에 '{member}' 필드가 없습니다.", line, col)
        };
    }

    private object EvalEntityMember(Entity entity, string member, int line, int col)
    {
        return member switch
        {
            "hp"        => (double)entity.HP,
            "maxHp"     => (double)entity.DefaultStat.HP.MaxValue,
            "hpRatio"   => (double)entity.DefaultStat.HP.Ratio,
            "moveSpeed" => (double)entity.DefaultStat.Get(DefaultStatType.MOVE_SPEED),
            "position"  => entity.Rb2d != null ? (object)entity.Rb2d.position : Vector2.zero,
            _ => throw new RuntimeException($"Entity에 '{member}' 필드가 없습니다.", line, col)
        };
    }

    private object EvalVector2Member(Vector2 v, string member, int line, int col)
    {
        return member switch
        {
            "x"         => (double)v.x,
            "y"         => (double)v.y,
            "magnitude" => (double)v.magnitude,
            _ => throw new RuntimeException($"vec2에 '{member}' 필드가 없습니다.", line, col)
        };
    }

    // bolt(target, FIRE, 30), vec2(10, 5), myFunc(x)
    private object EvalCall(CallExpr e)
    {
        if (e.Callee is not IdentifierExpr nameExpr)
            throw new RuntimeException("함수 호출은 이름으로만 가능합니다.", e.Line, e.Column);

        var name = nameExpr.Name;

        // 인자 평가
        var args = new List<object>(e.Args.Count);
        foreach (var arg in e.Args)
            args.Add(EvalExpr(arg));

        // 환경에서 먼저 탐색 (사용자 정의 함수 + 내장 함수)
        if (_env.IsDefined(name))
        {
            var val = _env.Get(name, e.Line, e.Column);
            return val switch
            {
                SpellFunction fn      => CallSpellFunction(fn, args, e.Line, e.Column),
                SpellBuiltin  builtin => builtin.Call(args, e.Line, e.Column),
                SpellObject   _       => throw new RuntimeException(
                    $"'{name}'은 SpellObject이므로 직접 호출할 수 없습니다. Fire({name}, ...) 형태로 사용하세요.",
                    e.Line, e.Column),
                _ => throw new RuntimeException(
                        $"'{name}'은 호출 가능한 함수가 아닙니다.", e.Line, e.Column)
            };
        }

        // 게임 명령어 탐색 (bolt, heal 등)
        return _ctx.ExecuteCommand(name, args, e.Line, e.Column);
    }

    // 사용자 정의 함수 호출
    // 마나는 공유하고 환경은 격리된 새 인터프리터에서 실행한다.
    private object CallSpellFunction(SpellFunction fn, List<object> args, int line, int col)
    {
        if (args.Count != fn.Params.Count)
            throw new RuntimeException(
                $"'{fn.Name}': 인자 {fn.Params.Count}개 필요, {args.Count}개 전달됨.", line, col);

        var extraVars = new Dictionary<string, object>();
        for (int i = 0; i < fn.Params.Count; i++)
            extraVars[fn.Params[i]] = args[i];

        // 마나를 공유하는 격리된 인터프리터로 실행
        var fnInterp = new Interpreter(_ctx, _mana, extraVars);
        object returnValue = null;
        try
        {
            foreach (var stmt in fn.Body)
                fnInterp.ExecuteStmt(stmt);
        }
        catch (ReturnSignal ret)
        {
            returnValue = ret.Value;
        }
        return returnValue;
    }

    // ─── 이항 연산 ────────────────────────────────────────────

    private object EvalBinary(BinaryExpr e)
    {
        // 논리 연산자: 단락 평가 (short-circuit)
        if (e.Op == "&&")
        {
            return IsTruthy(EvalExpr(e.Left),  e.Left.Line,  e.Left.Column) &&
                   IsTruthy(EvalExpr(e.Right), e.Right.Line, e.Right.Column);
        }
        if (e.Op == "||")
        {
            return IsTruthy(EvalExpr(e.Left),  e.Left.Line,  e.Left.Column) ||
                   IsTruthy(EvalExpr(e.Right), e.Right.Line, e.Right.Column);
        }

        var left  = EvalExpr(e.Left);
        var right = EvalExpr(e.Right);

        return e.Op switch
        {
            "+"  => AddValues(left, right, e.Line, e.Column),
            "-"  => SubValues(left, right, e.Line, e.Column),
            "*"  => MulValues(left, right, e.Line, e.Column),
            "/"  => DivValues(left, right, e.Line, e.Column),
            "%"  => ToDouble(left, e.Line, e.Column) % ToDouble(right, e.Line, e.Column),
            "==" => AreEqual(left, right),
            "!=" => !AreEqual(left, right),
            "<"  => ToDouble(left, e.Line, e.Column) <  ToDouble(right, e.Line, e.Column),
            ">"  => ToDouble(left, e.Line, e.Column) >  ToDouble(right, e.Line, e.Column),
            "<=" => ToDouble(left, e.Line, e.Column) <= ToDouble(right, e.Line, e.Column),
            ">=" => ToDouble(left, e.Line, e.Column) >= ToDouble(right, e.Line, e.Column),
            _    => throw new RuntimeException($"알 수 없는 연산자 '{e.Op}'", e.Line, e.Column)
        };
    }

    // ─── 단항 연산 ────────────────────────────────────────────

    private object EvalUnary(UnaryExpr e)
    {
        var val = EvalExpr(e.Operand);

        return e.Op switch
        {
            "!" => !IsTruthy(val, e.Operand.Line, e.Operand.Column),
            "-" when val is Vector2 v => (object)(-v),
            "-" => -ToDouble(val, e.Operand.Line, e.Operand.Column),
            _   => throw new RuntimeException($"알 수 없는 단항 연산자 '{e.Op}'", e.Line, e.Column)
        };
    }

    // ─── 연산 헬퍼 ────────────────────────────────────────────

    // +: 숫자 덧셈 / Vector2 덧셈 / 문자열 연결
    private object AddValues(object l, object r, int line, int col)
    {
        if (l is string || r is string) return ToStringVal(l) + ToStringVal(r);
        if (l is Vector2 vl && r is Vector2 vr) return vl + vr;
        return ToDouble(l, line, col) + ToDouble(r, line, col);
    }

    // -: 숫자 뺄셈 / Vector2 뺄셈
    private object SubValues(object l, object r, int line, int col)
    {
        if (l is Vector2 vl && r is Vector2 vr) return vl - vr;
        return ToDouble(l, line, col) - ToDouble(r, line, col);
    }

    // *: 숫자 곱셈 / Vector2 × scalar
    private object MulValues(object l, object r, int line, int col)
    {
        if (l is Vector2 vl && r is double dr) return vl * (float)dr;
        if (l is double dl && r is Vector2 vr) return vr * (float)dl;
        return ToDouble(l, line, col) * ToDouble(r, line, col);
    }

    // /: 0 나눗셈 검사 포함
    private object DivValues(object l, object r, int line, int col)
    {
        double divisor = ToDouble(r, line, col);
        if (divisor == 0.0)
            throw new RuntimeException("0으로 나눌 수 없습니다.", line, col);
        return ToDouble(l, line, col) / divisor;
    }

    // ─── 타입 변환 헬퍼 ───────────────────────────────────────

    // 값의 진리값: null / false / 0 / "" 는 거짓, 그 외는 참
    private bool IsTruthy(object val, int line, int col)
    {
        return val switch
        {
            null     => false,
            bool b   => b,
            double d => d != 0.0,
            string s => s.Length > 0,
            _        => true
        };
    }

    private double ToDouble(object val, int line, int col)
    {
        return val switch
        {
            double d => d,
            bool   b => b ? 1.0 : 0.0,
            _ => throw new RuntimeException(
                    $"숫자가 예상되지만 '{val?.GetType().Name ?? "null"}'가 전달되었습니다.", line, col)
        };
    }

    private Vector2 ToVector2(object val, int line, int col)
    {
        return val switch
        {
            Vector2 v => v,
            Entity  e => e.Rb2d != null ? e.Rb2d.position : Vector2.zero,
            _ => throw new RuntimeException(
                    $"vec2가 예상되지만 '{val?.GetType().Name ?? "null"}'가 전달되었습니다.", line, col)
        };
    }

    private string ToStringVal(object val)
    {
        return val switch
        {
            null             => "null",
            bool b           => b ? "true" : "false",
            double d         => d % 1 == 0 ? ((long)d).ToString() : d.ToString("F4"),
            Vector2 v        => $"vec2({v.x:F2}, {v.y:F2})",
            Entity en        => $"[Entity:{en.name}]",
            SpellFunction f  => $"[Function:{f.Name}]",
            SpellObject  o => $"[SpellObject:{o.Name}]",
            SpellAction  a => $"[SpellAction:{a.DSLName}]",
            _                => val.ToString()
        };
    }

    private bool AreEqual(object l, object r)
    {
        if (l == null && r == null) return true;
        if (l == null || r == null) return false;
        return l.Equals(r);
    }

    // ─── 내장 함수 등록 ───────────────────────────────────────
    // 함수명은 C# 메서드 규칙에 따라 PascalCase로 작성한다.

    private void RegisterBuiltins()
    {
        // Vec2(x, y) : Vector2 생성
        _env.Define("Vec2", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 2)
                throw new RuntimeException("Vec2(x, y): 인자 2개 필요", line, col);
            return (object)new Vector2((float)ToDouble(args[0], line, col),
                                       (float)ToDouble(args[1], line, col));
        }));

        // Distance(a, b) : 두 위치 간의 거리 (double 반환)
        _env.Define("Distance", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 2)
                throw new RuntimeException("Distance(a, b): 인자 2개 필요", line, col);
            return (double)Vector2.Distance(ToVector2(args[0], line, col),
                                            ToVector2(args[1], line, col));
        }));

        // Abs(x)
        _env.Define("Abs", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 1)
                throw new RuntimeException("Abs(x): 인자 1개 필요", line, col);
            return Math.Abs(ToDouble(args[0], line, col));
        }));

        // Min(a, b) / Max(a, b)
        _env.Define("Min", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 2)
                throw new RuntimeException("Min(a, b): 인자 2개 필요", line, col);
            return Math.Min(ToDouble(args[0], line, col), ToDouble(args[1], line, col));
        }));

        _env.Define("Max", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 2)
                throw new RuntimeException("Max(a, b): 인자 2개 필요", line, col);
            return Math.Max(ToDouble(args[0], line, col), ToDouble(args[1], line, col));
        }));

        // Floor(x) / Ceil(x)
        _env.Define("Floor", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 1)
                throw new RuntimeException("Floor(x): 인자 1개 필요", line, col);
            return Math.Floor(ToDouble(args[0], line, col));
        }));

        _env.Define("Ceil", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 1)
                throw new RuntimeException("Ceil(x): 인자 1개 필요", line, col);
            return Math.Ceiling(ToDouble(args[0], line, col));
        }));

        // Sqrt(x) : 제곱근
        _env.Define("Sqrt", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 1)
                throw new RuntimeException("Sqrt(x): 인자 1개 필요", line, col);
            double val = ToDouble(args[0], line, col);
            if (val < 0)
                throw new RuntimeException("Sqrt에 음수를 전달할 수 없습니다.", line, col);
            return Math.Sqrt(val);
        }));

        // Normalize(vec) : Vec2를 단위 벡터로 반환
        _env.Define("Normalize", new SpellBuiltin((args, line, col) =>
        {
            if (args.Count != 1)
                throw new RuntimeException("Normalize(vec): 인자 1개 필요", line, col);
            return (object)ToVector2(args[0], line, col).normalized;
        }));
    }
}

// ─── 보조 타입 ────────────────────────────────────────────────

// 내장 함수 래퍼
// SpellEnvironment에 일반 값처럼 저장되며, SpellFunction(사용자 정의)과 구분된다.
public class SpellBuiltin
{
    private readonly Func<List<object>, int, int, object> _impl;

    public SpellBuiltin(Func<List<object>, int, int, object> impl) { _impl = impl; }

    public object Call(List<object> args, int line, int col) => _impl(args, line, col);
}

// 사용자 정의 함수 (def)
// SpellEnvironment에 저장되는 값이며 CallExpr에서 호출된다.
public class SpellFunction
{
    public string       Name   { get; }
    public List<string> Params { get; }
    public List<Stmt>   Body   { get; }

    public SpellFunction(string name, List<string> @params, List<Stmt> body)
    {
        Name   = name;
        Params = @params;
        Body   = body;
    }
}

// return 시 호출 스택을 되감는 내부 제어 신호 (실제 오류가 아님)
internal class ReturnSignal : Exception
{
    public object Value { get; }
    public ReturnSignal(object value) : base() { Value = value; }
}

// DSL 런타임 오류
public class RuntimeException : Exception
{
    public int Line   { get; }
    public int Column { get; }

    public RuntimeException(string message, int line = 0, int col = 0)
        : base($"[{line}:{col}] {message}")
    {
        Line   = line;
        Column = col;
    }
}
