using System;

// 노드 파라미터 직렬화 데이터.
// Blueprint UI ↔ 런타임 노드 간 파라미터 값을 문자열로 저장한다.
[Serializable]
public class SpellParamData
{
    public string name;
    public string type;   // "float" | "int" | "bool" | "string" | "ElementType" | "Vector2"
    public string value;
}
