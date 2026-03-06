using System;
using System.Collections.Generic;
using System.Text;

// DSL 렉서 - 소스 문자열을 토큰 목록으로 변환한다.
// 파이프라인: 소스 문자열 → Lexer → List<Token> → Parser
public class Lexer
{
    private readonly string _source;
    private int _pos;
    private int _line;
    private int _col;

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
        _source = source;
        _pos    = 0;
        _line   = 1;
        _col    = 1;
    }

    // 소스 전체를 토큰 목록으로 변환한다.
    // 마지막 토큰은 항상 EOF이다.
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (true)
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
        int line = _line, col = _col;
        var sb       = new StringBuilder();
        bool isFloat = false;

        while (!IsEnd() && (char.IsDigit(Current()) || Current() == '.'))
        {
            if (Current() == '.')
            {
                if (isFloat) break;
                if (_pos + 1 >= _source.Length || !char.IsDigit(_source[_pos + 1])) break;
                isFloat = true;
            }
            sb.Append(Advance());
        }

        var type = isFloat ? TokenType.Float : TokenType.Int;
        return new Token(type, sb.ToString(), line, col);
    }

    // 문자열 리터럴 ("...")
    private Token ReadString()
    {
        int line = _line, col = _col;
        Advance(); // 여는 " 소비

        var sb = new StringBuilder();
        while (!IsEnd() && Current() != '"')
        {
            if (Current() == '\n')
                throw new LexerException("문자열이 닫히지 않았습니다.", line, col);
            sb.Append(Advance());
        }

        if (IsEnd())
            throw new LexerException("문자열이 닫히지 않았습니다.", line, col);

        Advance(); // 닫는 " 소비
        return new Token(TokenType.String, sb.ToString(), line, col);
    }

    // 식별자 / 키워드
    private Token ReadIdentifier()
    {
        int line = _line, col = _col;
        var sb = new StringBuilder();

        while (!IsEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_'))
            sb.Append(Advance());

        var text = sb.ToString();
        var type = Keywords.TryGetValue(text, out var kw) ? kw : TokenType.Identifier;
        return new Token(type, text, line, col);
    }

    // 연산자 및 구분자
    private Token ReadSymbol()
    {
        int line = _line, col = _col;
        char c = Advance();

        return c switch
        {
            '+' => new Token(TokenType.Plus,      "+",  line, col),
            '-' => new Token(TokenType.Minus,     "-",  line, col),
            '*' => new Token(TokenType.Star,      "*",  line, col),
            '/' => new Token(TokenType.Slash,     "/",  line, col),
            '%' => new Token(TokenType.Percent,   "%",  line, col),
            '.' => new Token(TokenType.Dot,       ".",  line, col),
            '{' => new Token(TokenType.LBrace,    "{",  line, col),
            '}' => new Token(TokenType.RBrace,    "}",  line, col),
            '(' => new Token(TokenType.LParen,    "(",  line, col),
            ')' => new Token(TokenType.RParen,    ")",  line, col),
            ',' => new Token(TokenType.Comma,     ",",  line, col),
            ';' => new Token(TokenType.Semicolon, ";",  line, col),

            '=' => Match('=') ? new Token(TokenType.Eq,     "==", line, col)
                              : new Token(TokenType.Assign,  "=",  line, col),

            '!' => Match('=') ? new Token(TokenType.Neq,    "!=", line, col)
                              : new Token(TokenType.Not,     "!",  line, col),

            '<' => Match('=') ? new Token(TokenType.Lte,    "<=", line, col)
                              : new Token(TokenType.Lt,      "<",  line, col),

            '>' => Match('=') ? new Token(TokenType.Gte,    ">=", line, col)
                              : new Token(TokenType.Gt,      ">",  line, col),

            '&' => Match('&') ? new Token(TokenType.And,    "&&", line, col)
                              : throw new LexerException("'&' 단독 사용 불가. '&&'를 사용하세요.", line, col),

            '|' => Match('|') ? new Token(TokenType.Or,     "||", line, col)
                              : throw new LexerException("'|' 단독 사용 불가. '||'를 사용하세요.", line, col),

            _ => throw new LexerException($"알 수 없는 문자 '{c}'입니다.", line, col),
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
                _line++;
                _col = 1;
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

    private char Current() => _source[_pos];

    // 현재 문자를 반환하고 위치를 한 칸 전진한다.
    private char Advance()
    {
        char c = _source[_pos++];
        _col++;
        return c;
    }

    // 다음 문자가 expected와 같으면 소비하고 true를 반환한다.
    private bool Match(char expected)
    {
        if (IsEnd() || _source[_pos] != expected) return false;
        Advance();
        return true;
    }

    private bool IsEnd() => _pos >= _source.Length;

    // 현재 위치 기반으로 토큰을 생성한다.
    private Token Make(TokenType type, string value) => new Token(type, value, _line, _col);

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
