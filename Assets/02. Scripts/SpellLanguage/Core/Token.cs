using System;

// DSL 토큰 타입 정의
// 언어의 모든 어휘 요소(리터럴, 키워드, 연산자, 구분자)를 열거한다.
public enum TokenType
{
    #region 리터럴
    Int,        // 정수: 30, 100
    Float,      // 실수: 1.5, 0.5
    String,     // 문자열: "FIRE"
    Bool,       // 불리언: true, false
    #endregion

    #region 식별자
    Identifier, // 변수명, 함수명: damage, bolt, target
    #endregion

    #region 키워드
    Let,        // 변수 선언
    If,         // 조건문
    Else,       // 조건문 분기
    Loop,       // 고정 횟수 반복
    While,      // 조건 반복 (티어 1 해금)
    Def,        // 함수 정의 (티어 2 해금)
    Return,     // 함수 반환
    #endregion

    #region 산술 연산자
    Plus,       // +
    Minus,      // -
    Star,       // *
    Slash,      // /
    Percent,    // %
    #endregion

    #region 비교 연산자
    Eq,         // ==
    Neq,        // !=
    Lt,         // <
    Gt,         // >
    Lte,        // <=
    Gte,        // >=
    #endregion

    #region 논리 연산자
    And,        // &&
    Or,         // ||
    Not,        // !
    #endregion

    #region 대입
    Assign,     // =
    #endregion

    #region 멤버 접근
    Dot,        // .  예: target.hp
    #endregion

    #region 구분자
    LBrace,     // {
    RBrace,     // }
    LParen,     // (
    RParen,     // )
    Comma,      // ,
    Semicolon,  // ;
    #endregion

    #region 특수
    EOF,        // 입력 끝
    #endregion
}

// DSL 토큰 - 타입, 값, 소스 위치를 보관한다.
// Line/Column은 오류 메시지에서 플레이어에게 위치를 알려주기 위해 사용된다.
public class Token
{
    public TokenType Type   { get; }
    public string    Value  { get; }
    public int       Line   { get; }
    public int       Column { get; }

    public Token(TokenType type, string value, int line, int column)
    {
        Type   = type;
        Value  = value;
        Line   = line;
        Column = column;
    }

    public override string ToString() => $"[{Type} '{Value}' ({Line}:{Column})]";
}
