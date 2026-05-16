using System.Collections.Generic;

// DSL 자동완성 후보를 관리하는 순수 C# 클래스.
// SpellLibrary에서 SpellAction/SpellObject를 동적으로 수집하고,
// Interpreter.BuiltinFunctions에서 내장 함수를 읽어 후보 목록을 빌드한다.
//
// ─── 확장 가이드 ───────────────────────────────────────────────
//   새 SpellAction 추가  → SpellLibrary에 등록만 하면 자동 반영
//   새 SpellObject 추가  → SpellLibrary에 등록만 하면 자동 반영
//   새 내장 함수 추가    → Interpreter.BuiltinFunctions 배열에 1줄 추가
//   새 키워드 추가       → 아래 Keywords 배열에 1줄 추가
//   새 멤버 추가         → 아래 MemberMap 딕셔너리에 1줄 추가
// ──────────────────────────────────────────────────────────────
public class AutocompleteProvider
{
    // ─── 확장 시 이 두 곳만 수정 ─────────────────────────────

    // 새 키워드 추가 시 여기에만 추가
    private static readonly string[] Keywords =
    {
        "let", "if", "else", "loop", "while", "def", "return", "true", "false",
    };

    // 새 Entity/Proj 멤버 추가 시 해당 키에 1줄 추가
    private static readonly Dictionary<string, string[]> MemberMap = new()
    {
        { "self",   new[] { "hp", "maxHp", "hpRatio", "moveSpeed", "position" } },
        { "target", new[] { "hp", "maxHp", "hpRatio", "moveSpeed", "position" } },
        { "proj",   new[] { "position", "velocity", "speed", "damage", "element" } },
    };

    // ─────────────────────────────────────────────────────────

    private const int MaxResults = 8;

    private readonly List<AutocompleteEntry> globalList = new();

    // SpellLibrary 전달 → SpellAction / SpellObject 전체를 자동 수집한다.
    public AutocompleteProvider(SpellLibrary library)
    {
        Build(library);
    }

    private void Build(SpellLibrary library)
    {
        // 키워드
        foreach (var kw in Keywords)
            globalList.Add(new AutocompleteEntry(kw, "keyword"));

        // SpellAction / SpellObject — SpellLibrary.GetAll()에서 자동 수집
        foreach (var item in library.GetAll())
        {
            switch (item)
            {
                case SpellAction action:
                    globalList.Add(new AutocompleteEntry(action.DSLName, action.Signature));
                    break;
                case SpellObject obj:
                    globalList.Add(new AutocompleteEntry(obj.Name, "SpellObject"));
                    break;
            }
        }

        // 내장 함수 — Interpreter.BuiltinFunctions에서 자동 수집
        foreach (var (name, sig) in Interpreter.BuiltinFunctions)
            globalList.Add(new AutocompleteEntry(name, sig));

        // 내장 변수
        foreach (var v in new[] { "self", "target", "power" })
            globalList.Add(new AutocompleteEntry(v, "variable"));

        // 속성 상수
        foreach (var el in new[] { "None", "Fire", "Water", "Ice", "Lightning",
                                    "Earth", "Wind", "Light", "Dark" })
            globalList.Add(new AutocompleteEntry(el, "element"));
    }

    // prefix로 시작하는 전역 후보를 최대 MaxResults개 반환한다.
    public List<AutocompleteEntry> GetGlobalCompletions(string prefix)
    {
        var result = new List<AutocompleteEntry>(MaxResults);
        foreach (var entry in globalList)
        {
            if (entry.Label.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                result.Add(entry);
                if (result.Count >= MaxResults) break;
            }
        }
        return result;
    }

    // identifier에 대한 멤버 후보를 prefix 필터와 함께 반환한다.
    public List<AutocompleteEntry> GetMemberCompletions(string identifier, string prefix)
    {
        if (!MemberMap.TryGetValue(identifier, out var members))
            return new List<AutocompleteEntry>();

        var result = new List<AutocompleteEntry>();
        foreach (var m in members)
        {
            if (string.IsNullOrEmpty(prefix) ||
                m.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                result.Add(new AutocompleteEntry(m, "member"));
        }
        return result;
    }
}

// 자동완성 후보 항목.
//   Label  : 실제 삽입되는 텍스트 (예: "Bolt")
//   Detail : 팝업 우측에 표시할 힌트 (예: "Bolt(target, element, damage)")
public class AutocompleteEntry
{
    public string Label  { get; }
    public string Detail { get; }

    public AutocompleteEntry(string label, string detail)
    {
        Label  = label;
        Detail = detail;
    }
}
