using System.Collections.Generic;

// 변수 스코프 관리
// 블록({})에 진입할 때 PushScope, 빠져나올 때 PopScope를 호출한다.
// 변수 탐색은 현재(가장 안쪽) 스코프부터 전역 스코프 방향으로 수행한다.
public class SpellEnvironment
{
    // Stack의 Peek()이 현재(가장 안쪽) 스코프
    private readonly Stack<Dictionary<string, object>> scopes = new();

    public SpellEnvironment()
    {
        PushScope(); // 전역 스코프 초기화
    }

    public void PushScope() => scopes.Push(new Dictionary<string, object>());
    public void PopScope()  => scopes.Pop();

    // 현재 스코프에 새 변수를 선언한다 (let, 함수 파라미터 바인딩)
    public void Define(string name, object value) => scopes.Peek()[name] = value;

    // 안쪽 → 바깥쪽 순서로 탐색해 변수를 반환한다
    public object Get(string name, int line, int col)
    {
        foreach (var scope in scopes)
            if (scope.TryGetValue(name, out var val)) return val;

        throw new RuntimeException($"'{name}'은 정의되지 않은 변수입니다.", line, col);
    }

    // 이미 선언된 변수에 새 값을 대입한다 (assign)
    // 선언되지 않은 변수에 대입하면 RuntimeException
    public void Set(string name, object value, int line, int col)
    {
        foreach (var scope in scopes)
        {
            if (scope.ContainsKey(name))
            {
                scope[name] = value;
                return;
            }
        }

        throw new RuntimeException(
            $"'{name}'은 선언되지 않은 변수입니다. 먼저 let으로 선언하세요.", line, col);
    }

    // 해당 이름이 현재 환경 어딘가에 정의되어 있는지 확인한다
    public bool IsDefined(string name)
    {
        foreach (var scope in scopes)
            if (scope.ContainsKey(name)) return true;
        return false;
    }
}
