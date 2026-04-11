using System;
using System.Collections.Generic;
using System.Text;

// DSL 렉서 - 소스 문자열을 토큰 목록으로 변환한다.
// 파이프라인: 소스 문자열 → Lexer → List<Token> → Parser
public class Lexer
{
    private readonly string source;
    private int pos;
    private int line;
    private int col;

    // 키워드 테이블: 식별자를 읽은 뒤 키워드 여부를 O(1)로 판별한다.
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "let",    TokenType.Let    },
        { "if",     TokenType.If     },
        { "else",   TokenType.Else   },
        { "loop",   TokenType.Loop   },
        { "while",  TokenType.While  },
        { "def",    TokenType.Def    },
        { "return", TokenType.Return },
        { "true",   TokenType.Bool   },
        { "false",  TokenType.Bool   },
    };

    public Lexer(string source)
    {
        this.source = source;
        pos    = 0;
        line   = 1;
        col    = 1;
    }

    // 소스 전체를 토큰 목록으로 변환한다.
    // 마지막 토큰은 항상 EOF이다.
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        for (;;)
        {
            SkipWhitespaceAndComments();

            if (IsEnd())
            {
                tokens.Add(Make(TokenType.EOF, ""));
                break;
            }

            tokens.Add(NextToken());
        }

        return tokens;
    }

    #region 토큰 읽기

    private Token NextToken()
    {
        char c = Current();

        if (char.IsDigit(c))                      return ReadNumber();
        if (c == '"')                              return ReadString();
        if (char.IsLetter(c) || c == '_')         return ReadIdentifier();

        return ReadSymbol();
    }

    // 정수 / 실수 리터럴
    // 소수점이 두 번 나오거나 다음 문자가 숫자가 아니면 멤버 접근 연산자로 판단해 중단
    private Token ReadNumber()
    {
        int startLine = line, startCol = col;
        var sb       = new StringBuilder();
        bool isFloat = false;

        while (!IsEnd() && (char.IsDigit(Current()) || Current() == '.'))
        {
            if (Current() == '.')
            {
                if (isFloat) break;
                if (pos + 1 >= source.Length || !char.IsDigit(source[pos + 1])) break;
                isFloat = true;
            }
            sb.Append(Advance());
        }

        var type = isFloat ? TokenType.Float : TokenType.Int;
        return new Token(type, sb.ToString(), startLine, startCol);
    }

    // 문자열 리터럴 ("...")
    private Token ReadString()
    {
        int startLine = line, startCol = col;
        Advance(); // 여는 " 소비

        var sb = new StringBuilder();
        while (!IsEnd() && Current() != '"')
        {
            if (Current() == '\n')
                throw new LexerException("문자열이 닫히지 않았습니다.", startLine, startCol);
            sb.Append(Advance());
        }

        if (IsEnd())
            throw new LexerException("문자열이 닫히지 않았습니다.", startLine, startCol);

        Advance(); // 닫는 " 소비
        return new Token(TokenType.String, sb.ToString(), startLine, startCol);
    }

    // 식별자 / 키워드
    private Token ReadIdentifier()
    {
        int startLine = line, startCol = col;
        var sb = new StringBuilder();

        while (!IsEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_'))
            sb.Append(Advance());

        var text = sb.ToString();
        var type = Keywords.TryGetValue(text, out var kw) ? kw : TokenType.Identifier;
        return new Token(type, text, startLine, startCol);
    }

    // 연산자 및 구분자
    private Token ReadSymbol()
    {
        int startLine = line, startCol = col;
        char c = Advance();

        return c switch
        {
            '+' => new Token(TokenType.Plus,      "+",  startLine, startCol),
            '-' => new Token(TokenType.Minus,     "-",  startLine, startCol),
            '*' => new Token(TokenType.Star,      "*",  startLine, startCol),
            '/' => new Token(TokenType.Slash,     "/",  startLine, startCol),
            '%' => new Token(TokenType.Percent,   "%",  startLine, startCol),
            '.' => new Token(TokenType.Dot,       ".",  startLine, startCol),
            '{' => new Token(TokenType.LBrace,    "{",  startLine, startCol),
            '}' => new Token(TokenType.RBrace,    "}",  startLine, startCol),
            '(' => new Token(TokenType.LParen,    "(",  startLine, startCol),
            ')' => new Token(TokenType.RParen,    ")",  startLine, startCol),
            ',' => new Token(TokenType.Comma,     ",",  startLine, startCol),
            ';' => new Token(TokenType.Semicolon, ";",  startLine, startCol),

            '=' => Match('=') ? new Token(TokenType.Eq,     "==", startLine, startCol)
                              : new Token(TokenType.Assign,  "=",  startLine, startCol),

            '!' => Match('=') ? new Token(TokenType.Neq,    "!=", startLine, startCol)
                              : new Token(TokenType.Not,     "!",  startLine, startCol),

            '<' => Match('=') ? new Token(TokenType.Lte,    "<=", startLine, startCol)
                              : new Token(TokenType.Lt,      "<",  startLine, startCol),

            '>' => Match('=') ? new Token(TokenType.Gte,    ">=", startLine, startCol)
                              : new Token(TokenType.Gt,      ">",  startLine, startCol),

            '&' => Match('&') ? new Token(TokenType.And,    "&&", startLine, startCol)
                              : throw new LexerException("'&' 단독 사용 불가. '&&'를 사용하세요.", startLine, startCol),

            '|' => Match('|') ? new Token(TokenType.Or,     "||", startLine, startCol)
                              : throw new LexerException("'|' 단독 사용 불가. '||'를 사용하세요.", startLine, startCol),

            _ => throw new LexerException($"알 수 없는 문자 '{c}'입니다.", startLine, startCol),
        };
    }

    #endregion

    #region 공백 및 주석 스킵

    // 공백·탭·줄바꿈·한 줄 주석(#)을 건너뛴다.
    private void SkipWhitespaceAndComments()
    {
        while (!IsEnd())
        {
            char c = Current();

            if (c == ' ' || c == '\t' || c == '\r')
            {
                Advance();
            }
            else if (c == '\n')
            {
                Advance();
                line++;
                col = 1;
            }
            else if (c == '#') // 한 줄 주석
            {
                while (!IsEnd() && Current() != '\n')
                    Advance();
            }
            else
            {
                break;
            }
        }
    }

    #endregion

    #region 내부 헬퍼

    private char Current() => source[pos];

    // 현재 문자를 반환하고 위치를 한 칸 전진한다.
    private char Advance()
    {
        char c = source[pos++];
        col++;
        return c;
    }

    // 다음 문자가 expected와 같으면 소비하고 true를 반환한다.
    private bool Match(char expected)
    {
        if (IsEnd() || source[pos] != expected) return false;
        Advance();
        return true;
    }

    private bool IsEnd() => pos >= source.Length;

    // 현재 위치 기반으로 토큰을 생성한다.
    private Token Make(TokenType type, string value) => new Token(type, value, line, col);

    #endregion
}

// 렉서 예외 - 오류 위치(줄/열)를 함께 보관해 플레이어에게 정확한 위치를 안내한다.
public class LexerException : Exception
{
    public int Line   { get; }
    public int Column { get; }

    public LexerException(string message, int line, int column)
        : base($"[{line}:{column}] {message}")
    {
        Line   = line;
        Column = column;
    }
}
