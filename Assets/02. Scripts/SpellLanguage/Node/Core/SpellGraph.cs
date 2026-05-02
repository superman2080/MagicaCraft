using System.Collections.Generic;

// 노드 그래프 컨테이너.
// CastEntryNode / UpdateEntryNode / EndEntryNode 중 사용하지 않는 슬롯은 null로 둔다.
public class SpellGraph
{
    public string Name { get; set; } = "새 그래프";

    // 진입 노드 3종
    public SpellNode CastEntryNode   { get; set; }
    public SpellNode UpdateEntryNode { get; set; }
    public SpellNode EndEntryNode    { get; set; }

    // 전체 노드 목록 — Blueprint 직렬화·탐색용
    private readonly List<SpellNode> nodes = new();
    public IReadOnlyList<SpellNode> Nodes => nodes;

    public void AddNode(SpellNode node)    => nodes.Add(node);
    public void RemoveNode(SpellNode node) => nodes.Remove(node);
}
