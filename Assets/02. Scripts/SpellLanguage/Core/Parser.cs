using System;
using System.Collections.Generic;
using System.Globalization;

// DSL 파서 - 토큰 목록을 AST로 변환한다.
// 재귀 하강(Recursive Descent) 방식으로 구현되며,
// 연산자 우선순위는 파싱 함수의 호출 순서로 표현한다.
//
// 우선순위 (낮음 → 높음)
//   or → and → equality → comparison → addition → multiplication → unary → postfix → primary
public class Parser
{
    private readonly List<Token> tokens;
    private int pos;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
        pos    = 0;
    }

    // 진입점: 전체 소스를 파싱해 ProgramNode를 반환한다.
    public ProgramNode Parse()
    {
        var stmts = new List<Stmt>();
        while (!IsEnd())
            stmts.Add(ParseStmt());
        return new ProgramNode(stmts);
    }

    #region 문장 파싱

    private Stmt ParseStmt()
    {
        return Current().Type switch
        {
            TokenType.Let    => ParseLetStmt(),
            TokenType.If     => ParseIfStmt(),
            TokenType.Loop   => ParseLoopStmt(),
            TokenType.While  => ParseWhileStmt(),
            TokenType.Def    => ParseDefStmt(),
            TokenType.Return => ParseReturnStmt(),
            _                => ParseAssignOrExprStmt(),
        };
    }

    // let x = expr;
    private Stmt ParseLetStmt()
    {
        var token = Consume(TokenType.Let);
        var name  = Consume(TokenType.Identifier).Value;
        Consume(TokenType.Assign);
        var init = ParseExpr();
        Consume(TokenType.Semicolon);
        return new LetStmt(name, init, token.Line, token.Column);
    }

    // if (expr) { ... } else { ... }
    private Stmt ParseIfStmt()
    {
        var token = Consume(TokenType.If);
        Consume(TokenType.LParen);
        var cond = ParseExpr();
        Consume(TokenType.RParen);
        var thenBranch = ParseBlock();

        List<Stmt> elseBranch = null;
        if (Match(TokenType.Else))
            elseBranch = ParseBlock();

        return new IfStmt(cond, thenBranch, elseBranch, token.Line, token.Column);
    }

    // loop (n) { ... }
    private Stmt ParseLoopStmt()
    {
        var token = Consume(TokenType.Loop);
        Consume(TokenType.LParen);
        var count = ParseExpr();
        Consume(TokenType.RParen);
        var body = ParseBlock();
        return new LoopStmt(count, body, token.Line, token.Column);
    }

    // while (expr) { ... }
    private Stmt ParseWhileStmt()
    {
        var token = Consume(TokenType.While);
        Consume(TokenType.LParen);
        var cond = ParseExpr();
        Consume(TokenType.RParen);
        var body = ParseBlock();
        return new WhileStmt(cond, body, token.Line, token.Column);
    }

    // def name(param1, param2) { ... }
    private Stmt ParseDefStmt()
    {
        var token = Consume(TokenType.Def);
        var name  = Consume(TokenType.Identifier).Value;
        Consume(TokenType.LParen);

        var @params = new List<string>();
        if (!Check(TokenType.RParen))
        {
            do { @params.Add(Consume(TokenType.Identifier).Value); }
            while (Match(TokenType.Comma));
        }

        Consume(TokenType.RParen);
        var body = ParseBlock();
        return new DefStmt(name, @params, body, token.Line, token.Column);
    }

    // return expr;  /  return;
    private Stmt ParseReturnStmt()
    {
        var token = Consume(TokenType.Return);
        Expr value = null;
        if (!Check(TokenType.Semicolon))
            value = ParseExpr();
        Consume(TokenType.Semicolon);
        return new ReturnStmt(value, token.Line, token.Column);
    }

    // x = expr;  또는  expr;
    // 식별자 바로 뒤에 '='(==가 아님)이 오면 대입문으로 판단한다.
    private Stmt ParseAssignOrExprStmt()
    {
        var token = Current();

        if (Check(TokenType.Identifier) && CheckNext(TokenType.Assign))
        {
            var name = Consume(TokenType.Identifier).Value;
            Consume(TokenType.Assign);
            var value = ParseExpr();
            Consume(TokenType.Semicolon);
            return new AssignStmt(name, value, token.Line, token.Column);
        }

        var expr = ParseExpr();
        Consume(TokenType.Semicolon);
        return new ExprStmt(expr, token.Line, token.Column);
    }

    // { stmt* }
    private List<Stmt> ParseBlock()
    {
        Consume(TokenType.LBrace);
        var stmts = new List<Stmt>();
        while (!Check(TokenType.RBrace) && !IsEnd())
            stmts.Add(ParseStmt());
        Consume(TokenType.RBrace);
        return stmts;
    }

    #endregion

    #region 표현식 파싱 (연산자 우선순위 낮음 → 높음)

    private Expr ParseExpr() => ParseOr();

    // ||
    private Expr ParseOr()
    {
        var left = ParseAnd();
        while (Check(TokenType.Or))
        {
            var op    = Advance();
            var right = ParseAnd();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // &&
    private Expr ParseAnd()
    {
        var left = ParseEquality();
        while (Check(TokenType.And))
        {
            var op    = Advance();
            var right = ParseEquality();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // == !=
    private Expr ParseEquality()
    {
        var left = ParseComparison();
        while (Check(TokenType.Eq) || Check(TokenType.Neq))
        {
            var op    = Advance();
            var right = ParseComparison();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // < > <= >=
    private Expr ParseComparison()
    {
        var left = ParseAddition();
        while (Check(TokenType.Lt) || Check(TokenType.Gt) ||
               Check(TokenType.Lte) || Check(TokenType.Gte))
        {
            var op    = Advance();
            var right = ParseAddition();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // + -
    private Expr ParseAddition()
    {
        var left = ParseMultiplication();
        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var op    = Advance();
            var right = ParseMultiplication();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // * / %
    private Expr ParseMultiplication()
    {
        var left = ParseUnary();
        while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
        {
            var op    = Advance();
            var right = ParseUnary();
            left = new BinaryExpr(left, op.Value, right, op.Line, op.Column);
        }
        return left;
    }

    // 단항: !flag, -value
    private Expr ParseUnary()
    {
        if (Check(TokenType.Not) || Check(TokenType.Minus))
        {
            var op      = Advance();
            var operand = ParseUnary();
            return new UnaryExpr(op.Value, operand, op.Line, op.Column);
        }
        return ParsePostfix();
    }

    // 후위: 멤버 접근(.x), 함수 호출((...))  — 체이닝 허용
    // 예: target.position.x  /  self.cast(target)
    private Expr ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Check(TokenType.Dot))
            {
                var dot    = Advance();
                var member = Consume(TokenType.Identifier);
                expr = new MemberExpr(expr, member.Value, dot.Line, dot.Column);
            }
            else if (Check(TokenType.LParen))
            {
                var paren = Advance();
                var args  = new List<Expr>();
                if (!Check(TokenType.RParen))
                {
                    do { args.Add(ParseExpr()); }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.RParen);
                expr = new CallExpr(expr, args, paren.Line, paren.Column);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    // 기본 요소: 리터럴, 식별자, 괄호 표현식
    private Expr ParsePrimary()
    {
        var token = Current();

        if (Check(TokenType.Int))
        {
            Advance();
            var val = double.Parse(token.Value, CultureInfo.InvariantCulture);
            return new NumberExpr(val, false, token.Line, token.Column);
        }

        if (Check(TokenType.Float))
        {
            Advance();
            var val = double.Parse(token.Value, CultureInfo.InvariantCulture);
            return new NumberExpr(val, true, token.Line, token.Column);
        }

        if (Check(TokenType.String))
        {
            Advance();
            return new StringExpr(token.Value, token.Line, token.Column);
        }

        if (Check(TokenType.Bool))
        {
            Advance();
            return new BoolExpr(token.Value == "true", token.Line, token.Column);
        }

        if (Check(TokenType.Identifier))
        {
            Advance();
            return new IdentifierExpr(token.Value, token.Line, token.Column);
        }

        if (Check(TokenType.LParen))
        {
            Advance();
            var expr = ParseExpr();
            Consume(TokenType.RParen);
            return expr;
        }

        throw new ParserException(
            $"표현식이 예상되지만 '{token.Value}'가 발견되었습니다.",
            token.Line, token.Column);
    }

    #endregion

    #region 내부 헬퍼

    private Token Current() => tokens[pos];

    // pos+offset 위치 토큰을 반환 (범위 초과 시 마지막 토큰 반환)
    private Token Peek(int offset) => tokens[Math.Min(pos + offset, tokens.Count - 1)];

    private Token Advance()
    {
        var t = tokens[pos];
        if (!IsEnd()) pos++;
        return t;
    }

    // 현재 토큰이 type과 일치하면 소비하고 반환, 아니면 예외를 던진다.
    private Token Consume(TokenType type)
    {
        if (!Check(type))
        {
            var cur = Current();
            throw new ParserException(
                $"'{TypeToString(type)}'이 예상되지만 '{cur.Value}'가 발견되었습니다.",
                cur.Line, cur.Column);
        }
        return Advance();
    }

    // 현재 토큰이 type과 일치하면 소비하고 true를 반환한다.
    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    private bool Check(TokenType type)     => !IsEnd() && Current().Type == type;

    // 한 칸 앞 토큰의 타입을 미리 확인한다 (대입 vs 표현식 구분용)
    private bool CheckNext(TokenType type) => Peek(1).Type == type;

    private bool IsEnd() => Current().Type == TokenType.EOF;

    private static string TypeToString(TokenType type) => type switch
    {
        TokenType.Semicolon  => ";",
        TokenType.LParen     => "(",
        TokenType.RParen     => ")",
        TokenType.LBrace     => "{",
        TokenType.RBrace     => "}",
        TokenType.Comma      => ",",
        TokenType.Assign     => "=",
        TokenType.Identifier => "식별자",
        _                    => type.ToString(),
    };

    #endregion
}

// 파서 예외 - 오류 위치(줄/열)를 함께 보관해 플레이어에게 정확한 위치를 안내한다.
public class ParserException : Exception
{
    public int Line   { get; }
    public int Column { get; }

    public ParserException(string message, int line, int column)
        : base($"[{line}:{column}] {message}")
    {
        Line   = line;
        Column = column;
    }
}
