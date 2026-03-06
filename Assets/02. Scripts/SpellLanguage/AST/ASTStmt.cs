using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────
// 문장(Stmt) 노드
// ─────────────────────────────────────────────────────────────

// 전체 프로그램: 최상위 문장 목록을 담는 루트 노드
public class ProgramNode : ASTNode
{
    public List<Stmt> Statements { get; }

    public ProgramNode(List<Stmt> statements) : base(0, 0)
    {
        Statements = statements;
    }
}

// 변수 선언: let x = expr;
public class LetStmt : Stmt
{
    public string Name        { get; }
    public Expr   Initializer { get; }

    public LetStmt(string name, Expr initializer, int line, int col) : base(line, col)
    {
        Name        = name;
        Initializer = initializer;
    }
}

// 변수 대입: x = expr;
// let과 달리 이미 선언된 변수에만 사용 가능 (인터프리터에서 검사)
public class AssignStmt : Stmt
{
    public string Name  { get; }
    public Expr   Value { get; }

    public AssignStmt(string name, Expr value, int line, int col) : base(line, col)
    {
        Name  = name;
        Value = value;
    }
}

// 조건문: if (expr) { ... } else { ... }
// ElseBranch는 else 블록이 없으면 null
public class IfStmt : Stmt
{
    public Expr       Condition  { get; }
    public List<Stmt> ThenBranch { get; }
    public List<Stmt> ElseBranch { get; }

    public IfStmt(Expr condition, List<Stmt> thenBranch, List<Stmt> elseBranch, int line, int col)
        : base(line, col)
    {
        Condition  = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }
}

// 고정 반복: loop (n) { ... }
// Count는 반복 횟수를 나타내는 표현식 (컴파일 타임 상수 or 변수)
public class LoopStmt : Stmt
{
    public Expr       Count { get; }
    public List<Stmt> Body  { get; }

    public LoopStmt(Expr count, List<Stmt> body, int line, int col) : base(line, col)
    {
        Count = count;
        Body  = body;
    }
}

// 조건 반복: while (expr) { ... }  [티어 1 해금]
public class WhileStmt : Stmt
{
    public Expr       Condition { get; }
    public List<Stmt> Body      { get; }

    public WhileStmt(Expr condition, List<Stmt> body, int line, int col) : base(line, col)
    {
        Condition = condition;
        Body      = body;
    }
}

// 함수 정의: def name(params) { ... }  [티어 2 해금]
public class DefStmt : Stmt
{
    public string       Name   { get; }
    public List<string> Params { get; }
    public List<Stmt>   Body   { get; }

    public DefStmt(string name, List<string> @params, List<Stmt> body, int line, int col)
        : base(line, col)
    {
        Name   = name;
        Params = @params;
        Body   = body;
    }
}

// 함수 반환: return expr;  /  return;
// Value는 반환값이 없는 bare return이면 null
public class ReturnStmt : Stmt
{
    public Expr Value { get; }

    public ReturnStmt(Expr value, int line, int col) : base(line, col)
    {
        Value = value;
    }
}

// 표현식 문장: bolt(target, FIRE, 30);
// 값을 반환하지만 결과를 버리는 호출 형태에 사용
public class ExprStmt : Stmt
{
    public Expr Expression { get; }

    public ExprStmt(Expr expression, int line, int col) : base(line, col)
    {
        Expression = expression;
    }
}
