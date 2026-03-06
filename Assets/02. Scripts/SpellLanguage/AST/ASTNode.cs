using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────
// AST 기반 클래스
// 모든 노드는 소스 위치(Line/Column)를 보관해 인게임 오류 표시에 활용한다.
// ─────────────────────────────────────────────────────────────

// 모든 AST 노드의 추상 기반
public abstract class ASTNode
{
    public int Line   { get; }
    public int Column { get; }

    protected ASTNode(int line, int column)
    {
        Line   = line;
        Column = column;
    }
}

// 값을 반환하는 계산 단위
public abstract class Expr : ASTNode
{
    protected Expr(int line, int col) : base(line, col) { }
}

// 실행되지만 값을 반환하지 않는 실행 단위
public abstract class Stmt : ASTNode
{
    protected Stmt(int line, int col) : base(line, col) { }
}


// ─────────────────────────────────────────────────────────────
// 표현식(Expr) 노드
// ─────────────────────────────────────────────────────────────

// 정수 / 실수 리터럴: 30, 1.5
public class NumberExpr : Expr
{
    public double Value   { get; }
    public bool   IsFloat { get; }

    public NumberExpr(double value, bool isFloat, int line, int col) : base(line, col)
    {
        Value   = value;
        IsFloat = isFloat;
    }
}

// 문자열 리터럴: "FIRE"
public class StringExpr : Expr
{
    public string Value { get; }

    public StringExpr(string value, int line, int col) : base(line, col)
    {
        Value = value;
    }
}

// 불리언 리터럴: true, false
public class BoolExpr : Expr
{
    public bool Value { get; }

    public BoolExpr(bool value, int line, int col) : base(line, col)
    {
        Value = value;
    }
}

// 변수 참조: x, power, target
public class IdentifierExpr : Expr
{
    public string Name { get; }

    public IdentifierExpr(string name, int line, int col) : base(line, col)
    {
        Name = name;
    }
}

// 멤버 접근: target.hp, pos.x
// 체이닝: target.position.x → MemberExpr(MemberExpr(target, "position"), "x")
public class MemberExpr : Expr
{
    public Expr   Object { get; }
    public string Member { get; }

    public MemberExpr(Expr obj, string member, int line, int col) : base(line, col)
    {
        Object = obj;
        Member = member;
    }
}

// 함수 호출: bolt(target, FIRE, 30), vec2(10, 5)
// Callee는 IdentifierExpr 또는 MemberExpr (예: self.cast(...))
public class CallExpr : Expr
{
    public Expr       Callee { get; }
    public List<Expr> Args   { get; }

    public CallExpr(Expr callee, List<Expr> args, int line, int col) : base(line, col)
    {
        Callee = callee;
        Args   = args;
    }
}

// 이항 연산: a + b, a == b, a && b
// Op는 연산자 문자열 그대로 보관 (+, -, *, /, %, ==, !=, <, >, <=, >=, &&, ||)
public class BinaryExpr : Expr
{
    public Expr   Left  { get; }
    public string Op    { get; }
    public Expr   Right { get; }

    public BinaryExpr(Expr left, string op, Expr right, int line, int col) : base(line, col)
    {
        Left  = left;
        Op    = op;
        Right = right;
    }
}

// 단항 연산: !flag, -value
public class UnaryExpr : Expr
{
    public string Op      { get; }
    public Expr   Operand { get; }

    public UnaryExpr(string op, Expr operand, int line, int col) : base(line, col)
    {
        Op      = op;
        Operand = operand;
    }
}
